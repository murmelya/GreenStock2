using GreenStock.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenStock.Data;

public class Repository : IRepository
{
    private readonly AppDbContext _context;

    public Repository(AppDbContext context)
    {
        _context = context;
    }

    public Repository()
    {
        _context = new AppDbContext();
    }
   
    public async Task<List<Product>> GetProductsAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Article)
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(Guid id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddProductAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await Task.CompletedTask;
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await GetProductByIdAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
        }
    }

    
    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task AddCategoryAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var category = await GetCategoryByIdAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
        }
    }

    public async Task AddShipmentAsync(Shipment shipment)
    {
        await _context.Shipments.AddAsync(shipment);

        foreach (var item in shipment.Items)
        {
            await _context.ShipmentItems.AddAsync(item);
        }

        foreach (var item in shipment.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.Stock -= item.Quantity;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<Shipment>> GetShipmentsAsync()
    {
        return await _context.Shipments
            .Include(s => s.CreatedByUser)
            .Include(s => s.Items)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Shipment?> GetShipmentByIdAsync(Guid id)
    {
        return await _context.Shipments
            .Include(s => s.CreatedByUser)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<User?> GetUserByLoginAsync(string login)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Login == login);
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}