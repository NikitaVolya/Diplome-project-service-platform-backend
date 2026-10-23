namespace AdminPanel.Services;

/// <summary>
/// Зберігає фото профілю у wwwroot/uploads/avatars. Тип файлу перевіряємо за його
/// першими байтами, а не за розширенням чи заголовком браузера: інакше під виглядом
/// картинки можна було б покласти на сервер що завгодно.
/// </summary>
public sealed class AvatarStorage
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private const string Folder = "uploads/avatars";

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AvatarStorage> _logger;

    public AvatarStorage(IWebHostEnvironment environment, ILogger<AvatarStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public sealed record Result(bool Succeeded, string Url, string Error)
    {
        public static Result Fail(string error) => new(false, string.Empty, error);
    }

    public async Task<Result> SaveAsync(string userId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return Result.Fail("Оберіть файл із фото.");
        }

        if (file.Length > MaxBytes)
        {
            return Result.Fail("Фото завелике — максимум 2 Мб.");
        }

        var head = new byte[12];
        await using (var probe = file.OpenReadStream())
        {
            _ = await probe.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, cancellationToken);
        }

        var extension = Detect(head);
        if (extension == null)
        {
            return Result.Fail("Підходять лише фото у форматі JPG, PNG або WEBP.");
        }

        var root = Path.Combine(_environment.WebRootPath, Folder);
        Directory.CreateDirectory(root);

        // Нова назва щоразу — щоб браузер не показував старе фото з кешу.
        var name = $"{Safe(userId)}-{Guid.NewGuid():N}{extension}";

        await using (var target = File.Create(Path.Combine(root, name)))
        {
            await file.CopyToAsync(target, cancellationToken);
        }

        return new Result(true, $"/{Folder}/{name}", string.Empty);
    }

    /// <summary>Видаляє лише файли з власної теки; будь-які інші адреси ігнорує.</summary>
    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith($"/{Folder}/", StringComparison.Ordinal))
        {
            return;
        }

        var name = Path.GetFileName(url);
        var path = Path.Combine(_environment.WebRootPath, Folder, name);

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Не вдалося видалити старе фото {Path}", path);
        }
    }

    private static string? Detect(byte[] head)
    {
        if (head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF)
        {
            return ".jpg";
        }

        if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47)
        {
            return ".png";
        }

        if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F'
            && head[8] == 'W' && head[9] == 'E' && head[10] == 'B' && head[11] == 'P')
        {
            return ".webp";
        }

        return null;
    }

    private static string Safe(string value) =>
        new(value.Where(char.IsAsciiLetterOrDigit).Take(36).ToArray());
}
