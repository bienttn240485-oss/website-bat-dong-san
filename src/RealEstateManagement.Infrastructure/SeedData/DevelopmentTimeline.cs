namespace RealEstateManagement.Infrastructure.SeedData;

internal static class DevelopmentTimeline
{
    public static readonly DateOnly OpenedDate = new(2026, 7, 20);
    public static readonly DateOnly DatasetDate = new(2026, 7, 30);
    public static readonly DateTimeOffset OpenedAtUtc = AtUtc(2026, 7, 20, 2);
    public static readonly DateTimeOffset DatasetAtUtc = AtUtc(2026, 7, 30, 9);

    public static DateTimeOffset AtUtc(int year, int month, int day, int hour = 2, int minute = 0)
        => new(year, month, day, hour, minute, 0, TimeSpan.Zero);

    public static DateTimeOffset SaleCreatedAtUtc(string email)
        => email switch
        {
            "sale.tham@anphurealestate.local" => AtUtc(2026, 7, 20, 3),
            "sale.thuy@anphurealestate.local" => AtUtc(2026, 7, 20, 4),
            "sale.tuan@anphurealestate.local" => AtUtc(2026, 7, 21, 2),
            "sale.linh@anphurealestate.local" => AtUtc(2026, 7, 21, 3),
            "sale.huy@anphurealestate.local" => AtUtc(2026, 7, 22, 2),
            _ => OpenedAtUtc
        };
}
