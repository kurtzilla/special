namespace Special.Host.Platform;

public interface IHostWindow : IDisposable
{
    bool IsOpen { get; }

    void Show();

    void SetTitle(string title);

    void PumpEvents();

    void Present(float deltaTime);
}
