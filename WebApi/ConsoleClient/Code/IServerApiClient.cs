using Refit;
using Shared;
using WebApi;

namespace ConsoleClient.Code;

public interface IServerApiClient
{
    [Post("/Login/Login")]
    Task<string> LoginAsync(LoginDto loginModel);

    [Get("/WeatherForecast")]
    Task<IEnumerable<WeatherForecast>> GetWeatherForecastAsync();

}