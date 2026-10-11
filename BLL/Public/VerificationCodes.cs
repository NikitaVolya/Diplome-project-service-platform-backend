using Microsoft.Extensions.Caching.Memory;

namespace BLL.Public;

public enum CodePurpose
{
    Registration,
    PasswordReset
}

public sealed class CodeTicket
{
    public string Code { get; init; } = string.Empty;

    public TimeSpan ValidFor { get; init; }

    public TimeSpan ResendAfter { get; init; }

    public bool TooSoon { get; init; }
}

public interface IVerificationCodes
{
    CodeTicket Issue(string email, CodePurpose purpose);

    bool Verify(string email, CodePurpose purpose, string code);

    bool HasActive(string email, CodePurpose purpose);
}

public sealed class VerificationCodes : IVerificationCodes
{
    private const int MaxAttempts = 5;

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(45);

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _clock;

    public VerificationCodes(IMemoryCache cache, TimeProvider? clock = null)
    {
        _cache = cache;
        _clock = clock ?? TimeProvider.System;
    }

    public CodeTicket Issue(string email, CodePurpose purpose)
    {
        var key = Key(email, purpose);
        var now = _clock.GetUtcNow();

        if (_cache.TryGetValue<Entry>(key, out var existing) && existing != null && now < existing.ResendAfter)
        {
            return new CodeTicket
            {
                Code = existing.Code,
                ValidFor = existing.ExpiresAt - now,
                ResendAfter = existing.ResendAfter - now,
                TooSoon = true
            };
        }

        var entry = new Entry
        {
            Code = Random.Shared.Next(0, 1_000_000).ToString("D6"),
            ExpiresAt = now + Lifetime,
            ResendAfter = now + ResendCooldown
        };

        _cache.Set(key, entry, entry.ExpiresAt);

        return new CodeTicket
        {
            Code = entry.Code,
            ValidFor = Lifetime,
            ResendAfter = ResendCooldown
        };
    }

    public bool Verify(string email, CodePurpose purpose, string code)
    {
        var key = Key(email, purpose);

        if (!_cache.TryGetValue<Entry>(key, out var entry) || entry == null)
        {
            return false;
        }

        if (_clock.GetUtcNow() >= entry.ExpiresAt || ++entry.Attempts > MaxAttempts)
        {
            _cache.Remove(key);
            return false;
        }

        var digits = Digits(code);

        if (digits.Length != entry.Code.Length || !Equal(digits, entry.Code))
        {
            return false;
        }

        _cache.Remove(key);
        return true;
    }

    public bool HasActive(string email, CodePurpose purpose) =>
        _cache.TryGetValue<Entry>(Key(email, purpose), out var entry)
        && entry != null
        && _clock.GetUtcNow() < entry.ExpiresAt;

    public static string Digits(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : new string(value.Where(char.IsAsciiDigit).ToArray());

    private static bool Equal(string left, string right)
    {
        var diff = 0;
        for (var i = 0; i < left.Length; i++)
        {
            diff |= left[i] ^ right[i];
        }

        return diff == 0;
    }

    private static string Key(string email, CodePurpose purpose) =>
        $"code:{purpose}:{email.Trim().ToLowerInvariant()}";

    private sealed class Entry
    {
        public string Code { get; init; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; init; }
        public DateTimeOffset ResendAfter { get; init; }
        public int Attempts { get; set; }
    }
}
