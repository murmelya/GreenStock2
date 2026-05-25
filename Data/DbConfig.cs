namespace GreenStock.Data;

public static class DbConfig
{
    public static string ConnectionString { get; set; } =
        "Host=localhost;Port=5432;Database=greenstock;Username=postgres;Password=93omupel";

    public static bool UseInMemory { get; set; } = false;

    public static string InMemoryDbName { get; set; } = "GreenStockTestDb";
}
