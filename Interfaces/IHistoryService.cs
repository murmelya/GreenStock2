using System;
using System.Collections.Generic;
using GreenStock.Services;

namespace GreenStock.Interfaces;

public interface IHistoryService
{
    List<HistoryService.ShipmentHistoryDto> GetAllShipments();
    List<HistoryService.ShipmentItemDto> GetShipmentItems(Guid shipmentId);
}