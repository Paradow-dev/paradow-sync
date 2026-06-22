using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using ParadowSync.Core.Catalog;
using ParadowSync.Windows.Services;

namespace ParadowSync.Windows.Overlay;

internal sealed record StripSlot(int AccountIndex, string Class, string Character);

internal static class OverlayWin32
{
    internal const int GwlExstyle = -20;
    internal const int WsExLayered = 0x80000;
    internal const int WsExTransparent = 0x20;
    internal const int WsExNoactivate = 0x8000000;
    internal const int WsExTopmost = 0x8;
    internal const int WmNchittest = 0x0084;
    internal const int HtTransparent = -1;
    internal const byte LwaAlpha = 0x2;

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, byte dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    internal static Color ParseClassColor(string? hex)
    {
        if (string.IsNullOrEmpty(hex) || hex.Length < 7 || hex[0] != '#')
            return Color.Gray;

        return Color.FromArgb(
            Convert.ToInt32(hex[1..3], 16),
            Convert.ToInt32(hex[3..5], 16),
            Convert.ToInt32(hex[5..7], 16));
    }

    internal static Bitmap? LoadIconBitmap(IconLoader iconLoader, string className)
    {
        var bytes = iconLoader.GetIconBytes(className);
        if (bytes is null || bytes.Length == 0)
            return null;

        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }
}

internal sealed class TeamStripWindow : Form
{
    private const int InactiveIconSize = 28;
    private const int ActiveIconSize = 36;
    private const int IconPadding = 4;
    private const int StripPadding = 6;
    private const int BorderWidth = 2;

    private readonly IconLoader _iconLoader;
    private readonly Dictionary<string, Bitmap> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<StripSlot> _slots = [];
    private int _activeIndex = -1;

    public event EventHandler<int>? IconClicked;

    public TeamStripWindow(IconLoader iconLoader)
    {
        _iconLoader = iconLoader;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= OverlayWin32.WsExLayered | OverlayWin32.WsExNoactivate | OverlayWin32.WsExTopmost;
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = OverlayWin32.SetLayeredWindowAttributes(Handle, 0, 255, OverlayWin32.LwaAlpha);
    }

    public void SetSlots(IReadOnlyList<StripSlot> slots)
    {
        _slots = slots;
        CacheIcons();
        ResizeToContent();
        Invalidate();
    }

    public void SetActiveIndex(int index)
    {
        if (_activeIndex == index)
            return;

        _activeIndex = index;
        Invalidate();
    }

    public void PositionOnMonitor(MonitorInfo monitor, string position)
    {
        ResizeToContent();
        var x = monitor.X + (monitor.Width - Width) / 2;
        var y = string.Equals(position, "bottom", StringComparison.OrdinalIgnoreCase)
            ? monitor.Y + monitor.Height - Height - StripPadding
            : monitor.Y + StripPadding;
        Location = new Point(x, y);
    }

    private void CacheIcons()
    {
        foreach (var bitmap in _iconCache.Values)
            bitmap.Dispose();
        _iconCache.Clear();

        foreach (var slot in _slots)
        {
            if (_iconCache.ContainsKey(slot.Class))
                continue;

            var bitmap = OverlayWin32.LoadIconBitmap(_iconLoader, slot.Class);
            if (bitmap is not null)
                _iconCache[slot.Class] = bitmap;
        }
    }

    private void ResizeToContent()
    {
        var width = StripPadding * 2;
        for (var i = 0; i < _slots.Count; i++)
        {
            var iconSize = i == _activeIndex ? ActiveIconSize : InactiveIconSize;
            width += iconSize + IconPadding;
        }

        if (_slots.Count > 0)
            width -= IconPadding;

        var height = StripPadding * 2 + ActiveIconSize + BorderWidth * 2;
        ClientSize = new Size(Math.Max(width, StripPadding * 2), height);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Color.FromArgb(200, 24, 24, 28));

        var x = StripPadding;
        var centerY = ClientSize.Height / 2;

        for (var i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            var isActive = i == _activeIndex;
            var iconSize = isActive ? ActiveIconSize : InactiveIconSize;
            var classColor = OverlayWin32.ParseClassColor(ClassCatalog.Get(slot.Class)?.ColorHex);

            var iconY = centerY - iconSize / 2;
            var iconRect = new Rectangle(x, iconY, iconSize, iconSize);

            if (isActive)
            {
                using var borderPen = new Pen(classColor, BorderWidth);
                e.Graphics.DrawRectangle(borderPen, iconRect.X - 1, iconRect.Y - 1, iconRect.Width + 1, iconRect.Height + 1);
            }

            if (_iconCache.TryGetValue(slot.Class, out var icon))
                e.Graphics.DrawImage(icon, iconRect);

            x += iconSize + IconPadding;
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == OverlayWin32.WmNchittest)
        {
            var lParam = m.LParam.ToInt64();
            var screenPoint = new Point((int)(lParam & 0xFFFF), (int)((lParam >> 16) & 0xFFFF));
            var clientPoint = PointToClient(screenPoint);

            if (!HitTestIcon(clientPoint, out _))
            {
                m.Result = (nint)OverlayWin32.HtTransparent;
                return;
            }
        }

        base.WndProc(ref m);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left)
            return;

        if (HitTestIcon(e.Location, out var slotIndex))
            IconClicked?.Invoke(this, slotIndex);
    }

    private bool HitTestIcon(Point clientPoint, out int slotIndex)
    {
        slotIndex = -1;
        var x = StripPadding;
        var centerY = ClientSize.Height / 2;

        for (var i = 0; i < _slots.Count; i++)
        {
            var iconSize = i == _activeIndex ? ActiveIconSize : InactiveIconSize;
            var iconY = centerY - iconSize / 2;
            var iconRect = new Rectangle(x, iconY, iconSize, iconSize);

            if (iconRect.Contains(clientPoint))
            {
                slotIndex = _slots[i].AccountIndex;
                return true;
            }

            x += iconSize + IconPadding;
        }

        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var bitmap in _iconCache.Values)
                bitmap.Dispose();
            _iconCache.Clear();
        }

        base.Dispose(disposing);
    }
}
