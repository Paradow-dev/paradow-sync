namespace ParadowSync.Core.Models;

public sealed class AccountSlot
{
    public required string AccountId { get; init; }
    public required string Character { get; init; }
    public required string Class { get; init; }
    public int Monitor { get; init; }
    public required WindowSlot Slot { get; init; }
}

public sealed class WindowSlot
{
    public int X { get; init; }
    public int Y { get; init; }
    public int W { get; init; }
    public int H { get; init; }
}
