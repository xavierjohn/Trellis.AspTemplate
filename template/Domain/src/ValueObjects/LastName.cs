namespace BestWeatherForecast.Domain;

[StringLength(50)]
public partial class LastName : RequiredString<LastName>
{
}
