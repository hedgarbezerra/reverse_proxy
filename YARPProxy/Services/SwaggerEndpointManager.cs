using Yarp.ReverseProxy;

namespace YARPProxy.Services;

public record SwaggerVersionsEndpoint(string Name, string Address, List<string> Versions);

public interface ISwaggerEndpointManager
{
    Task<List<SwaggerVersionsEndpoint>> DiscoverSwaggerVersionsAsync();
    List<SwaggerVersionsEndpoint> AvailableEndpointsVersions { get; }
}

public class SwaggerEndpointManager : ISwaggerEndpointManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyStateLookup _proxyStateLookup;
    private readonly ILogger<SwaggerEndpointManager> _logger;

    public List<SwaggerVersionsEndpoint> AvailableEndpointsVersions { get; }

    public SwaggerEndpointManager(IHttpClientFactory httpClientFactory, IProxyStateLookup proxyStateLookup, ILogger<SwaggerEndpointManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _proxyStateLookup = proxyStateLookup;
        _logger = logger;

        AvailableEndpointsVersions = DiscoverSwaggerVersionsAsync().Result;
    }

    public async Task<List<SwaggerVersionsEndpoint>> DiscoverSwaggerVersionsAsync()
    {
        var results = new List<SwaggerVersionsEndpoint>();

        foreach (var route in _proxyStateLookup.GetRoutes())
        {
            var (_, endpoint) = route.Cluster.Destinations.First();
            var baseAddress = endpoint.Model.Config.Address;
            var versions = new List<string>();

            try
            {
                await LoadRouteVersions(baseAddress, versions);

                if (versions.Any())
                {
                    results.Add(new SwaggerVersionsEndpoint(route.Config.RouteId, baseAddress, versions));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao descobrir versões do Swagger para {Address}", baseAddress);
            }
        }

        return results;
    }
    private async Task LoadRouteVersions(string baseAddress, List<string> versions)
    {
        // Tenta descobrir as versões testando cada endpoint
        for (int majorVersion = 1; majorVersion <= 3; majorVersion++)
        {
            var url = $"{baseAddress}/swagger/v{majorVersion}/swagger.json";
            try
            {
                var response = await _httpClientFactory.CreateClient().GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    versions.Add($"v{majorVersion}");
                }
                if(response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Versão {Version} não encontrada em {Url}", majorVersion, url);
            }
        }
    }
}