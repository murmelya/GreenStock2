using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GreenStock.Data;
using GreenStock.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenStock.Services;

public class HeatmapCell
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int DaysUntilExpiry { get; set; }
    public double MovementVelocity { get; set; }
    public Color HeatColor { get; set; }
    public string Tooltip { get; set; } = string.Empty;
}

public static class WarehouseHeatmapService
{
    private const int Rows = 8;
    private const int Cols = 10;
    private static readonly Random _random = new Random();

    private static readonly Color[] HeatColorsExpiry = new[]
    {
        Color.FromArgb(0, 200, 0),   
        Color.FromArgb(150, 200, 0),
        Color.FromArgb(255, 200, 0),  
        Color.FromArgb(255, 100, 0),
        Color.FromArgb(255, 0, 0)     
    };

    private static readonly Color[] HeatColorsMovement = new[]
    {
        Color.FromArgb(0, 200, 0),    
        Color.FromArgb(150, 200, 0),  
        Color.FromArgb(255, 200, 0),  
        Color.FromArgb(255, 100, 0),  
        Color.FromArgb(255, 0, 0)     
    };

    public static List<HeatmapCell> GenerateHeatmap()
    {
        var cells = new List<HeatmapCell>();

        using var db = new AppDbContext();

        var products = db.Products
            .Where(p => p.Stock > 0)
            .ToList();

        var shipments = db.ShipmentItems
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, TotalShipped = g.Sum(si => si.Quantity) })
            .ToDictionary(x => x.ProductId, x => x.TotalShipped);

        var dates = db.Shipments.Select(s => s.CreatedAt).ToList();
        var daysInSystem = dates.Any()
            ? Math.Max(1, (DateTime.UtcNow - dates.Min()).Days)
            : 1;

        if (!products.Any())
        {
            return GetEmptyHeatmap();
        }

        var allCells = new List<(int Row, int Col, Product Product, int Quantity, double MovementVelocity)>();

        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                var product = products[_random.Next(products.Count)];

                int totalShipped = shipments.GetValueOrDefault(product.Id, 0);
                double movementVelocity = daysInSystem > 0
                    ? (double)totalShipped / daysInSystem * 30  
                    : 0;

                int quantityInCell = _random.Next(1, Math.Min(product.Stock + 10, product.Stock + 5));
                if (quantityInCell > product.Stock) quantityInCell = product.Stock;

                allCells.Add((row, col, product, quantityInCell, movementVelocity));
            }
        }

        foreach (var item in allCells)
        {
            var product = item.Product;

            var daysUntilExpiry = -1;
            if (product.ExpiryDate.HasValue)
            {
                daysUntilExpiry = (product.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days;
            }

            var heatColorExpiry = CalculateHeatColorExpiry(daysUntilExpiry, product.Stock);
            var heatColorMovement = CalculateHeatColorMovement(item.MovementVelocity);

            var tooltip = $"📦 {product.Name}\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n" +
                         $"📊 Остаток: {item.Quantity} {product.Unit}\n" +
                         $"💰 Цена: {product.PurchasePrice:N2} ₽\n" +
                         (daysUntilExpiry >= 0
                             ? $"⏰ До истечения: {daysUntilExpiry} дней\n"
                             : "♾️ Бессрочный товар\n") +
                         $"📈 Скорость движения: {item.MovementVelocity:F2} шт/мес\n" +
                         (daysUntilExpiry <= 7 && daysUntilExpiry >= 0
                             ? "⚠️ СРОЧНО! Требуется распродажа!\n"
                             : "") +
                         $"📍 Ряд: {item.Row + 1}, Стеллаж: {item.Col + 1}\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n" +
                         $"🖱️ Нажмите для деталей";

            cells.Add(new HeatmapCell
            {
                Row = item.Row,
                Column = item.Col,
                ProductId = product.Id.ToString(),
                ProductName = product.Name,
                Stock = item.Quantity,
                DaysUntilExpiry = daysUntilExpiry,
                MovementVelocity = item.MovementVelocity,
                HeatColor = heatColorExpiry,
                Tooltip = tooltip
            });
        }

        return cells;
    }

    private static Color CalculateHeatColorExpiry(int daysUntilExpiry, int stock)
    {
        double riskScore = 0;

        if (daysUntilExpiry >= 0)
        {
            if (daysUntilExpiry <= 0) riskScore = 1.0;
            else if (daysUntilExpiry <= 7) riskScore = 1.0;
            else if (daysUntilExpiry <= 30) riskScore = 0.7;
            else if (daysUntilExpiry <= 90) riskScore = 0.4;
            else riskScore = 0.2;
        }

        if (stock > 100) riskScore = Math.Max(riskScore, 0.8);
        else if (stock > 50) riskScore = Math.Max(riskScore, 0.6);

        var index = Math.Min((int)(riskScore * (HeatColorsExpiry.Length - 1)), HeatColorsExpiry.Length - 1);
        return HeatColorsExpiry[index];
    }

    private static Color CalculateHeatColorMovement(double movementVelocity)
    {
        double score = 0;
        if (movementVelocity <= 0) score = 1.0;     
        else if (movementVelocity < 1) score = 0.8; 
        else if (movementVelocity < 5) score = 0.5; 
        else if (movementVelocity < 10) score = 0.3; 
        else score = 0.0;                           

        var index = Math.Min((int)(score * (HeatColorsMovement.Length - 1)), HeatColorsMovement.Length - 1);
        return HeatColorsMovement[index];
    }

    private static List<HeatmapCell> GetEmptyHeatmap()
    {
        var cells = new List<HeatmapCell>();
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Cols; col++)
            {
                cells.Add(new HeatmapCell
                {
                    Row = row,
                    Column = col,
                    ProductName = "Свободно",
                    Stock = 0,
                    DaysUntilExpiry = -1,
                    MovementVelocity = 0,
                    HeatColor = Color.LightGray,
                    Tooltip = "Свободная ячейка\nМожно разместить товар"
                });
            }
        }
        return cells;
    }

    public static (int Rows, int Cols) GetGridSize() => (Rows, Cols);
}