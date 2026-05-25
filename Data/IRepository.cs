using GreenStock.Models;

namespace GreenStock.Data;

public interface IRepository
{
    Task<List<Product>> GetProductsAsync();
    Task<Product?> GetProductByIdAsync(Guid id);
    Task AddProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task DeleteProductAsync(Guid id);

    Task<List<Category>> GetCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(Guid id);
    Task AddCategoryAsync(Category category);
    Task DeleteCategoryAsync(Guid id);

    Task AddShipmentAsync(Shipment shipment);
    Task<List<Shipment>> GetShipmentsAsync();
    Task<Shipment?> GetShipmentByIdAsync(Guid id);

    Task<User?> GetUserByLoginAsync(string login);
    Task AddUserAsync(User user);

    Task<int> SaveChangesAsync();
}