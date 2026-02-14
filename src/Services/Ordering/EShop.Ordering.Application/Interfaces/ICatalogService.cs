namespace EShop.Ordering.Application.Interfaces;

public interface ICatalogService
{
    Task<List<ProductDto>> GetProductsByIdsAsync(List<Guid> productIds, CancellationToken cancellationToken = default);
}

public record ProductDto(Guid Id, string Name, decimal Price);
