# ServiceHub — вебпанель адміністрування

Адмінпанель на ASP.NET Core 8 MVC (обсяг робіт Software Developer 4). Вона використовує вже
наявні шари `Domain` / `DAL` / `BLL` і працює **окремим застосунком** від REST API для мобільного
клієнта, тому їх можна розгортати й оновлювати незалежно одне від одного.

---

## Звідки беруться дані

**Короткa відповідь: усі дані — з тієї самої бази `KabanchikDB`, з якою працює мобільний API.**
Панель не має власного сховища, не тримає копій і нічого не вигадує: вона читає ті самі таблиці,
які наповнює застосунок.

Шлях даних завжди однаковий:

```
База даних KabanchikDB (SQL Server)
        ↓
ApplicationDbContext            DAL/Context/ApplicationDbContext.cs
        ↓
сервіси адмінки                 BLL/Admin/Services/*.cs      ← тут усі запити
        ↓
контролер                       AdminPanel/Controllers/*.cs
        ↓
представлення (сторінка)        AdminPanel/Views/*.cshtml
```

### Яка сторінка які таблиці читає

| Сторінка панелі | Таблиці в базі | Сервіс, що робить запит |
|---|---|---|
| Дашборд, Статистика | `Orders`, `Payments`, `AspNetUsers`, `Reviews`, `Complaints`, `Categories` | `AdminDashboardService` |
| Користувачі | `AspNetUsers`, `AspNetUserRoles`, `AspNetRoles`, `Orders`, `Reviews`, `Payments`, `Complaints` | `AdminUserService` |
| Замовлення | `Orders`, `Categories`, `Applications`, `Payments`, `Reviews`, `OrderMessages` | `AdminOrderService` |
| Категорії | `Categories`, `Orders` | `AdminCategoryService` |
| Скарги, Відгуки, Черга модерації | `Complaints`, `Reviews`, `Orders`, `Payments`, `AspNetUsers` | `AdminModerationService` |
| Платежі | `Payments`, `Orders`, `AspNetUsers` | `AdminPaymentService` |
| Чат | `OrderMessages`, `Orders`, `AspNetUsers` | `AdminChatService` |
| Журнал дій | `AuditLogs` | `AuditLogService` |

### Що саме я додав до бази

Одну нову таблицю — **`AuditLogs`** (журнал дій адміністраторів). Її створює міграція
`DAL/Migrations/20260828092911_AddAuditLog.cs`, а сутність описана в `Domain/Models/AuditLog.cs`.
Решта таблиць — ті, що вже були в проєкті; я їх лише читаю та оновлюю, схему не змінював.

### Числа на дашборді рахуються на льоту

Дашборд **не** читає готові підсумки з таблиці `Statistics`. Кожне число — це окремий запит
до `Orders`, `Payments` чи `AspNetUsers` у момент відкриття сторінки (`AdminDashboardService`).
Так зроблено навмисно: панель показує правильні цифри навіть тоді, коли фонове завдання
підрахунку статистики ще не відпрацювало.

### Звідки беруться дані для показу (демо)

У порожній базі показувати нічого, тому є `DAL/Seed/DatabaseSeeder.cs`. Під час запуску він:

1. створює ролі `Admin`, `Moderator`, `Support`, `Customer`, `Executor`;
2. створює акаунт адміністратора з секції `Seed` у `appsettings.json`;
3. якщо ввімкнено `Seed:DemoData` **і в базі немає жодного замовлення** — генерує демо-дані:
   категорії, ~40 користувачів, 160 замовлень, відгуки виконавців, платежі, відгуки, скарги
   та листування.

Генератор має фіксоване зерно (`RandomSeed = 20260828`), тому на будь-якій машині вийдуть
однакові дані. **Якщо в базі вже є замовлення — сидер не чіпає нічого**, крім ролей і акаунта
адміністратора. Демо-дані ввімкнені лише у файлі `appsettings.Development.json`.

### Де ще щось зберігається

- **Сесія адміністратора** — у cookie браузера `ServiceHub.Admin` (8 годин).
- **Лічильники біля пунктів меню** — у пам'яті застосунку, 30 секунд
  (`AdminPanel/Services/NavigationBadgeService.cs`).
- **Файли Excel** — ніде не зберігаються: формуються в пам'яті й одразу віддаються в браузер.
- Ніяких інших сховищ, кешів чи зовнішніх сервісів панель не використовує.

---

## Що реалізовано

| Вимога із завдання | Де шукати |
|---|---|
| Dashboard | `Controllers/DashboardController.cs`, `Views/Dashboard/Index.cshtml` |
| Statistics | `Controllers/StatisticsController.cs` — період, розбивка по днях, вивантаження |
| Users | список, фільтри, профіль, блокування, м'яке видалення, призначення ролей |
| Orders | список, фільтри, картка замовлення, зміна статусу, видалення |
| Complaints | список, картка, вирішити / відхилити, дії щодо порушника |
| Categories | дерево у два рівні, створення / редагування, приховування, видалення |
| Reviews | список, фільтр за оцінкою, видалення |
| Payments | список, підсумки за статусами, картка, позначка про повернення |
| Moderation | єдина черга всього, що чекає на рішення |
| SignalR Chat | `Hubs/AdminChatHub.cs`, `Views/Chat/Index.cshtml` |
| Admin UI | Bootstrap 5, власний `wwwroot/css/admin.css`, адаптивне бічне меню |
| Charts | Chart.js, обгортка `wwwroot/js/admin.js` (`AdminCharts.line/bar/doughnut`) |
| Role Management | `Controllers/RolesController.cs` + ролі в картці користувача |
| Logs Viewer | `Controllers/LogsController.cs` над таблицею `AuditLogs` |
| Export Excel | `BLL/Admin/Services/ExcelExportService.cs` — кожен список вивантажується у `.xlsx` |
| Responsive Layout | меню згортається на екранах вужче 992 px, таблиці прокручуються |

