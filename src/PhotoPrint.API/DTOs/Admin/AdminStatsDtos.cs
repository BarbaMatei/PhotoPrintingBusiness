namespace PhotoPrint.API.DTOs.Admin;

public record AdminStatsDto(
    int TodayOrders,
    decimal TodayRevenue,
    int MonthOrders,
    decimal MonthRevenue);

public record RevenueDataPointDto(string Date, decimal Revenue);

public record ProductStatsDto(string ProductName, int TotalQuantity, int OrderCount);

public record OrdersByStatusDto(string Status, int Count);
