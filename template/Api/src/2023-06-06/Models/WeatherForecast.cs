namespace BestWeatherForecast.Api._2023_06_06.Models;

/// <summary>
/// Weather Forecast of a city.
/// </summary>
public class WeatherForecast
{
    /// <summary>
    /// The zip code.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// The 5 day temperatures.
    /// </summary>
    public DailyTemperature[] DailyTemperatures { get; set; } = Array.Empty<DailyTemperature>();
}
