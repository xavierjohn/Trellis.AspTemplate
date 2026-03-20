namespace BestWeatherForecast.Domain;

using Trellis.Primitives;

public class User : Aggregate<UserId>
{
    public FirstName FirstName { get; }
    public LastName LastName { get; }
    public EmailAddress Email { get; }
    public string Password { get; } // Intentionally a raw string — demonstrates that primitives work alongside value objects

    public static Result<User> TryCreate(FirstName firstName, LastName lastName, EmailAddress email, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return ValidationError.For("password", "Password must not be empty.");

        if (password.Length < 8)
            return ValidationError.For("password", "Password must be at least 8 characters long.");

        return new User(firstName, lastName, email, password);
    }

    private User(FirstName firstName, LastName lastName, EmailAddress email, string password)
        : base(UserId.NewUniqueV4())
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Password = password;
    }
}
