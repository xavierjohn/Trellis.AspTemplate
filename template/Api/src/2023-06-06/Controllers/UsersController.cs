namespace BestWeatherForecast.Api.v2023_06_06.Controllers;

using BestWeatherForecast.Api.v2023_06_06.Models;
using BestWeatherForecast.Domain;
using Microsoft.AspNetCore.Mvc;
using Trellis.Asp;


/// <summary>
/// User management controller.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Register a new user.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(User), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<User> RegisterUser([FromBody] RegisterUserRequest request) =>
        Domain.User.TryCreate(request.FirstName, request.LastName, request.Email, request.Password)
        .ToCreatedAtActionResult(this, nameof(Get), user => new { name = user.FirstName.Value });

    /// <summary>
    /// Get a greeting for the specified user.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [HttpGet("{name}")]
    public ActionResult<string> Get(string name) => Ok($"Hello {name}!");

    /// <summary>
    /// Delete a user by ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id}")]
    public ActionResult<Unit> Delete(string id) =>
        UserId.TryCreate(id).Match(
            ok => NoContent(),
            err => err.ToActionResult<Unit>(this));
}
