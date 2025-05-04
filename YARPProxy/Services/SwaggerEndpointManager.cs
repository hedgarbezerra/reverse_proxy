using Yarp.ReverseProxy;

namespace YARPProxy.Services;

public record SwaggerVersionsEndpointWithContent(string Name, string Address, List<EndpointVersion> AvailableVersions);
public record EndpointVersion(string Version, string Content);

public interface ISwaggerEndpointManager
{
    Task<List<SwaggerVersionsEndpointWithContent>> DiscoverSwaggerVersionsAsync();
    List<SwaggerVersionsEndpointWithContent> AvailableEndpointsVersions { get; }
}

public class SwaggerEndpointManager : ISwaggerEndpointManager
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyStateLookup _proxyStateLookup;
    private readonly ILogger<SwaggerEndpointManager> _logger;

    public List<SwaggerVersionsEndpointWithContent> AvailableEndpointsVersions { get; }

    public SwaggerEndpointManager(IHttpClientFactory httpClientFactory, IProxyStateLookup proxyStateLookup, ILogger<SwaggerEndpointManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _proxyStateLookup = proxyStateLookup;
        _logger = logger;

        AvailableEndpointsVersions = DiscoverSwaggerVersionsAsync().Result;
    }

    //TODO: Melhorar uso dessas chamas e salvar o conteúdo do resultado(Json)
    public async Task<List<SwaggerVersionsEndpointWithContent>> DiscoverSwaggerVersionsAsync()
    {
        var results = new List<SwaggerVersionsEndpointWithContent>();

        foreach (var route in _proxyStateLookup.GetRoutes())
        {
            var (_, endpoint) = route.Cluster.Destinations.First();
            var baseAddress = endpoint.Model.Config.Address;
            var versions = new List<EndpointVersion>();

            try
            {
                await LoadRouteVersions(baseAddress, versions);

                if (versions.Any())
                {
                    results.Add(new SwaggerVersionsEndpointWithContent(route.Config.RouteId, baseAddress, versions));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao descobrir versões do Swagger para {Address}", baseAddress);
            }
        }

        return results;
    }
    private async Task LoadRouteVersions(string baseAddress, List<EndpointVersion> versions)
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
                    versions.Add(new EndpointVersion($"v{majorVersion}", await response.Content.ReadAsStringAsync()));
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