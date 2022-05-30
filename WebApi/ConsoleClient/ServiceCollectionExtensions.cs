using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace ConsoleClient;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlockchainShared(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddWalletServiceClient(configuration);

        return services;
    }

    public static IServiceCollection AddWalletServiceClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WalletServiceOptions>(configuration.GetSection(WalletServiceOptions.SectionName));

        services.AddHttpClient<HttpClient>("HttpClient").AddPolicyHandler(GetTransientHttpErrorRetryPolicy());
        services.AddTransient<WalletServiceApiAuthHeaderHandler>();
        services.AddRefitClient<IWalletServiceApiClient>()
            .ConfigureHttpClient((sp, c) =>
            {
                var options = sp.GetService<IOptions<WalletServiceOptions>>();

                c.BaseAddress = new Uri(options.Value.Url);
                c.Timeout = TimeSpan.FromMinutes(5);
            })
            .AddPolicyHandler((sp, request) => GetTransientHttpErrorRetryPolicyWithForbidden(sp))
            .AddHttpMessageHandler<WalletServiceApiAuthHeaderHandler>();

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
                    var authHeaderHandler = sp.GetService<WalletServiceApiAuthHeaderHandler>();
                    authHeaderHandler.ResetToken();
                }
            });
    }
}
