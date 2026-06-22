using System.Drawing;
using System.Drawing.Drawing2D;
using ParadowSync.Core.Catalog;

namespace ParadowSync.Windows.Overlay;

internal sealed class WindowBadge : IDisposable
{
    private const int IconSize = 16;
    private const int BorderWidth = 2;
    private const int BadgePadding = 4;

    private readonly BadgeForm _badge;
    private readonly BorderForm _border;
    private nint _gameHwnd;
    private bool _isActive;
    private bool _disposed;

    public WindowBadge(
        nint gameHwnd,
        string character,
        string className,
        IconLoader iconLoader)
    {
        _gameHwnd = gameHwnd;
        _badge = new BadgeForm(character, className, iconLoader);
        _border = new BorderForm(className);
    }

    public void UpdatePosition(nint gameHwnd)
    {
        _gameHwnd = gameHwnd;
        if (_gameHwnd == nint.Zero || !OverlayWin32.GetWindowRect(_gameHwnd, out var rect))
            return;

        _badge.PositionAt(rect.Left, rect.Top);
        _border.PositionAround(rect);
    }

    public void SetActive(bool active)
    {
        _isActive = active;
        _border.SetActive(active);
        _border.UpdateIfActive(_gameHwnd);
    }

    public void Show()
    {
        _badge.Show();
        if (_isActive)
            _border.Show();
    }

    public void SetVisible(bool visible)
    {
        _badge.Visible = visible;
        _border.Visible = visible && _isActive;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _badge.Dispose();
        _border.Dispose();
    }

    private sealed class BadgeForm : Form
    {
        private readonly string _character;
        private readonly string _className;
        private readonly IconLoader _iconLoader;
        private Bitmap? _icon;

        public BadgeForm(string character, string className, IconLoader iconLoader)
        {
            _character = character;
            _className = className;
            _iconLoader = iconLoader;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            _icon = OverlayWin32.LoadIconBitmap(iconLoader, className);
            AutoSizeToContent();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= OverlayWin32.WsExLayered
                    | OverlayWin32.WsExNoactivate
                    | OverlayWin32.WsExTopmost
                    | OverlayWin32.WsExTransparent;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _ = OverlayWin32.SetLayeredWindowAttributes(Handle, 0, 255, OverlayWin32.LwaAlpha);
        }

        public void PositionAt(int x, int y) => Location = new Point(x, y);

        private void AutoSizeToContent()
        {
            using var g = CreateGraphics();
            var textWidth = (int)g.MeasureString(_character, SystemFonts.DefaultFont).Width;
            ClientSize = new Size(BadgePadding * 2 + IconSize + 4 + textWidth, BadgePadding * 2 + IconSize);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(220, 20, 20, 24));

            var iconRect = new Rectangle(BadgePadding, BadgePadding, IconSize, IconSize);
            if (_icon is not null)
                e.Graphics.DrawImage(_icon, iconRect);

            var textRect = new Rectangle(iconRect.Right + 4, BadgePadding, ClientSize.Width - iconRect.Right - 4, IconSize);
            TextRenderer.DrawText(
                e.Graphics,
                _character,
                SystemFonts.DefaultFont,
                textRect,
                Color.White,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _icon?.Dispose();
                _icon = null;
            }

            base.Dispose(disposing);
        }
    }

    private sealed class BorderForm : Form
    {
        private readonly Color _classColor;
        private bool _active;

        public BorderForm(string className)
        {
            _classColor = OverlayWin32.ParseClassColor(ClassCatalog.Get(className)?.ColorHex);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Visible = false;
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

        public void SetActive(bool active)
        {
            _active = active;
            Visible = _active;
        }

        public void UpdateIfActive(nint gameHwnd)
        {
            if (!_active || gameHwnd == nint.Zero || !OverlayWin32.GetWindowRect(gameHwnd, out var rect))
                return;

            PositionAround(rect);
        }

        public void PositionAround(OverlayWin32.RECT rect)
        {
            Bounds = new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(Color.Transparent);
            using var pen = new Pen(_classColor, BorderWidth);
            var inset = BorderWidth / 2;
            e.Graphics.DrawRectangle(
                pen,
                inset,
                inset,
                ClientSize.Width - BorderWidth,
                ClientSize.Height - BorderWidth);
        }
    }
}
