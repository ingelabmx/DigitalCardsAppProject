namespace DigitalCards.Domain;

public sealed class Client
{
    public Client(
        Guid id,
        string userName,
        string firstName,
        string lastName,
        string email,
        string passwordHashPlaceholder = "")
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new ArgumentException("User name is required.", nameof(userName));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("First name is required.", nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("Last name is required.", nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
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

    public string FullName => $"{FirstName} {LastName}";
}
