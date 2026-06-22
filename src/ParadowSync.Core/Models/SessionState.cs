namespace ParadowSync.Core.Models;

public sealed class SessionState
{
    public string? ActiveProfileId { get; set; }
    public List<RuntimeSlot> Slots { get; init; } = [];
    public int? FocusedSlotIndex { get; set; }
}

public sealed class RuntimeSlot
{
    public int AccountIndex { get; init; }
    public nint Hwnd { get; set; }
    public required string Character { get; init; }
    public required string Class { get; init; }
    public SlotStatus Status { get; set; }
}
