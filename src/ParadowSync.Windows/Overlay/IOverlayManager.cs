using ParadowSync.Core.Models;

namespace ParadowSync.Windows.Overlay;

public interface IOverlayManager : IDisposable
{
    void Show(SessionState session, Profile profile);
    void UpdateFocusedSlot(int slotIndex);
    void Hide();
    void SetVisible(bool visible);
    event EventHandler<int>? SlotClicked;
}
