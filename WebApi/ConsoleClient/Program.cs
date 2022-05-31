using System.Text.Json.Serialization;
using ConsoleClient.Code;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleClient;

public class Program
{
    public static async Task Main(string[] args)
    {
        IConfiguration Configuration = null;

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                Configuration = hostContext.Configuration;
                services.AddServices(Configuration);

            });

        var host = builder.Build();

        await host.StartAsync();

        var serverClient =  host.Services.GetService<IServerApiClient>();
        var weatherForecasts =  await serverClient.GetWeatherForecastAsync();

        foreach (var weatherForecast in weatherForecasts)
        {
            Console.WriteLine(weatherForecast.Summary);
        }

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }


}
