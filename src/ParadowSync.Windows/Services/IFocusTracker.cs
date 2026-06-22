namespace ParadowSync.Windows.Services;

public interface IFocusTracker : IDisposable
{
    event EventHandler<nint>? ForegroundChanged;
    void Start();
    void Stop();
}
