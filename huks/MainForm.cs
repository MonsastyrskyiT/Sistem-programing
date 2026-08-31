namespace Huks;

public sealed class MainForm : Form
{
    private const int RestrictionSize = 500;
    private readonly Label _statusLabel = new()
    {
        AutoSize = true,
        Font = new Font(SystemFonts.DefaultFont.FontFamily, 10, FontStyle.Bold)
    };
    private GlobalInputHooks? _hooks;

    public MainForm()
    {
        Text = "Глобальні хуки клавіатури та миші";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(680, 330);
        MinimumSize = new Size(600, 300);

        var title = new Label
        {
            Text = "Демонстрація низькорівневих хуків Windows",
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 14, FontStyle.Bold)
        };
        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(620, 0),
            Text =
                "• Ctrl + Shift + Q — миттєво приховати або повернути це вікно.\n\n" +
                "• Утримуйте Alt — курсор буде обмежений невидимим квадратом " +
                "500 × 500 пікселів у центрі основного екрана.\n\n" +
                "Комбінація та обмеження працюють глобально, навіть коли активне інше вікно."
        };
        var closeButton = new Button { Text = "Закрити програму", AutoSize = true };

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(22),
            AutoScroll = true
        };
        layout.Controls.Add(title);
        layout.SetFlowBreak(title, true);
        layout.Controls.Add(instructions);
        layout.SetFlowBreak(instructions, true);
        layout.Controls.Add(_statusLabel);
        layout.SetFlowBreak(_statusLabel, true);
        layout.Controls.Add(closeButton);
        Controls.Add(layout);

        closeButton.Click += (_, _) => Close();
        Shown += MainForm_Shown;
        FormClosed += MainForm_FormClosed;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        Rectangle screen = Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;
        var restriction = new Rectangle(
            screen.Left + (screen.Width - RestrictionSize) / 2,
            screen.Top + (screen.Height - RestrictionSize) / 2,
            RestrictionSize,
            RestrictionSize);

        try
        {
            _hooks = new GlobalInputHooks(restriction);
            _hooks.VisibilityToggleRequested += Hooks_VisibilityToggleRequested;
            _statusLabel.Text =
                $"Хуки активні. Межі: X={restriction.Left}..{restriction.Right - 1}, " +
                $"Y={restriction.Top}..{restriction.Bottom - 1}.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Не вдалося активувати хуки.";
            MessageBox.Show(this, ex.Message, "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Hooks_VisibilityToggleRequested(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;

        BeginInvoke(new Action(() =>
        {
            Visible = !Visible;
            if (Visible)
            {
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }));
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        if (_hooks is not null)
        {
            _hooks.VisibilityToggleRequested -= Hooks_VisibilityToggleRequested;
            _hooks.Dispose();
            _hooks = null;
        }
    }
}
