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

            _logger.LogInformation("Seed: demo data ready");
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
                        Comment = "I can take this order, I have done similar work before.",
                        CreatedAt = order.CreatedAt.AddHours(random.Next(1, 48)),
                        Status = ApplicationStatus.Rejected
                    });
                }

                if (order.ExecutorId != null)
                {
                    applications.Add(new Application
                    {
                        OrderId = order.Id,
                        ExecutorId = order.ExecutorId,
                        ProposedPrice = order.Price,
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
