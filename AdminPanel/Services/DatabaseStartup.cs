#pragma warning disable EF1001 // Порівняння моделі з міграціями — той самий внутрішній API, яким користується EF 9.

using System.Data;
using System.Diagnostics;
using DAL.Context;
using DAL.Seed;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AdminPanel.Services;

/// <summary>
/// Готує базу даних під час запуску сайту:
/// 1) чекає, поки SQL Server стане доступним (після рестарту сервера він піднімається не одразу);
/// 2) створює базу, якщо її ще немає, і накочує всі міграції, яких у ній бракує;
/// 3) заповнює довідники (ролі, адміністратор, категорії).
///
/// Якщо сайт запущено на кількох серверах за балансувальником, міграції виконує лише один:
/// решта чекають на блокування SQL Server (sp_getapplock) і бачать уже оновлену базу.
///
/// Налаштування — секція «Database» в appsettings.json:
///   AutoMigrate     — накочувати міграції під час запуску (типово true);
///   StartupRetries  — скільки разів пробувати підключитися (типово 6);
///   StopOnFailure   — зупинити сайт, якщо базу не вдалося підготувати (типово false:
///                     сайт стартує, а помилка лишається в журналі).
/// </summary>
public static class DatabaseStartup
{
    private const string LockName = "Vybir.DatabaseMigrations";

    private const int LockTimeoutMs = 5 * 60 * 1000;

    public static async Task PrepareAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseStartup");
        var config = app.Configuration;

        var autoMigrate = config.GetValue("Database:AutoMigrate", true);
        var retries = Math.Max(1, config.GetValue("Database:StartupRetries", 6));
        var stopOnFailure = config.GetValue("Database:StopOnFailure", false);

        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();

            if (app.Environment.IsDevelopment() && config.GetValue("Database:RecreateOnStart", false))
            {
                logger.LogWarning("Database:RecreateOnStart увімкнено — базу буде видалено й створено заново");
                await db.Database.EnsureDeletedAsync(cancellationToken);
            }

            if (autoMigrate)
            {
                await WithRetriesAsync(() => MigrateAsync(db, logger, cancellationToken), retries, logger, cancellationToken);
                WarnAboutModelChanges(db, logger);
            }
            else
            {
                logger.LogInformation("Database:AutoMigrate вимкнено — міграції під час запуску не накочуються");
            }

            var seeder = services.GetRequiredService<IDatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
            logger.LogInformation("База даних готова до роботи");
        }
        catch (Exception ex) when (!stopOnFailure && ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Не вдалося підготувати базу даних. Сайт запущено, але сторінки з даними не працюватимуть, доки базу не буде доступно");
        }
    }

    private static async Task MigrateAsync(ApplicationDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var all = db.Database.GetMigrations().ToList();

        // Бази ще немає: створювати її й блокувати нічого — MigrateAsync створить базу сам.
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            logger.LogInformation("Базу даних не знайдено — створюємо її та застосовуємо {Count} міграцій", all.Count);
            await TimedMigrateAsync(db, logger, all, cancellationToken);
            return;
        }

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("База даних актуальна: застосовано {Count} міграцій, нових немає", all.Count);
            return;
        }

        // Одне з'єднання на все: блокування належить сесії й знімається разом із нею.
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await AcquireLockAsync(db, cancellationToken);

            // Поки ми чекали на блокування, міграції міг накотити інший сервер.
            pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

            if (pending.Count == 0)
            {
                logger.LogInformation("Міграції вже застосовано іншим екземпляром сайту");
                return;
            }

            await TimedMigrateAsync(db, logger, pending, cancellationToken);
        }
        finally
        {
            await ReleaseLockAsync(db);
            await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task TimedMigrateAsync(
        ApplicationDbContext db, ILogger logger, IReadOnlyList<string> migrations, CancellationToken cancellationToken)
    {
        logger.LogInformation("Застосовуємо міграції ({Count}): {Migrations}", migrations.Count, string.Join(", ", migrations));

        var timer = Stopwatch.StartNew();
        await db.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Міграції застосовано за {Seconds:0.0} с", timer.Elapsed.TotalSeconds);
    }

    private static async Task AcquireLockAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "sp_getapplock";
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = LockTimeoutMs / 1000 + 30;
        command.Parameters.Add(new SqlParameter("@Resource", LockName));
        command.Parameters.Add(new SqlParameter("@LockMode", "Exclusive"));
        command.Parameters.Add(new SqlParameter("@LockOwner", "Session"));
        command.Parameters.Add(new SqlParameter("@LockTimeout", LockTimeoutMs));
        var result = new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue };
        command.Parameters.Add(result);

        await command.ExecuteNonQueryAsync(cancellationToken);

        if ((int)result.Value < 0)
        {
            throw new InvalidOperationException(
                $"Не вдалося дочекатися блокування міграцій (sp_getapplock повернув {result.Value})");
        }
    }

    private static async Task ReleaseLockAsync(ApplicationDbContext db)
    {
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Resource", LockName));
            command.Parameters.Add(new SqlParameter("@LockOwner", "Session"));
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Блокування не було взято або сесія вже закрита — тоді воно зняте й так.
        }
    }

    /// <summary>Повторює підключення, поки SQL Server ще не відповідає (рестарт сервера, мережа).</summary>
    private static async Task WithRetriesAsync(
        Func<Task> action, int attempts, ILogger logger, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (SqlException ex) when (attempt < attempts && IsTransient(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 2 * attempt));
                logger.LogWarning("SQL Server поки недоступний ({Message}). Спроба {Attempt} з {Attempts}, повтор через {Delay} с",
                    ex.Message, attempt, attempts, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <summary>Помилки мережі й тимчасової недоступності сервера; помилки самих міграцій не повторюємо.</summary>
    private static bool IsTransient(SqlException ex) =>
        ex.Number is -2 or 2 or 53 or 121 or 233 or 64 or 10053 or 10054 or 10060 or 40197 or 40501 or 40613 or 49918 or 49919 or 49920;

    /// <summary>
    /// Попереджає, якщо в коді змінили сутності, а міграцію під це не створили:
    /// тоді база після накочування не збігатиметься з моделлю, і запити падатимуть.
    /// </summary>
    private static void WarnAboutModelChanges(ApplicationDbContext db, ILogger logger)
    {
        try
        {
            var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot?.Model;

            if (snapshot == null)
            {
                return;
            }

            if (snapshot is IMutableModel mutable)
            {
                snapshot = mutable.FinalizeModel();
            }

            snapshot = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot);

            var differ = db.GetService<IMigrationsModelDiffer>();
            var current = db.GetService<IDesignTimeModel>().Model;

            if (differ.HasDifferences(snapshot.GetRelationalModel(), current.GetRelationalModel()))
            {
                logger.LogWarning(
                    "Модель даних змінено, але міграцію під ці зміни не створено. " +
                    "Виконайте Add-Migration <Назва> -Project DAL -StartupProject AdminPanel і закомітьте її");
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Не вдалося порівняти модель із міграціями");
        }
    }
}
