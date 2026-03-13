namespace BestWeatherForecast.Api._2023_06_06.Models;

using BestWeatherForecast.Domain;
using Trellis.Primitives;

/// <summary>
/// Request model for registering a user.
/// </summary>
public record RegisterUserRequest
{
    /// <summary>First name of the user.</summary>
    public FirstName FirstName { get; init; } = null!;

    /// <summary>Last name of the user.</summary>
    public LastName LastName { get; init; } = null!;

    /// <summary>Email address of the user.</summary>
    public EmailAddress Email { get; init; } = null!;

    /// <summary>Password for the user account.</summary>
    public string Password { get; init; } = null!;
}
