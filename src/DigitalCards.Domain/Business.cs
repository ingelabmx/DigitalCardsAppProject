namespace DigitalCards.Domain;

public sealed class Business
{
    public Business(
        Guid id,
        string name,
        string email,
        string passwordHashPlaceholder,
        string logoPath,
        string? publicName = null,
        string? primaryColor = null,
        string? secondaryColor = null,
        string? programName = null,
        string? programDescription = null)
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
        PublicName = NormalizeOptional(publicName);
        PrimaryColor = NormalizeOptional(primaryColor);
        SecondaryColor = NormalizeOptional(secondaryColor);
        ProgramName = NormalizeOptional(programName);
        ProgramDescription = NormalizeOptional(programDescription);
        GoogleClassSuffix = Slugify(Name);
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Email { get; }

    public string PasswordHashPlaceholder { get; }

    public string LogoPath { get; }

    public string? PublicName { get; }

    public string? PrimaryColor { get; }

    public string? SecondaryColor { get; }

    public string? ProgramName { get; }

    public string? ProgramDescription { get; }

    public string GoogleClassSuffix { get; }

    public string DisplayName => PublicName ?? Name;

    private static string Slugify(string value)
    {
        var allowed = value.Where(char.IsLetterOrDigit).ToArray();
        return allowed.Length == 0 ? Guid.NewGuid().ToString("N") : new string(allowed);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
