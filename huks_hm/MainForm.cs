namespace HuksHomework;

public sealed class MainForm : Form
{
    private readonly TextBox _testTextBox = new()
    {
        Dock = DockStyle.Top,
        Multiline = true,
        Height = 75,
        PlaceholderText = "Введіть тут текст для перевірки клавіатурного хука..."
    };
    private readonly Button _loggingButton = new()
    {
        Text = "Старт логування",
        AutoSize = true
    };
    private readonly Label _loggingStatus = new()
    {
        Text = "Логування вимкнено.",
        AutoSize = true
    };
    private readonly Panel _playground = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.AliceBlue,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly Button _escapeButton = new()
    {
        Text = "Спробуй натиснути!",
        Size = new Size(165, 42),
        Location = new Point(40, 40)
    };
    private readonly KeyboardLogger _keyboardLogger = new();
    private readonly object _buttonBoundsLock = new();
    private GlobalMouseHook? _mouseHook;
    private Rectangle _buttonScreenBounds;

    public MainForm()
    {
        Text = "Глобальні хуки — домашнє завдання";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(760, 530);
        MinimumSize = new Size(650, 430);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(14),
            ColumnCount = 2,
            RowCount = 4
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            Text = "Тестове поле:",
            AutoSize = true,
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 10, FontStyle.Bold)
        }, 0, 0);
        header.Controls.Add(_testTextBox, 0, 1);
        header.SetColumnSpan(_testTextBox, 2);
        header.Controls.Add(_loggingButton, 0, 2);
        header.Controls.Add(_loggingStatus, 0, 3);
        header.SetColumnSpan(_loggingStatus, 2);

        _playground.Controls.Add(_escapeButton);
        Controls.Add(_playground);
        Controls.Add(header);

        _loggingButton.Click += LoggingButton_Click;
        _escapeButton.LocationChanged += (_, _) => UpdateButtonScreenBounds();
        LocationChanged += (_, _) => UpdateButtonScreenBounds();
        Resize += (_, _) => UpdateButtonScreenBounds();
        Shown += MainForm_Shown;
        FormClosed += MainForm_FormClosed;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        UpdateButtonScreenBounds();

        try
        {
            _mouseHook = new GlobalMouseHook();
            _mouseHook.LeftButtonDown += MouseHook_LeftButtonDown;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Помилка хука миші",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoggingButton_Click(object? sender, EventArgs e)
    {
        if (_keyboardLogger.IsRunning)
        {
            _keyboardLogger.Stop();
            _loggingButton.Text = "Старт логування";
            _loggingStatus.Text = "Логування вимкнено.";
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Оберіть файл журналу клавіш",
            Filter = "Текстовий файл (*.txt)|*.txt",
            FileName = $"keyboard-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt",
            AddExtension = true,
            DefaultExt = "txt"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _keyboardLogger.Start(dialog.FileName);
            _loggingButton.Text = "Стоп логування";
            _loggingStatus.Text = $"Логування увімкнено. Файл: {dialog.FileName}";
            _testTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Помилка хука клавіатури",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private bool MouseHook_LeftButtonDown(Point screenPoint)
    {
        Rectangle bounds;
        lock (_buttonBoundsLock)
        {
            bounds = _buttonScreenBounds;
        }

        if (!bounds.Contains(screenPoint)) return false;

        // Координати змінюються в UI-потоці, але клік блокується відразу в хуку.
        if (!IsDisposed && !Disposing)
            BeginInvoke(new Action(MoveEscapeButton));

        return true;
    }

    private void MoveEscapeButton()
    {
        int maximumX = Math.Max(0, _playground.ClientSize.Width - _escapeButton.Width);
        int maximumY = Math.Max(0, _playground.ClientSize.Height - _escapeButton.Height);

        _escapeButton.Location = new Point(
            Random.Shared.Next(maximumX + 1),
            Random.Shared.Next(maximumY + 1));
    }

    private void UpdateButtonScreenBounds()
    {
        if (!IsHandleCreated || !_escapeButton.IsHandleCreated) return;

        Rectangle bounds = _escapeButton.RectangleToScreen(_escapeButton.ClientRectangle);
        lock (_buttonBoundsLock)
        {
            _buttonScreenBounds = bounds;
        }
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _keyboardLogger.Dispose();

        if (_mouseHook is not null)
        {
            _mouseHook.LeftButtonDown -= MouseHook_LeftButtonDown;
            _mouseHook.Dispose();
            _mouseHook = null;
        }
    }
}
