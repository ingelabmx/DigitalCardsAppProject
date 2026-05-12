namespace DigitalCards.Domain;

public sealed class AdminUser
{
    public AdminUser(
        Guid id,
        string userName,
        string firstName,
        string lastName,
        string email,
        string passwordHashPlaceholder)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Admin user id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("Admin user name is required.", nameof(userName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Admin email is required.", nameof(email));
        }

        Id = id;
        UserName = userName.Trim();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHashPlaceholder = passwordHashPlaceholder;
    }

    public Guid Id { get; }

    public string UserName { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public string Email { get; }

    public string PasswordHashPlaceholder { get; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
