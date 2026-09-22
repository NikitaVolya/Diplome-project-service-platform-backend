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

        // Дерево каталогу в тому порядку, як намальовано в макеті сторінки «Каталог послуг».
        // Рядок: назва напряму | опис для шапки панелі | підкатегорії через «;».
        // Крапка з комою, а не кома, бо назви самі містять коми («Вікна, двері та скло»).
        private static readonly string[] CategoryTree =
        {
            "Дім і ремонт|Будівельні роботи, монтаж та встановлення|Сантехнічні роботи;Електромонтажні роботи;Ремонт квартир і будинків;Будівельні роботи;Меблі та столярні роботи;Вікна, двері та скло;Покрівля та фасади;Опалення та водопостачання;Встановлення та ремонт техніки;Системи безпеки та розумний дім;Оздоблювальні роботи;Ландшафтні та зовнішні роботи;Домашній майстер;Вентиляція та кондиціонування;Інші роботи",
            "Прибирання|Регулярне, генеральне та післяремонтне прибирання|Прибирання квартири;Прибирання офісу;Миття вікон;Після ремонту;Генеральне прибирання;Хімчистка меблів;Хімчистка килимів;Миття фасадів;Прибирання після свята;Дезінфекція;Прання та прасування;Прибирання подвір'я;Вивезення сміття;Догляд за басейном;Миття посуду на захід",
            "Доставка|Кур'єри, вантажні перевезення та переїзди|Кур'єрська доставка;Доставка їжі;Доставка документів;Доставка квітів;Доставка ліків;Вантажні перевезення;Переїзд квартири;Переїзд офісу;Послуги вантажників;Доставка меблів;Доставка техніки;Доставка з магазину;Термінова доставка;Міжміська доставка;Зберігання речей",
            "Краса та догляд|Перукарі, майстри манікюру та косметологи|Стрижка;Манікюр;Педикюр;Макіяж;Косметологія;Фарбування волосся;Зачіски та укладки;Брови та вії;Депіляція;Догляд за обличчям;Масаж обличчя;Барбершоп;Нарощування нігтів;Весільний образ;Виїзд майстра додому",
            "Спорт та здоров'я|Тренери, масажисти та фахівці з відновлення|Масаж;Персональний тренер;Йога;Дієтолог;Лікувальна фізкультура;Плавання;Бокс та єдиноборства;Пілатес;Розтяжка;Реабілітація після травм;Тренування вдома;Групові заняття;Психолог;Догляд за хворими;Медсестра вдома",
            "IT та розробка|Сайти, застосунки, мережі та підтримка техніки|Ремонт комп'ютерів;Налаштування ПЗ;Мережі;Розробка сайтів;Мобільні застосунки;Верстка сайтів;Тестування;Боти та автоматизація;Бази даних;Системне адміністрування;Відновлення даних;Кібербезпека;Хмарні сервіси;Технічна підтримка;Обліковi системи",
            "Дизайн і творчість|Графіка, інтер'єри та ручна робота|Дизайн логотипу;Вебдизайн;Ілюстрація;Поліграфія;Дизайн інтер'єру;Брендинг;Моушн-дизайн;3D-моделювання;Дизайн упаковки;Презентації;Банери та афіші;Рукоділля;Малювання на замовлення;Каліграфія;Ретуш зображень",
            "Фото та відео|Зйомка, монтаж та обробка матеріалів|Фотозйомка;Відеозйомка;Обробка фото;Аерозйомка;Весільна зйомка;Предметна зйомка;Репортажна зйомка;Відеомонтаж;Студійна зйомка;Сімейна фотосесія;Зйомка інтер'єрів;Кольорокорекція;Оцифрування плівок;Стріми та трансляції;Озвучення",
            "Бізнес-послуги|Бухгалтерія, юристи та супровід підприємців|Бухгалтерія;Юридичні консультації;Реєстрація бізнесу;Податкова звітність;Кадровий облік;Бізнес-план;Аудит;Оцінка майна;Складання договорів;Представництво в суді;Аналітика;Фінансове планування;Страхування;Патенти та торгові марки;Переклад документів",
            "Автопослуги|Ремонт, обслуговування та догляд за авто|Шиномонтаж;Мийка авто;Діагностика;Вантажні перевезення;Ремонт двигуна;Заміна масла;Автоелектрика;Кузовний ремонт;Полірування;Хімчистка салону;Евакуатор;Тонування;Встановлення сигналізації;Підбір авто;Допомога на дорозі",
            "Маркетинг і реклама|Просування, реклама та робота з аудиторією|Просування в соцмережах;Контекстна реклама;SEO-просування;Таргетована реклама;Email-розсилки;Копірайтинг;Контент-план;Робота з блогерами;Зовнішня реклама;Поліграфічна реклама;Маркетингова стратегія;Аналітика реклами;Нейминг;Відеореклама;Реклама на маркетплейсах",
            "Освіта і розвиток|Репетитори, курси та підготовка до іспитів|Математика;Англійська мова;Програмування;Переклади;Українська мова;Хімія та біологія;Фізика;Підготовка до НМТ;Музика;Малювання;Танці;Водіння;Курси кулінарії;Підготовка до співбесіди;Логопед",
            "Послуги для тварин|Догляд, вигул і ветеринарна допомога|Вигул собак;Грумінг;Ветеринар;Передержка;Дресирування;Стрижка кігтів;Зоотаксі;Догляд за котами;Купання тварин;Кінолог;Ветеринар вдома;Чипування;Підбір корму;Прибирання за тваринами;Фотосесія з улюбленцем",
            "Події та допомога|Свята, організація заходів і поміч у справах|Організація свята;Ведучий;Аніматор;Оформлення залу;Кейтеринг;Оренда обладнання;Музиканти;Фотозона;Торти на замовлення;Організація весілля;Корпоративи;Волонтерська допомога;Допомога літнім;Супровід у справах;Дрібні доручення",
            "Діти та догляд|Няні, репетитори та догляд за близькими|Няня;Аніматор;Підготовка до школи;Догляд за літніми;Репетитор молодших класів;Супровід дитини;Догляд за немовлям;Дитячий психолог;Розвивальні заняття;Логопед для дітей;Няня на годину;Гувернантка;Допомога з уроками;Дитячі свята;Догляд за маломобільними",
            "Інше|Послуги, що не увійшли до інших напрямів|Складання меблів;Помічник по господарству;Розбирання завалів;Пошук майстра;Разові доручення;Інші послуги"
        };

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
            "Сантехнічні роботи", "Прибирання квартири", "Дизайн логотипу", "Електромонтажні роботи"
        };

        private static readonly (string From, string To)[] CategoryRenames =
        {
            ("Дизайн та творчість", "Дизайн і творчість"),
            ("Авто і транспорт", "Автопослуги"),
            ("IT та технології", "IT та розробка"),
            ("Освіта та репетиторство", "Освіта і розвиток"),
            ("Здоров'я та спорт", "Спорт та здоров'я"),
            ("Тварини", "Послуги для тварин"),
            ("Сантехнік", "Сантехнічні роботи"),
            ("Електрик", "Електромонтажні роботи"),
            ("Малярні роботи", "Оздоблювальні роботи"),
            ("Збірка меблів", "Меблі та столярні роботи")
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

            foreach (var (from, to) in CategoryRenames)
            {
                var category = known.FirstOrDefault(c => SameName(c.Name, from));

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

            foreach (var line in CategoryTree)
            {
                var parts = line.Split('|');
                var rootName = parts[0].Trim();
                var rootDescription = parts.Length > 1 ? parts[1].Trim() : RootDescription(rootName);

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

                var childNames = parts.Length > 2
                    ? parts[2].Split(';', StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>();

                foreach (var raw in childNames)
                {
                    var childName = raw.Trim();
                    if (childName.Length == 0)
                    {
                        continue;
                    }

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

                await _db.SaveChangesAsync(cancellationToken);
            }

            return leaves;
        }

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
