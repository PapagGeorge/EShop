using System.Net.Http.Json;
using EShop.Ordering.Application.Interfaces;

namespace EShop.Ordering.Infrastructure.Services;

public class CatalogServiceClient : ICatalogService
{
    private readonly HttpClient _httpClient;

    public CatalogServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<ProductDto>> GetProductsByIdsAsync(
        List<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/products/batch", productIds, cancellationToken);

        response.EnsureSuccessStatusCode();

        var products = await response.Content
            .ReadFromJsonAsync<List<ProductDto>>(cancellationToken: cancellationToken);

        return products ?? new List<ProductDto>();
    }
}
