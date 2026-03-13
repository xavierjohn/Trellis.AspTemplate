namespace BestWeatherForecast.Api._2023_06_06.Models;

/// <summary>
/// Daily Temperature
/// </summary>
public class DailyTemperature
{
    /// <summary>
    /// Recorded Date
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Temperature in Centigrade.
    /// </summary>
    public double TemperatureC { get; set; }

    /// <summary>
    /// Temperature in Fahrenheit.
    /// </summary>
    public double TemperatureF { get; set; }

    /// <summary>
    /// Weather.
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}
