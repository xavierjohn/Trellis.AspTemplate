namespace BestWeatherForecast.Api.v2023_06_06.Controllers;

using BestWeatherForecast.Application.WeatherForcast;
using BestWeatherForecast.Domain;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using ServiceLevelIndicators;
using Trellis.Asp;

/// <summary>
/// Weather forecast controller.
/// </summary>
[ApiController]
[Consumes("application/json")]
[Produces("application/json")]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sender"></param>
    public WeatherForecastController(ISender sender) => _sender = sender;

    /// <summary>
    /// Get the weather forecast for Redmond,WA.
    /// </summary>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(Models.WeatherForecast), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Models.WeatherForecast>> GetRedmond(CancellationToken cancellationToken)
        => await ZipCode.TryCreate("98052")
            .Bind(static zipCode => WeatherForecastQuery.TryCreate(zipCode))
            .BindAsync(q => _sender.Send(q, cancellationToken))
            .MapAsync(MapToDto)
            .ToActionResultAsync(this);

    /// <summary>
    /// Get the weather forecast for the given zip code.
    /// </summary>
    /// <param name="zipCode">Zip code.</param>
    /// <param name="cancellationToken">Cancellation Token.</param>
    /// <returns></returns>
    [HttpGet("{zipCode}")]
    [ProducesResponseType(typeof(Models.WeatherForecast), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<Models.WeatherForecast>> Get([CustomerResourceId] string zipCode, CancellationToken cancellationToken)
        => await ZipCode.TryCreate(zipCode)
            .Bind(static zipCode => WeatherForecastQuery.TryCreate(zipCode))
            .BindAsync(q => _sender.Send(q, cancellationToken))
            .MapAsync(MapToDto)
            .ToActionResultAsync(this);

    /// <summary>
    /// This method throws to show the error handling middleware handles it.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    [HttpGet("Throw")]
    public string Throw()
    {
        throw new NotImplementedException("Catch me middleware.");
    }

    private static Models.WeatherForecast MapToDto(WeatherForecast forecast) => new()
    {
        ZipCode = forecast.Id.Value,
        DailyTemperatures = forecast.DailyTemperatures.Select(d => new Models.DailyTemperature
        {
            Date = d.Date,
            TemperatureC = d.TemperatureC,
            TemperatureF = d.TemperatureF,
            Summary = d.Summary
        }).ToArray()
    };
}
