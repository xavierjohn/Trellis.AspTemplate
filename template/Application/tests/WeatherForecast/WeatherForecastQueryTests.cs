namespace Application.Tests.WeatherForecast;

using System;
using System.Threading.Tasks;
using BestWeatherForecast.Application.WeatherForcast;
using BestWeatherForecast.Domain;
using FluentAssertions;
using Mediator;

public class WeatherForecastQueryTests
{
    private readonly ISender _sender;

    public WeatherForecastQueryTests(ISender sender) => _sender = sender;

    [Fact]
    public async Task Will_get_weather_forecast_for_Redmond()
    {
        // Arrange
        var (_, query, _) = ZipCode.TryCreate("98052")
            .Bind(WeatherForecastQuery.TryCreate);

        // Act
        var result = await _sender.Send(query!, TestContext.Current.CancellationToken);

        // Assert
        var (isSuccess, forecast, _) = result;
        isSuccess.Should().BeTrue();
        forecast!.Id.Value.Should().Be(query!.ZipCode);
        forecast.DailyTemperatures.Should().HaveCount(3);
        var day = forecast.DailyTemperatures[0];
        day.Date.Should().Be(new DateOnly(2023, 6, 6));
        day.TemperatureC.Should().Be(10);
        day.TemperatureF.Should().BeApproximately(50, 0.001);
        day.Summary.Should().Be("Sunny");

        day = forecast.DailyTemperatures[1];
        day.Date.Should().Be(new DateOnly(2023, 6, 7));
        day.TemperatureC.Should().Be(20);
        day.TemperatureF.Should().BeApproximately(68.0, 0.001);
        day.Summary.Should().Be("Cloudy");

        day = forecast.DailyTemperatures[2];
        day.Date.Should().Be(new DateOnly(2023, 6, 8));
        day.TemperatureC.Should().Be(30);
        day.TemperatureF.Should().BeApproximately(86.0, 0.001);
        day.Summary.Should().Be("Rainy");
    }
    [Fact]
    public async Task Will_return_NotFound_from_unknown_zip()
    {
        // Arrange
        var (_, query, _) = ZipCode.TryCreate("12345")
            .Bind(WeatherForecastQuery.TryCreate);

        // Act
        var result = await _sender.Send(query!, TestContext.Current.CancellationToken);

        // Assert
        var (isSuccess, _, err) = result;
        isSuccess.Should().BeFalse();
        var error = err!;
        error.Should().BeOfType<NotFoundError>();
        error.Code.Should().Be("not.found.error");
        error.Detail.Should().Be("No weather forecast found for the zip code.");
        error.Instance.Should().Be("12345");
    }
}
