using System.Drawing;
using System.Windows.Forms;

namespace Special.Host.Platform;

public sealed class WinFormsHostWindow : IHostWindow
{
    readonly HostForm _form;
    float _visualTime;

    public WinFormsHostWindow(string title, int width, int height)
    {
        _form = new HostForm
        {
            Text = title,
            ClientSize = new Size(width, height),
            StartPosition = FormStartPosition.CenterScreen,
        };

        _form.Paint += HandlePaint;
        _form.FormClosed += HandleFormClosed;
        IsOpen = true;
    }

    public bool IsOpen { get; private set; }

    public void Show() => _form.Show();

    public void SetTitle(string title) => _form.Text = title;

    public void PumpEvents() => Application.DoEvents();

    public void Present(float deltaTime)
    {
        _visualTime += deltaTime;
        _form.Invalidate();
    }

    public void Dispose()
    {
        _form.Paint -= HandlePaint;
        _form.FormClosed -= HandleFormClosed;
        _form.Dispose();
    }

    void HandleFormClosed(object? sender, FormClosedEventArgs e) => IsOpen = false;

    void HandlePaint(object? sender, PaintEventArgs e)
    {
        var graphics = e.Graphics;
        var client = _form.ClientSize;
        graphics.Clear(Color.FromArgb(28, 36, 54));

        const int rectSize = 56;
        var x = (client.Width - rectSize) * (0.5f + 0.4f * MathF.Sin(_visualTime * 2f));
        var y = (client.Height - rectSize) * (0.5f + 0.4f * MathF.Cos(_visualTime * 1.4f));
        graphics.FillRectangle(Brushes.DeepSkyBlue, x, y, rectSize, rectSize);
    }

    sealed class HostForm : Form
    {
        public HostForm()
        {
            DoubleBuffered = true;
        }
    }
}
