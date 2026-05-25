using GreenStock.Models;

namespace GreenStock.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByArticleAsync(string article);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<List<Product>> GetByCategoryAsync(Guid categoryId);
    Task<List<Product>> SearchAsync(string searchTerm);
}
