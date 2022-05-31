using System.Net;
using ConsoleClient.Code;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace ConsoleClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ServerOptions>(configuration.GetSection(ServerOptions.SectionName));

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

        return services;
    }

    static IAsyncPolicy<HttpResponseMessage> GetTransientHttpErrorRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }

    static IAsyncPolicy<HttpResponseMessage> GetTransientHttpErrorRetryPolicyWithForbidden(IServiceProvider sp)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound || msg.StatusCode == HttpStatusCode.Unauthorized)
            .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)), onRetry: (message, retryCount, context) =>
            {
                if (message.Result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var authHeaderHandler = sp.GetService<AuthHeaderHandler>();
                    authHeaderHandler.ResetToken();
                }
            });
    }
}
