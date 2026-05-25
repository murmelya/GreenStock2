using System.Collections.Generic;
using GreenStock.Services;

namespace GreenStock.Interfaces;

public interface IWarehouseHeatmapService
{
    List<HeatmapCell> GenerateHeatmap();
    (int Rows, int Cols) GetGridSize();
}