using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Refit;

namespace ConsoleClient.Code;

public class AuthHeaderHandler : DelegatingHandler
{
    private static string token;
    private readonly ILogger<AuthHeaderHandler> logger;
    private readonly IOptions<ServerOptions> options;
    private readonly IServerApiClient authApi;

    public AuthHeaderHandler(ILogger<AuthHeaderHandler> logger, IOptions<ServerOptions> options, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.options = options;
        var httpClient = httpClientFactory.CreateClient("HttpClient");
        httpClient.BaseAddress = new Uri(options.Value.Url);
        authApi = RestService.For<IServerApiClient>(httpClient);
    }

    private async Task LoginAsync()
    {
        token = await authApi.LoginAsync(new LoginDto { UserName = options.Value.UserName, Password = options.Value.Password });
    }

    public bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(token);
    }

    public void ResetToken()
    {
        token = string.Empty;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token))
        {
            await LoginAsync();
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}