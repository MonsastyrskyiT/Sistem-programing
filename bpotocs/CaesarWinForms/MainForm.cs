namespace CaesarWinForms;

public sealed class MainForm : Form
{
    private readonly TextBox _pathTextBox = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _shiftInput = new() { Minimum = -1000, Maximum = 1000, Value = 3, Width = 100 };
    private readonly Button _browseButton = new() { Text = "Огляд...", AutoSize = true };
    private readonly Button _encryptButton = new() { Text = "Зашифрувати", AutoSize = true };
    private readonly Button _cancelButton = new() { Text = "Скасувати", AutoSize = true, Enabled = false };
    private readonly ProgressBar _progressBar = new() { Dock = DockStyle.Fill };
    private readonly Label _statusLabel = new() { Text = "Оберіть текстовий файл.", AutoSize = true };
    private CancellationTokenSource? _cancellation;

    public MainForm()
    {
        Text = "Шифр Цезаря — багатопоточність";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(650, 260);
        ClientSize = new Size(720, 280);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 3,
            RowCount = 5
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "Файл:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_pathTextBox, 1, 0);
        layout.Controls.Add(_browseButton, 2, 0);
        layout.Controls.Add(new Label { Text = "Зсув:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_shiftInput, 1, 1);
        layout.Controls.Add(_progressBar, 0, 2);
        layout.SetColumnSpan(_progressBar, 3);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { _encryptButton, _cancelButton });
        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 3);
        layout.Controls.Add(_statusLabel, 0, 4);
        layout.SetColumnSpan(_statusLabel, 3);
        Controls.Add(layout);

        _browseButton.Click += BrowseButton_Click;
        _encryptButton.Click += EncryptButton_Click;
        _cancelButton.Click += (_, _) => _cancellation?.Cancel();
        FormClosing += (_, _) => _cancellation?.Cancel();
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Оберіть текстовий файл",
            Filter = "Текстові файли (*.txt)|*.txt|Усі файли (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) _pathTextBox.Text = dialog.FileName;
    }

    private async void EncryptButton_Click(object? sender, EventArgs e)
    {
        string inputPath = _pathTextBox.Text.Trim().Trim('"');
        if (!File.Exists(inputPath))
        {
            MessageBox.Show(this, "Вказаний файл не знайдено.", "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string outputPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(inputPath))!,
            $"{Path.GetFileNameWithoutExtension(inputPath)}_encrypted{Path.GetExtension(inputPath)}");
        _cancellation = new CancellationTokenSource();
        SetRunningState(true);
        _progressBar.Value = 0;
        _statusLabel.Text = "Шифрування виконується в окремому потоці...";
        var progress = new Progress<int>(value => _progressBar.Value = value);

        try
        {
            await Task.Run(() => CaesarCipher.EncryptFile(inputPath, outputPath,
                (int)_shiftInput.Value, _cancellation.Token, progress), _cancellation.Token);
            _statusLabel.Text = $"Готово: {outputPath}";
            MessageBox.Show(this, $"Шифрування завершено.\n\n{outputPath}", "Готово",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Шифрування скасовано користувачем.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Сталася помилка.";
            MessageBox.Show(this, ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            SetRunningState(false);
        }
    }

    private void SetRunningState(bool running)
    {
        _pathTextBox.Enabled = !running;
        _browseButton.Enabled = !running;
        _shiftInput.Enabled = !running;
        _encryptButton.Enabled = !running;
        _cancelButton.Enabled = running;
    }
}
