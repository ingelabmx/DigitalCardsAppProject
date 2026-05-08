namespace DigitalCards.Infrastructure.Persistence.MySql;

internal static class LegacyIdMapper
{
    public static Guid ToGuid(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Legacy ids must be positive.");
        }

        return Guid.Parse($"00000000-0000-0000-0000-{id:x12}");
    }

    public static int ToInt32(Guid id)
    {
        var value = id.ToString("N");
        if (!value.StartsWith("00000000000000000000", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Guid {id} does not represent a legacy integer id.");
        }

        return Convert.ToInt32(value[20..], 16);
    }

    public static int? TryTokenToInt32(string token)
    {
        if (Guid.TryParse(token, out var guid))
        {
            return ToInt32(guid);
        }

        return int.TryParse(token, out var id) ? id : null;
    }
}
