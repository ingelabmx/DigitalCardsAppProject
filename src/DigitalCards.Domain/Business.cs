namespace DigitalCards.Domain;

public sealed class Business
{
    public Business(Guid id, string name, string email, string passwordHashPlaceholder, string logoPath)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Business name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Business email is required.", nameof(email));
        }

        Id = id;
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHashPlaceholder = passwordHashPlaceholder;
        LogoPath = logoPath;
        GoogleClassSuffix = Slugify(Name);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Email { get; }

    public string PasswordHashPlaceholder { get; }

    public string LogoPath { get; }

    public string GoogleClassSuffix { get; }

    private static string Slugify(string value)
    {
        var allowed = value.Where(char.IsLetterOrDigit).ToArray();
        return allowed.Length == 0 ? Guid.NewGuid().ToString("N") : new string(allowed);
    }
}