---

## Структура проєкту

```
Domain            сутності, переліки, константи ролей (AppRoles)
  DAL             ApplicationDbContext, репозиторії, міграції, DatabaseSeeder
    BLL           наявні сервіси + BLL/Admin/* (запити адмінки, журнал, Excel)
      API         REST API для мобільного застосунку   (JWT)
      AdminPanel  цей проєкт — вебпанель               (cookie)
```

Контролери навмисно тонкі: прийняти фільтр, викликати один сервіс, віддати одне представлення.
Уся робота з базою — у `BLL/Admin/*`.

**Чому окремий проєкт.** Мобільний API автентифікується токенами, панелі потрібні cookie,
захист від CSRF і Razor. Змішування двох схем в одному застосунку ускладнює налаштування безпеки
й прив'язує релізи команди одне до одного.

---

## Як запустити

1. **Рядок підключення** — `appsettings.json`, ключ `ConnectionStrings:DefaultConnection`.
   За замовчуванням це SQL Server LocalDB і база `KabanchikDB`, та сама, що в API.

2. **Запустити** проєкт `AdminPanel` (F5 у Visual Studio або `dotnet run --project AdminPanel`).

   Під час старту панель сама:
   - застосує міграції, яких бракує (`Database:AutoMigrate`, за замовчуванням `true`);
   - створить ролі;
   - створить акаунт адміністратора з секції `Seed`.

3. **Увійти**:

   ```
   admin@servicehub.local / Admin#2026
   ```

   Перед реальним розгортанням обов'язково змініть `Seed:AdminPassword`.

Для показу ролей сидер створює ще два акаунти:
`moderator@servicehub.local / Moderator#2026` і `support@servicehub.local / Support#2026`.

---

## Ролі та права

| Розділ | Admin | Moderator | Support |
|---|:--:|:--:|:--:|
| Дашборд, Статистика, Замовлення, Чат | ✅ | ✅ | ✅ |
| Користувачі, Категорії, Скарги, Відгуки, Черга | ✅ | ✅ | — |
| Платежі, Ролі, Журнал дій | ✅ | — | — |
| Видалення замовлення / користувача, призначення ролей | ✅ | — | — |

Права описані політиками в `Extensions/ServiceCollectionExtensions.cs`
(`StaffOnly`, `Moderation`, `AdminOnly`). Політика за замовчуванням робить кожну сторінку
доступною лише персоналу, поки вона явно не вкаже інше, — тож новий контролер неможливо
випадково залишити відкритим для всіх.

Акаунт без службової ролі не пустить у панель навіть із правильним паролем: звичайні користувачі
платформи лежать у тому самому сховищі Identity.

---

## Кілька рішень, які варто розуміти

**Блокування зроблено через механізм Identity** (`LockoutEnd`), а не через власний прапорець, —
тому заблокований користувач не зможе увійти й через мобільний API. Видалення користувача м'яке:
замовлення, відгуки та платежі зберігають коректні зв'язки.

**Excel формується без сторонньої бібліотеки.** `ExcelExportService` пише XML-частини книги в
zip засобами `System.IO.Compression`. ClosedXML чи EPPlus змусили б усю команду відновлювати
новий пакет заради однієї функції.

**Журнал дій лише доповнюється.** Кожна дія, що змінює стан, пише рядок у `AuditLogs` через
`AdminControllerBase.AuditAsync`. Кнопки видалення в переглядачі журналу немає навмисно.
Помилка запису в журнал свідомо ігнорується: втратити рядок журналу краще, ніж зірвати рішення
модератора.

**Дані графіків готуються на сервері.** `BLL/Admin/Models/ChartData` (підписи + ряди) серіалізується
в JSON і передається в `AdminCharts`, тому в представленнях немає обробки даних, а той самий
формат віддає `/Dashboard/ChartData` для перемикача періоду.

---

## Як додати міграцію

У проєкті є фабрика контексту для етапу розробки
(`DAL/Context/ApplicationDbContextFactory.cs`), тому міграції можна створювати так:

```bash
dotnet ef migrations add MyMigration --project DAL --startup-project AdminPanel
```

Рядок підключення для цієї команди можна перевизначити змінною середовища `SERVICEHUB_CONNECTION`.

---

## Файли фронтенду

Bootstrap 5.3.3, Bootstrap Icons 1.11.3, Chart.js 4.4.3 і JavaScript-клієнт SignalR 8.0.7 лежать
у `wwwroot/lib/`. Нічого не підвантажується з CDN, тому панель працює — і показується на захисті —
без інтернету.
