using ConsoleClient.Code;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;

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
                services.Configure<ServerOptions>(Configuration.GetSection(ServerOptions.SectionName));
                services.Configure<SeqOptions>(Configuration.GetSection(SeqOptions.SectionName));

                services.AddShared();
                services.AddBrowserScripts();

                services.AddHttpClient<HttpClient>("HttpClient").AddPolicyHandler(GetTransientHttpErrorRetryPolicy());
                services.AddTransient<AuthHeaderHandler>();
                services.AddRefitClient<IServerApiClient>()
                    .ConfigureHttpClient((sp, c) =>
                    {
                        var options = sp.GetService<IOptions<ServerOptions>>();

                        c.BaseAddress = new Uri(options.Value.Url);
                        c.Timeout = TimeSpan.FromMinutes(5);
                    })
                    .AddPolicyHandler((sp, request) => GetTransientHttpErrorRetryPolicyWithForbidden(sp))
                    .AddHttpMessageHandler<AuthHeaderHandler>();

                services.AddHostedService<AgentService>();
                services.AddTransient<WorkerService>();
            });

        builder.UseSerilog();

        var host = builder.Build();

        var seOptions = host.Services.GetService<IOptions<SeqOptions>>();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .Enrich.WithMachineName()
            .Enrich.WithAssemblyName()
            .Enrich.WithAssemblyVersion()
            .Enrich.WithClientIp()
            .Enrich.WithClientAgent()
            .WriteTo.Seq(seOptions.Value.Url, LogEventLevel.Warning, apiKey: seOptions.Value.ApiKey)
            .CreateLogger();

        await host.StartAsync();

        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }


}
