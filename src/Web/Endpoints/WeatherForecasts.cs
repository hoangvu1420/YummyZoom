using YummyZoom.Application.WeatherForecasts.Queries.GetWeatherForecasts;

namespace YummyZoom.Web.Endpoints;

public class WeatherForecasts : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app
            .MapGroup(this)
            .RequireAuthorization();

        // GET /api/WeatherForecasts
        group.MapGet(GetWeatherForecasts)
            .WithStandardResults<IEnumerable<WeatherForecast>>();
    }

    public async Task<IResult> GetWeatherForecasts(ISender sender)
    {
        var result = await sender.Send(new GetWeatherForecastsQuery());
        
        return result.ToIResult();
    }
}
