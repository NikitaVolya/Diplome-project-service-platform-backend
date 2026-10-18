using DAL.Context;
using Domain.Common;
using Domain.Entities;
using Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DAL.Seed
{
    public interface IDatabaseSeeder
    {
        Task SeedAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Доводить свіжу базу до робочого стану: ролі, службовий акаунт для входу та, за бажанням,
    /// масив демо-даних, щоб під час показу дашборд не був суцільними нулями.
    /// Кожен крок ідемпотентний, тому запускати його на кожному старті безпечно.
    /// </summary>
    public class DatabaseSeeder : IDatabaseSeeder
    {
        /// <summary>Фіксоване зерно генератора: два запуски на двох машинах дають однакові демо-дані.</summary>
        private const int RandomSeed = 20260828;

        // Дизайнер перейменував частину напрямів уже після того, як база була заповнена.
        // Перейменовуємо наявні рядки замість того, щоб створювати дублікати: замовлення,
        // відгуки та платежі лишаються прив'язаними до тих самих категорій.
        /// <summary>
        /// Послуги з вітрини «Популярне поруч із вами» на головній. Демо-замовлення
        /// зміщуємо в їхній бік: напрямів у каталозі понад дві сотні, і якщо сипати
        /// замовлення рівним шаром, кожна послуга отримує одне-два — вітрина виходить
        /// порожньою, та й у житті кілька напрямів завжди збирають більшість замовлень.
        /// </summary>
        private static readonly string[] ShowcaseServices =
        {
            "Сантехнічні роботи", "Прибирання квартир", "Логотипи та стиль", "Електромонтажні роботи"
        };

        /// <summary>
        /// Назви, які дизайнер змінив уже після того, як у базі зʼявилися замовлення.
        /// Перейменовуємо, а не створюємо заново, щоб не загубити історію.
        /// Parent — напрям, у якому шукати: однакові назви трапляються в різних
        /// напрямах («Аніматор» був і в подіях, і в дітях), і без напряму
        /// перейменувався б перший-ліпший. Для самих напрямів Parent порожній.
        /// </summary>
        private static readonly (string From, string To, string? Parent)[] CategoryRenames =
        {
            ("Дизайн та творчість", "Дизайн і творчість", null),
            ("Авто і транспорт", "Автопослуги", null),
            ("IT та технології", "IT та розробка", null),
            ("Освіта та репетиторство", "Освіта і розвиток", null),
            ("Здоров'я та спорт", "Спорт та здоров'я", null),
            ("Тварини", "Послуги для тварин", null),
            ("Сантехнік", "Сантехнічні роботи", "Дім і ремонт"),
            ("Електрик", "Електромонтажні роботи", "Дім і ремонт"),
            ("Малярні роботи", "Оздоблювальні роботи", "Дім і ремонт"),
            ("Збірка меблів", "Меблі та столярні роботи", "Дім і ремонт"),
            ("Прибирання квартири", "Прибирання квартир", "Прибирання"),
            ("Дизайн логотипу", "Логотипи та стиль", "Дизайн і творчість"),

            ("Ветеринар", "Ветеринарні послуги", "Послуги для тварин"),
            ("Передержка", "Перетримка тварин", "Послуги для тварин"),
            ("Зоотаксі", "Перевезення тварин", "Послуги для тварин"),
            ("Догляд за котами", "Догляд за тваринами", "Послуги для тварин"),
            ("Няня", "Послуги няні", "Діти та догляд"),
            ("Супровід дитини", "Супровід дітей", "Діти та догляд"),
            ("Догляд за немовлям", "Догляд за немовлятами", "Діти та догляд"),
            ("Догляд за маломобільними", "Особливий догляд", "Діти та догляд"),
            ("Математика", "Репетитори та шкільні предмети", "Освіта і розвиток"),
            ("Англійська мова", "Іноземні мови", "Освіта і розвиток"),
            ("Підготовка до НМТ", "Підготовка до іспитів", "Освіта і розвиток"),
            ("Програмування", "Комп'ютерні та IT-курси", "Освіта і розвиток"),
            ("Малювання", "Творчі заняття", "Освіта і розвиток"),
            ("Музика", "Музика та вокал", "Освіта і розвиток"),
            ("Організація свята", "Організація подій", "Події та допомога"),
            ("Організація весілля", "Весільні послуги", "Події та допомога"),
            ("Ведучий", "Ведучі та шоумени", "Події та допомога"),
            ("Музиканти", "Музиканти та DJ", "Події та допомога"),
            ("Аніматор", "Аніматори", "Події та допомога"),
            ("Оформлення залу", "Декор та оформлення", "Події та допомога"),
            ("Оренда обладнання", "Оренда для подій", "Події та допомога"),
            ("Супровід у справах", "Допомога у справах", "Події та допомога"),
            ("Разові доручення", "Разові завдання", "Інше")
        };

        private static readonly string[] FirstNames =
        {
            "Andrii", "Olena", "Dmytro", "Iryna", "Serhii", "Kateryna", "Oleh", "Nataliia",
            "Maksym", "Yuliia", "Vitalii", "Anna", "Roman", "Sofiia", "Taras", "Mariia",
            "Ihor", "Daria", "Bohdan", "Viktoriia"
        };

        private static readonly string[] LastNames =
        {
            "Shevchenko", "Kovalenko", "Bondarenko", "Tkachenko", "Kravchuk", "Melnyk",
            "Boiko", "Moroz", "Lysenko", "Marchenko", "Savchenko", "Rudenko"
        };

        private static readonly string[] Cities =
        {
            "Kyiv", "Lviv", "Odesa", "Kharkiv", "Dnipro", "Vinnytsia", "Poltava", "Chernivtsi"
        };

        /// <summary>Підписи під іменем виконавця в кабінеті.</summary>
        private static readonly string[] Headlines =
        {
            "Графічний дизайнер", "Копірайтер", "Фотограф", "Веброзробник",
            "Перекладач", "Маркетолог", "Ілюстратор", "Майстер з ремонту"
        };

        /// <summary>
        /// Розділи портфоліо для демонстраційних робіт.
        ///
        /// Беремо лише ті чотири, для яких у wwwroot/img/portfolio лежать знімки з макета:
        /// плитка без фото показує заглушку, і сторінка з них виглядала б напівпорожньою.
        /// У фільтрі на сторінці розділів більше — туди людина додає власні роботи.
        /// </summary>
        private static readonly string[] PortfolioGroups =
        {
            "Логотипи", "Ілюстрації", "Реклама", "Web-дизайн"
        };

        /// <summary>Назви подій календаря за типом.</summary>
        private static readonly string[] EventTitles =
        {
            "Дизайн логотипа", "Консультація", "Таргетована реклама", "Копірайтинг статті",
            "Інстаграм пост", "Ілюстрації для книги", "Переклад текстів", "Правки по макету"
        };

        /// <summary>Скільки днів показує сторінка аналітики в одному періоді.</summary>
        private const int AnalyticsDays = 30;

        /// <summary>Мітки фаху для псевдоніма: «annadesign», «annaphoto».</summary>
        private static readonly string[] NicknameTags =
        {
            "design", "photo", "text", "web", "art", "pro", "studio", "work"
        };

        /// <summary>Теми звернень у підтримку — ті самі, що у списку на формі.</summary>
        private static readonly string[] TicketTopics =
        {
            "Початок роботи", "Замовлення та оплата", "Послуги та профіль",
            "Виплата та фінанси", "Обліковий запис", "Правила та політики"
        };

        /// <summary>Категорії звернень.</summary>
        private static readonly string[] TicketKinds =
        {
            "Питання", "Проблема", "Пропозиція"
        };

        private static readonly string[] ComplaintReasons =
        {
            "Executor did not show up",
            "Poor quality of work",
            "Rude communication",
            "Price changed after agreement",
            "Suspected fraud",
            "Spam in chat"
        };

        private static readonly string[] ReviewComments =
        {
            "Everything was done on time, thank you!",
            "Good job, but arrived a bit late.",
            "Excellent specialist, will order again.",
            "Average quality for the price.",
            "Had to redo part of the work myself.",
            "Very polite and accurate, recommend.",
            "Communication could be better.",
            "Fast, cheap and neat — five stars."
        };

        private static readonly string[] ChatPhrases =
        {
            "Hello! Is this order still available?",
            "Yes, when can you start?",
            "I can be there tomorrow at 10:00.",
            "Could you send a photo of the problem?",
            "The price includes materials.",
            "Thanks, see you tomorrow!",
            "Sorry, I am running 15 minutes late.",
            "Work is finished, please confirm."
        };

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SeedOptions _options;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<SeedOptions> options,
            ILogger<DatabaseSeeder> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return;
            }

            await SeedRolesAsync();
            await SeedStaffAsync();

            // Дерево категорій — частина самого застосунку, а не демонстраційних даних,
            // тому оновлюємо його незалежно від прапорця DemoData.
            await SeedCategoriesAsync(cancellationToken);

            if (_options.DemoData)
            {
                await SeedDemoDataAsync(cancellationToken);
            }
        }

        // -----------------------------------------------------------------

        private async Task SeedRolesAsync()
        {
            foreach (var role in AppRoles.All)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    _logger.LogInformation("Seed: created role {Role}", role);
                }
            }
        }

        private async Task SeedStaffAsync()
        {
            var admin = await EnsureUserAsync(
                _options.AdminEmail,
                _options.AdminPassword,
                _options.AdminFirstName,
                _options.AdminLastName,
                AppRoles.Admin);

            if (admin != null)
            {
                _logger.LogInformation("Seed: administrator {Email} is ready", _options.AdminEmail);
            }

            if (!_options.DemoData)
            {
                return;
            }

            // Демонстраційний персонал, щоб розмежування прав за ролями можна було реально показати.
            await EnsureUserAsync("moderator@servicehub.local", "Moderator#2026", "Maria", "Moderator", AppRoles.Moderator);
            await EnsureUserAsync("support@servicehub.local", "Support#2026", "Sam", "Support", AppRoles.Support);
        }

        private async Task<ApplicationUser?> EnsureUserAsync(
            string email,
            string password,
            string firstName,
            string lastName,
            params string[] roles)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    FullName = $"{firstName} {lastName}",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    _logger.LogError(
                        "Seed: could not create {Email}: {Errors}",
                        email,
                        string.Join("; ", result.Errors.Select(e => e.Description)));
                    return null;
                }
            }

            foreach (var role in roles)
            {
                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            return user;
        }

        // -----------------------------------------------------------------
        // Демонстраційні дані
        // -----------------------------------------------------------------

        private async Task SeedDemoDataAsync(CancellationToken cancellationToken)
        {
            if (await _db.Orders.AnyAsync(cancellationToken))
            {
                return; // Never touch a database that already carries real data.
            }

            _logger.LogInformation("Seed: generating demo data…");

            var random = new Random(RandomSeed);

            var categories = await SeedCategoriesAsync(cancellationToken);
            var (customers, executors) = await SeedDemoUsersAsync(random);

            if (customers.Count == 0 || executors.Count == 0 || categories.Count == 0)
            {
                _logger.LogWarning("Seed: not enough demo users or categories, skipping orders");
                return;
            }

            await SeedOrdersAsync(random, categories, customers, executors, cancellationToken);
            await SeedCabinetAsync(random, customers, executors, cancellationToken);
            await SeedExecutorCabinetAsync(random, categories, customers, executors, cancellationToken);
            await SeedExecutorInsightsAsync(random, executors, cancellationToken);

            _logger.LogInformation("Seed: demo data ready");
        }

        /// <summary>
        /// Те, з чого складається особистий кабінет замовника: збережені адреси, гроші на
        /// рахунку, обрані виконавці. Без цього екрани кабінету виходять порожніми —
        /// самих замовлень для них замало.
        /// </summary>
        private async Task SeedCabinetAsync(
            Random random,
            List<ApplicationUser> customers,
            List<ApplicationUser> executors,
            CancellationToken cancellationToken)
        {
            if (await _db.UserAddresses.AnyAsync(cancellationToken))
            {
                return;
            }

            var addresses = new List<UserAddress>();
            var favorites = new List<Favorite>();

            foreach (var customer in customers)
            {
                customer.WalletBalance = Math.Round((decimal)(random.Next(300, 9000) + random.NextDouble()), 2);
                customer.PaymentCard = $"Visa •••• {random.Next(1000, 9999)}";
                customer.Currency = "UAH";
                customer.Language = "uk";
                customer.TimeZoneName = "Europe/Kyiv";
                customer.DateFormat = "dd.MM.yyyy";
                customer.TimeFormat = "24";
                customer.Theme = "light";
                customer.ProfileVisibility = "Registered";
                customer.ShowOnlineStatus = true;
                customer.NotifyByEmail = true;
                customer.NotifyByPush = true;

                var city = Cities[random.Next(Cities.Length)];

                addresses.Add(new UserAddress
                {
                    UserId = customer.Id,
                    Title = "Дім",
                    Kind = AddressKind.Home,
                    Line = $"вул. Левадна, {random.Next(1, 90)}, кв. {random.Next(1, 120)}",
                    City = $"м. {city}",
                    PostalCode = $"0{random.Next(1000, 9999)}",
                    Note = $"Домофон {random.Next(1, 99)}К, поверх {random.Next(1, 12)}",
                    IsPrimary = true
                });

                if (random.Next(100) < 70)
                {
                    addresses.Add(new UserAddress
                    {
                        UserId = customer.Id,
                        Title = "Офіс",
                        Kind = AddressKind.Office,
                        Line = $"вул. Хрещатик, {random.Next(1, 60)}, офіс {random.Next(100, 1200)}",
                        City = $"м. {city}",
                        PostalCode = $"0{random.Next(1000, 9999)}",
                        Note = $"Бізнес центр, поверх {random.Next(2, 20)}"
                    });
                }

                if (random.Next(100) < 45)
                {
                    addresses.Add(new UserAddress
                    {
                        UserId = customer.Id,
                        Title = "Дача",
                        Kind = AddressKind.Country,
                        Line = $"вул. Лісова {random.Next(1, 40)}",
                        City = "с. Хотянівка",
                        Region = "Київська обл.",
                        PostalCode = $"0{random.Next(1000, 9999)}",
                        Note = "Зелений паркан, перед останній будинок вулиці"
                    });
                }

                // Обране: кілька виконавців, без повторів у межах однієї людини.
                var picked = new HashSet<string>(StringComparer.Ordinal);
                var wanted = random.Next(3, 9);

                for (var i = 0; i < wanted && picked.Count < executors.Count; i++)
                {
                    var executor = executors[random.Next(executors.Count)];

                    if (picked.Add(executor.Id))
                    {
                        favorites.Add(new Favorite { UserId = customer.Id, TargetExecutorId = executor.Id });
                    }
                }
            }

            _db.UserAddresses.AddRange(addresses);
            _db.Favorites.AddRange(favorites);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seed: cabinet ready — {Addresses} addresses, {Favorites} favorites",
                addresses.Count, favorites.Count);
        }

        /// <summary>
        /// Кабінет виконавця: власні послуги, портфоліо, календар і підписи профілю.
        /// Без цього всі сім екранів виконавця стояли б порожні — самих замовлень для них замало.
        /// </summary>
        private async Task SeedExecutorCabinetAsync(
            Random random,
            List<Category> categories,
            List<ApplicationUser> customers,
            List<ApplicationUser> executors,
            CancellationToken cancellationToken)
        {
            if (await _db.ExecutorServices.AnyAsync(cancellationToken))
            {
                return;
            }

            // Послуги вішаємо на кінцеві напрями: саме там їх шукають у каталозі.
            var leaves = categories.Where(c => c.ParentCategoryId != null).ToList();

            if (leaves.Count == 0)
            {
                _logger.LogWarning("Seed: no leaf categories, skipping executor services");
                return;
            }

            var services = new List<ExecutorService>();
            var works = new List<PortfolioWork>();
            var events = new List<CalendarEvent>();

            for (var index = 0; index < executors.Count; index++)
            {
                var executor = executors[index];

                // Частині виконавців додаємо й роль замовника: у житті та сама людина і
                // виконує, і замовляє, а в кабінеті тоді працює перемикач боку.
                if (index % 6 == 0 && !await _userManager.IsInRoleAsync(executor, AppRoles.Customer))
                {
                    await _userManager.AddToRoleAsync(executor, AppRoles.Customer);
                }

                executor.Headline = Headlines[random.Next(Headlines.Length)];
                executor.ProfileViews = random.Next(18, 320);
                executor.Presence = random.Next(100) < 70 ? "online" : "away";
                executor.Experience ??= $"{random.Next(1, 9)}";

                // ── Послуги ──────────────────────────────────────────────────
                var own = new HashSet<int>();
                var wanted = random.Next(3, 9);

                for (var i = 0; i < wanted; i++)
                {
                    var category = leaves[random.Next(leaves.Count)];

                    if (!own.Add(category.Id))
                    {
                        continue;
                    }

                    // Більшість послуг опубліковані, кілька на паузі або в чернетках —
                    // щоб усі чотири плитки на екрані «Мої послуги» мали непорожні числа.
                    var roll = random.Next(100);
                    var state = roll switch
                    {
                        < 70 => ServiceState.Active,
                        < 85 => ServiceState.Paused,
                        _ => ServiceState.Draft
                    };

                    services.Add(new ExecutorService
                    {
                        OwnerId = executor.Id,
                        CategoryId = category.Id,
                        Title = category.Name,
                        Description = $"Виконую «{category.Name.ToLowerInvariant()}» під ключ. " +
                                      "Обговорюємо деталі в чаті, правки входять у вартість.",
                        FromPrice = Math.Round((decimal)(random.Next(300, 6000) / 100 * 100), 2),
                        State = state,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(5, _options.DemoHistoryDays))
                    });
                }

                // ── Портфоліо ────────────────────────────────────────────────
                var groups = PortfolioGroups
                    .OrderBy(_ => random.Next())
                    .Take(random.Next(2, 5))
                    .ToList();

                foreach (var group in groups)
                {
                    // По чотири роботи в розділі — саме стільки в рядку на макеті.
                    for (var i = 0; i < 4; i++)
                    {
                        works.Add(new PortfolioWork
                        {
                            OwnerId = executor.Id,
                            Group = group,
                            Title = $"{group}: робота {i + 1}",

                            // Вага задає і порядок розділів, і місце роботи всередині розділу.
                            Sort = PortfolioSections.Rank(group, i),
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 200))
                        });
                    }
                }
            }

            // ── Календар: тиждень навколо сьогодні для кожного виконавця ──────
            var monday = DateTime.UtcNow.Date.AddDays(-(((int)DateTime.UtcNow.DayOfWeek + 6) % 7));

            foreach (var executor in executors)
            {
                var count = random.Next(3, 9);

                for (var i = 0; i < count; i++)
                {
                    var day = monday.AddDays(random.Next(0, 7));
                    var hour = random.Next(8, 18);
                    var start = day.AddHours(hour);

                    var kind = random.Next(100) switch
                    {
                        < 50 => EventKind.Meeting,
                        < 80 => EventKind.Deadline,
                        < 93 => EventKind.Personal,
                        _ => EventKind.Busy
                    };

                    var partner = customers[random.Next(customers.Count)];

                    events.Add(new CalendarEvent
                    {
                        OwnerId = executor.Id,
                        Title = EventTitles[random.Next(EventTitles.Length)],
                        Kind = kind,
                        StartAt = start,
                        EndAt = start.AddHours(random.Next(1, 3)),
                        PartnerName = kind is EventKind.Personal or EventKind.Busy
                            ? null
                            : $"{partner.FirstName} {partner.LastName[0]}."
                    });
                }
            }

            _db.ExecutorServices.AddRange(services);
            _db.PortfolioWorks.AddRange(works);
            _db.CalendarEvents.AddRange(events);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seed: executor cabinet ready - {Services} services, {Works} works, {Events} events",
                services.Count, works.Count, events.Count);
        }

        /// <summary>
        /// Те, що показують «Аналітика», «Фінанси» та «Підтримка»: денні підсумки переглядів
        /// профілю, способи виведення коштів із кількома заявками та пара звернень у підтримку.
        ///
        /// Переглядів не вигадуємо на льоту при кожному відкритті сторінки: графік має бути
        /// той самий і завтра, і після перезапуску, інакше порівняння з попереднім періодом
        /// щоразу показувало б інші відсотки.
        /// </summary>
        private async Task SeedExecutorInsightsAsync(
            Random random,
            List<ApplicationUser> executors,
            CancellationToken cancellationToken)
        {
            if (await _db.ProfileVisits.AnyAsync(cancellationToken))
            {
                return;
            }

            // Два періоди по 30 днів: поточний і попередній, із яким порівнюємо.
            const int days = AnalyticsDays * 2;

            var today = DateTime.UtcNow.Date;
            var visits = new List<ProfileVisit>(executors.Count * days);
            var methods = new List<PayoutMethod>();
            var payouts = new List<Payout>();
            var tickets = new List<SupportTicket>();

            for (var index = 0; index < executors.Count; index++)
            {
                var executor = executors[index];

                executor.Nickname ??= Nickname(executor, index);

                // Базовий рівень у кожного свій, плюс повільне зростання й денні коливання:
                // так на графіку видно тренд, а не рівний частокіл.
                var baseLine = random.Next(14, 46);
                var growth = (random.NextDouble() * 0.7) + 0.1;
                var week = 0;

                for (var back = days - 1; back >= 0; back--)
                {
                    var day = today.AddDays(-back);
                    var position = days - 1 - back;

                    // У вихідні замовників менше — провали на графіку не випадкові.
                    var weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                    var total = (int)Math.Round(baseLine + position * growth);
                    total = Math.Max(3, total + random.Next(-6, 9) - (weekend ? 7 : 0));

                    // Частки джерел тримаємо близькими до макета: пошук, каталог, прямі, поради.
                    var search = (int)Math.Round(total * 0.42);
                    var catalog = (int)Math.Round(total * 0.28);
                    var direct = (int)Math.Round(total * 0.18);
                    var referral = (int)Math.Round(total * 0.08);
                    var other = Math.Max(0, total - search - catalog - direct - referral);

                    visits.Add(new ProfileVisit
                    {
                        OwnerId = executor.Id,
                        Day = day,
                        FromSearch = search,
                        FromCatalog = catalog,
                        FromDirect = direct,
                        FromReferral = referral,
                        FromOther = other,
                        PortfolioViews = (int)Math.Round(total * (0.5 + random.NextDouble() * 0.4))
                    });

                    if (back < 7)
                    {
                        week += total;
                    }
                }

                // Віджет під меню показує тиждень — беремо те саме число, що й графік.
                executor.ProfileViews = week;

                // ── Способи виведення ────────────────────────────────────────
                var card = new PayoutMethod
                {
                    OwnerId = executor.Id,
                    Kind = PayoutKind.Card,
                    Label = $"Visa ****** {random.Next(1000, 9999)}",
                    Note = "основна карта",
                    IsPrimary = true,
                    CreatedAt = today.AddDays(-random.Next(60, 400))
                };

                methods.Add(card);

                if (index % 3 == 0)
                {
                    methods.Add(new PayoutMethod
                    {
                        OwnerId = executor.Id,
                        Kind = PayoutKind.PayPal,
                        Label = $"{executor.Nickname}@gmail.com",
                        Note = "Pay Pal",
                        CreatedAt = today.AddDays(-random.Next(30, 300))
                    });
                }

                // ── Заявки на виплату ────────────────────────────────────────
                for (var i = 0; i < random.Next(1, 4); i++)
                {
                    var created = today.AddDays(-random.Next(2, AnalyticsDays * 2));
                    var done = created < today.AddDays(-3);

                    payouts.Add(new Payout
                    {
                        OwnerId = executor.Id,
                        Method = card,
                        MethodLabel = card.Label,
                        Amount = random.Next(10, 60) * 100,
                        Status = done ? PayoutStatus.Paid : PayoutStatus.Pending,
                        CreatedAt = created,
                        PaidAt = done ? created.AddDays(random.Next(1, 3)) : null
                    });
                }

                // ── Звернення в підтримку ────────────────────────────────────
                if (index % 5 == 0)
                {
                    var asked = today.AddDays(-random.Next(1, 40));
                    var answered = asked < today.AddDays(-2);

                    tickets.Add(new SupportTicket
                    {
                        OwnerId = executor.Id,
                        Topic = TicketTopics[random.Next(TicketTopics.Length)],
                        Category = TicketKinds[random.Next(TicketKinds.Length)],
                        Message = "Доброго дня! Підкажіть, будь ласка, як правильно вчинити — " +
                                  "у кабінеті не бачу потрібної кнопки.",
                        Status = answered ? TicketStatus.Answered : TicketStatus.New,
                        CreatedAt = asked,
                        AnsweredAt = answered ? asked.AddHours(random.Next(1, 9)) : null
                    });
                }
            }

            _db.ProfileVisits.AddRange(visits);
            _db.PayoutMethods.AddRange(methods);
            _db.Payouts.AddRange(payouts);
            _db.SupportTickets.AddRange(tickets);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Seed: executor insights ready - {Visits} days, {Methods} payout methods, {Payouts} payouts, {Tickets} tickets",
                visits.Count, methods.Count, payouts.Count, tickets.Count);
        }

        /// <summary>
        /// Псевдонім виду «annadesign»: латиниця з пошти плюс коротка мітка фаху.
        /// Унікальності не доводимо — це підпис у профілі, а не другий логін.
        /// </summary>
        private static string Nickname(ApplicationUser executor, int index)
        {
            var mail = executor.Email ?? string.Empty;
            var at = mail.IndexOf('@');
            var head = at > 0 ? mail[..at] : executor.FirstName;

            var letters = new string(head.Where(char.IsLetter).ToArray()).ToLowerInvariant();

            if (letters.Length == 0)
            {
                letters = "vybir";
            }

            return letters + NicknameTags[index % NicknameTags.Length];
        }

        /// <summary>
        /// Доводить дерево категорій до того, що намальовано в макеті. Працює і на порожній,
        /// і на вже заповненій базі: спершу перейменовує те, що дизайнер назвав інакше,
        /// потім дописує відсутнє. Нічого не видаляє — категорії, яких немає в макеті,
        /// міг додати адміністратор, і на них можуть висіти замовлення.
        /// </summary>
        private async Task<List<Category>> SeedCategoriesAsync(CancellationToken cancellationToken)
        {
            var known = await _db.Categories.ToListAsync(cancellationToken);

            foreach (var (from, to, parent) in CategoryRenames)
            {
                int? parentId = parent == null
                    ? null
                    : known.FirstOrDefault(c => c.ParentCategoryId == null && SameName(c.Name, parent))?.Id;

                // Напрям указано, а його в базі ще немає — нічого перейменовувати.
                if (parent != null && parentId == null)
                {
                    continue;
                }

                var category = known.FirstOrDefault(c =>
                    SameName(c.Name, from) && (parent == null || c.ParentCategoryId == parentId));

                // Якщо назва вже нова або таку вже хтось створив поруч — не чіпаємо.
                if (category == null
                    || known.Any(c => c != category && c.ParentCategoryId == category.ParentCategoryId && SameName(c.Name, to)))
                {
                    continue;
                }

                _logger.LogInformation("Seed: renamed category {From} to {To}", category.Name, to);

                if (category.ParentCategoryId == null && IsGeneratedDescription(category.Description, category.Name))
                {
                    category.Description = RootDescription(to);
                }

                category.Name = to;
            }

            await _db.SaveChangesAsync(cancellationToken);

            var leaves = new List<Category>();

            foreach (var rootName in CatalogTree.Roots)
            {
                var rootDescription = CatalogTree.Description(rootName);
                if (rootDescription.Length == 0)
                {
                    rootDescription = RootDescription(rootName);
                }

                var root = known.FirstOrDefault(c => c.ParentCategoryId == null && SameName(c.Name, rootName));

                if (root == null)
                {
                    root = new Category { Name = rootName, Description = rootDescription, IsActive = true };
                    _db.Categories.Add(root);
                    await _db.SaveChangesAsync(cancellationToken);
                    known.Add(root);
                    _logger.LogInformation("Seed: added category {Name}", rootName);
                }
                else if (IsGeneratedDescription(root.Description, root.Name))
                {
                    // Опис із макета ставимо лише замість автоматичного: те, що
                    // адміністратор написав руками, лишається недоторканим.
                    root.Description = rootDescription;
                }

                var childNames = CatalogTree.Children(rootName);

                foreach (var childName in childNames)
                {
                    var child = known.FirstOrDefault(c => c.ParentCategoryId == root.Id && SameName(c.Name, childName));

                    if (child == null)
                    {
                        child = new Category
                        {
                            Name = childName,
                            Description = $"{childName} — напрям «{root.Name}»",
                            ParentCategoryId = root.Id,
                            IsActive = true
                        };

                        _db.Categories.Add(child);
                        known.Add(child);
                    }

                    leaves.Add(child);
                }

                RetireMissingChildren(root, known, childNames);

                await _db.SaveChangesAsync(cancellationToken);
            }

            return leaves;
        }

        /// <summary>
        /// Ховає підкатегорії, яких більше немає в макеті. Не видаляє: на них можуть висіти
        /// замовлення й відгуки. І чіпає лише ті, що створив сам сидер — упізнаємо їх за описом,
        /// який він же й написав. Якщо адміністратор додав свою категорію або змінив опис,
        /// вона лишається недоторканою.
        /// </summary>
        private void RetireMissingChildren(Category root, List<Category> known, IReadOnlyList<string> wanted)
        {
            foreach (var child in known.Where(c => c.ParentCategoryId == root.Id && c.IsActive))
            {
                if (wanted.Any(name => SameName(name, child.Name))
                    || !IsGeneratedChildDescription(child.Description, child.Name))
                {
                    continue;
                }

                child.IsActive = false;
                _logger.LogInformation("Seed: hid category {Name} - it is no longer in the catalogue", child.Name);
            }
        }

        private static bool IsGeneratedChildDescription(string? description, string name) =>
            description != null && description.StartsWith($"{name} — напрям «", StringComparison.Ordinal);

        private static string RootDescription(string name) => $"Послуги напряму «{name}»";

        private static bool IsGeneratedDescription(string? description, string name) =>
            string.IsNullOrWhiteSpace(description) || description == RootDescription(name);

        /// <summary>
        /// Порівняння назв без огляду на тип апострофа: у макеті стоїть ’, у базі — '.
        /// </summary>
        private static bool SameName(string left, string right) =>
            string.Equals(left.Replace('\u2019', '\''), right.Replace('\u2019', '\''),
                          StringComparison.OrdinalIgnoreCase);

        private async Task<(List<ApplicationUser> Customers, List<ApplicationUser> Executors)> SeedDemoUsersAsync(Random random)
        {
            var customers = new List<ApplicationUser>();
            var executors = new List<ApplicationUser>();

            var total = Math.Clamp(_options.DemoUsers, 6, 300);

            for (var i = 0; i < total; i++)
            {
                var firstName = FirstNames[random.Next(FirstNames.Length)];
                var lastName = LastNames[random.Next(LastNames.Length)];
                var isExecutor = i % 2 == 1;
                var email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}{i}@example.com";

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = random.Next(10) > 1,
                        FirstName = firstName,
                        LastName = lastName,
                        FullName = $"{firstName} {lastName}",
                        PhoneNumber = $"+3806{random.Next(1000000, 9999999)}",
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, _options.DemoHistoryDays))
                    };

                    var result = await _userManager.CreateAsync(user, "User#2026");
                    if (!result.Succeeded)
                    {
                        continue;
                    }

                    await _userManager.AddToRoleAsync(user, isExecutor ? AppRoles.Executor : AppRoles.Customer);

                    // Кілька заблокованих акаунтів роблять екрани модерації змістовними.
                    if (i % 17 == 0)
                    {
                        user.LockoutEnabled = true;
                        user.LockoutEnd = DateTimeOffset.UtcNow.AddDays(random.Next(3, 30));
                        await _userManager.UpdateAsync(user);
                    }
                }

                if (isExecutor)
                {
                    executors.Add(user);
                }
                else
                {
                    customers.Add(user);
                }
            }

            return (customers, executors);
        }

        private async Task SeedOrdersAsync(
            Random random,
            List<Category> categories,
            List<ApplicationUser> customers,
            List<ApplicationUser> executors,
            CancellationToken cancellationToken)
        {
            var orderCount = Math.Clamp(_options.DemoOrders, 10, 2000);
            var historyDays = Math.Clamp(_options.DemoHistoryDays, 30, 720);

            var orders = new List<Order>(orderCount);

            var showcase = categories
                .Where(c => ShowcaseServices.Any(name => SameName(name, c.Name)))
                .ToList();

            for (var i = 0; i < orderCount; i++)
            {
                // Кожне п'яте замовлення — на послугу з вітрини головної сторінки.
                var category = showcase.Count > 0 && random.Next(5) == 0
                    ? showcase[random.Next(showcase.Count)]
                    : categories[random.Next(categories.Count)];
                var customer = customers[random.Next(customers.Count)];
                var createdAt = DateTime.UtcNow.AddDays(-random.Next(0, historyDays)).AddHours(-random.Next(0, 24));

                // Ваги підібрані так, щоб воронка виглядала правдоподібно: більшість замовлень зрештою виконані.
                var roll = random.Next(100);
                var status = roll switch
                {
                    < 18 => OrderStatus.Pending,
                    < 33 => OrderStatus.InProgress,
                    < 88 => OrderStatus.Completed,
                    _ => OrderStatus.Cancelled
                };

                var needsExecutor = status is OrderStatus.InProgress or OrderStatus.Completed;
                var executor = needsExecutor ? executors[random.Next(executors.Count)] : null;

                orders.Add(new Order
                {
                    Title = $"{category.Name} in {Cities[random.Next(Cities.Length)]}",
                    Description = $"Need a specialist for \"{category.Name.ToLowerInvariant()}\". " +
                                  "Details are discussed in the chat, materials are on the customer.",
                    Price = Math.Round((decimal)(random.Next(250, 12000) + random.NextDouble()), 2),
                    Status = status,
                    Address = $"{Cities[random.Next(Cities.Length)]}, vul. Soborna {random.Next(1, 120)}",
                    CreatedAt = createdAt,
                    ExecutionAt = status == OrderStatus.Pending ? null : createdAt.AddDays(random.Next(1, 10)),
                    CategoryId = category.Id,
                    CustomerId = customer.Id,
                    ExecutorId = executor?.Id
                });
            }

            _db.Orders.AddRange(orders);
            await _db.SaveChangesAsync(cancellationToken);

            var applications = new List<Application>();
            var payments = new List<Payment>();
            var reviews = new List<Review>();
            var messages = new List<OrderMessage>();
            var notifications = new List<Notification>();

            foreach (var order in orders)
            {
                // Відгуки виконавців, які не отримали це замовлення.
                var applicationCount = random.Next(0, 5);
                for (var a = 0; a < applicationCount; a++)
                {
                    var applicant = executors[random.Next(executors.Count)];
                    if (applicant.Id == order.ExecutorId)
                    {
                        continue;
                    }

                    applications.Add(new Application
                    {
                        OrderId = order.Id,
                        ExecutorId = applicant.Id,
                        ProposedPrice = Math.Round(order.Price * (decimal)(0.8 + random.NextDouble() * 0.5), 2),
                        Days = random.Next(1, 15),
                        Comment = "I can take this order, I have done similar work before.",
                        CreatedAt = order.CreatedAt.AddHours(random.Next(1, 48)),

                        // Поки замовлення чекає виконавця, відгуки на нього теж не розглянуті —
                        // інакше на сторінці деталей ніколи не було б кнопок «Прийняти»/«Відхилити».
                        Status = order.Status == OrderStatus.Pending
                            ? ApplicationStatus.Pending
                            : ApplicationStatus.Rejected
                    });
                }

                if (order.ExecutorId != null)
                {
                    applications.Add(new Application
                    {
                        OrderId = order.Id,
                        ExecutorId = order.ExecutorId,
                        ProposedPrice = order.Price,
                        Days = random.Next(1, 10),
                        Comment = "Ready to start right away.",
                        CreatedAt = order.CreatedAt.AddHours(random.Next(1, 24)),
                        Status = ApplicationStatus.Accepted
                    });
                }

                if (order.Status == OrderStatus.Completed)
                {
                    var paidAt = (order.ExecutionAt ?? order.CreatedAt).AddHours(random.Next(1, 48));
                    if (paidAt > DateTime.UtcNow)
                    {
                        paidAt = DateTime.UtcNow;
                    }

                    payments.Add(new Payment
                    {
                        OrderId = order.Id,
                        UserId = order.CustomerId,
                        Amount = order.Price,
                        Currency = "UAH",
                        Provider = (PaymentProvider)random.Next(0, 3),
                        ExternalTransactionId = $"TX-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        Status = random.Next(100) < 92 ? PaymentStatus.Completed : PaymentStatus.Refunded,
                        CreatedAt = paidAt.AddMinutes(-random.Next(5, 240)),
                        PaidAt = paidAt
                    });

                    // На більшість виконаних замовлень замовник залишає відгук.
                    if (random.Next(100) < 70 && order.ExecutorId != null)
                    {
                        var reviewedAt = paidAt.AddHours(random.Next(1, 72));
                        if (reviewedAt > DateTime.UtcNow)
                        {
                            reviewedAt = DateTime.UtcNow;
                        }

                        // Розподіл як на живих майданчиках: більшість оцінок 4–5,
                        // низькі трапляються рідко. Попередній набір давав середню 3,6,
                        // і навіть найпопулярніші послуги виглядали посередніми.
                        var rating = random.Next(100) switch
                        {
                            < 2 => 1,
                            < 5 => 2,
                            < 11 => 3,
                            < 30 => 4,
                            _ => 5
                        };

                        reviews.Add(new Review
                        {
                            OrderId = order.Id,
                            AuthorId = order.CustomerId,
                            TargetUserId = order.ExecutorId,
                            Rating = rating,
                            Comment = ReviewComments[random.Next(ReviewComments.Length)],
                            CreatedAt = reviewedAt
                        });
                    }
                }
                else if (order.Status == OrderStatus.InProgress && random.Next(100) < 40)
                {
                    var startedAt = order.CreatedAt.AddHours(random.Next(1, 72));
                    if (startedAt > DateTime.UtcNow)
                    {
                        startedAt = DateTime.UtcNow;
                    }

                    payments.Add(new Payment
                    {
                        OrderId = order.Id,
                        UserId = order.CustomerId,
                        Amount = order.Price,
                        Currency = "UAH",
                        Provider = (PaymentProvider)random.Next(0, 3),
                        ExternalTransactionId = $"TX-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                        Status = random.Next(100) < 80 ? PaymentStatus.Pending : PaymentStatus.Failed,
                        CreatedAt = startedAt
                    });
                }

                // Листування приблизно на половині замовлень, у яких є виконавець.
                if (order.ExecutorId != null && random.Next(100) < 55)
                {
                    var messageCount = random.Next(2, 9);
                    var sentAt = order.CreatedAt.AddHours(random.Next(1, 12));

                    for (var m = 0; m < messageCount; m++)
                    {
                        var fromCustomer = m % 2 == 0;
                        sentAt = sentAt.AddMinutes(random.Next(2, 180));

                        if (sentAt > DateTime.UtcNow)
                        {
                            break;
                        }

                        messages.Add(new OrderMessage
                        {
                            OrderId = order.Id,
                            SenderId = fromCustomer ? order.CustomerId : order.ExecutorId,
                            Text = ChatPhrases[random.Next(ChatPhrases.Length)],
                            SentAt = sentAt,
                            IsRead = m < messageCount - 2
                        });
                    }
                }

                if (random.Next(100) < 12)
                {
                    notifications.Add(new Notification
                    {
                        UserId = order.CustomerId,
                        Title = "Order update",
                        Message = $"The status of order \"{order.Title}\" changed to {order.Status}.",
                        IsRead = random.Next(100) < 60,
                        CreatedAt = order.CreatedAt.AddHours(random.Next(1, 100))
                    });
                }
            }

            _db.Applications.AddRange(applications);
            _db.Payments.AddRange(payments);
            _db.Reviews.AddRange(reviews);
            _db.OrderMessages.AddRange(messages);
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync(cancellationToken);

            await SeedComplaintsAsync(random, customers, executors, cancellationToken);
            await SeedStatisticsAsync(cancellationToken);
        }

        private async Task SeedComplaintsAsync(
            Random random,
            List<ApplicationUser> customers,
            List<ApplicationUser> executors,
            CancellationToken cancellationToken)
        {
            if (await _db.Complaints.AnyAsync(cancellationToken))
            {
                return;
            }

            var complaints = new List<Complaint>();
            var count = Math.Max(8, _options.DemoOrders / 8);

            for (var i = 0; i < count; i++)
            {
                var sender = customers[random.Next(customers.Count)];
                var target = executors[random.Next(executors.Count)];

                complaints.Add(new Complaint
                {
                    SenderId = sender.Id,
                    TargetUserId = target.Id,
                    Reason = ComplaintReasons[random.Next(ComplaintReasons.Length)],
                    Description = "Please look into this, the situation repeated more than once.",
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, _options.DemoHistoryDays)),
                    Status = random.Next(100) switch
                    {
                        < 45 => ComplaintStatus.Pending,
                        < 80 => ComplaintStatus.Resolved,
                        _ => ComplaintStatus.Rejected
                    }
                });
            }

            _db.Complaints.AddRange(complaints);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Заповнює таблицю Statistics зі згенерованих замовлень і платежів, щоб точки статистики
        /// мобільного API віддавали ті самі числа, які показує панель.
        /// </summary>
        private async Task SeedStatisticsAsync(CancellationToken cancellationToken)
        {
            if (await _db.Statistics.AnyAsync(cancellationToken))
            {
                return;
            }

            var from = DateTime.UtcNow.Date.AddDays(-Math.Clamp(_options.DemoHistoryDays, 30, 720));

            var orders = await _db.Orders
                .Where(o => o.CreatedAt >= from)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(o => o.Status == OrderStatus.Completed)
                })
                .ToListAsync(cancellationToken);

            var revenue = await _db.Payments
                .Where(p => p.Status == PaymentStatus.Completed && p.PaidAt != null && p.PaidAt >= from)
                .GroupBy(p => p.PaidAt!.Value.Date)
                .Select(g => new { Day = g.Key, Amount = g.Sum(p => p.Amount) })
                .ToListAsync(cancellationToken);

            var users = await _db.Users
                .Where(u => u.CreatedAt >= from)
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var days = orders.Select(o => o.Day)
                .Union(revenue.Select(r => r.Day))
                .Union(users.Select(u => u.Day))
                .Distinct()
                .OrderBy(d => d);

            var statistics = days.Select(day => new Statistic
            {
                Date = day,
                TotalOrders = orders.FirstOrDefault(o => o.Day == day)?.Total ?? 0,
                CompletedOrders = orders.FirstOrDefault(o => o.Day == day)?.Completed ?? 0,
                TotalRevenue = revenue.FirstOrDefault(r => r.Day == day)?.Amount ?? 0m,
                NewUsersCount = users.FirstOrDefault(u => u.Day == day)?.Count ?? 0
            }).ToList();

            _db.Statistics.AddRange(statistics);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
