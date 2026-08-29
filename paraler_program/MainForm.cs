namespace ParalerProgram;

public sealed class MainForm : Form
{
    private readonly TextBox _directoryTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _extensionTextBox = new() { Text = ".txt", Width = 130 };
    private readonly Button _browseButton = new() { Text = "Огляд...", AutoSize = true };
    private readonly Button _searchButton = new() { Text = "Почати пошук", AutoSize = true };
    private readonly TextBox _resultTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10)
    };
    private readonly Label _statusLabel = new() { Text = "Введіть каталог і розширення файлу.", AutoSize = true };

    public MainForm()
    {
        Text = "Паралельний пошук файлів";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 420);
        ClientSize = new Size(780, 470);

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

        layout.Controls.Add(new Label { Text = "Каталог:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        layout.Controls.Add(_directoryTextBox, 1, 0);
        layout.Controls.Add(_browseButton, 2, 0);
        layout.Controls.Add(new Label { Text = "Розширення:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_extensionTextBox, 1, 1);
        layout.Controls.Add(_searchButton, 1, 2);
        layout.Controls.Add(_statusLabel, 0, 3);
        layout.SetColumnSpan(_statusLabel, 3);
        layout.Controls.Add(_resultTextBox, 0, 4);
        layout.SetColumnSpan(_resultTextBox, 3);
        Controls.Add(layout);

        AcceptButton = _searchButton;
        _browseButton.Click += BrowseButton_Click;
        _searchButton.Click += SearchButton_Click;
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Оберіть каталог для пошуку",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _directoryTextBox.Text = dialog.SelectedPath;
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        string directory = _directoryTextBox.Text.Trim().Trim('"');
        string extension = NormalizeExtension(_extensionTextBox.Text);

        if (!Directory.Exists(directory))
        {
            MessageBox.Show(this, "Вказаний каталог не існує.", "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (extension.Length < 2 || extension.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show(this, "Введіть коректне розширення, наприклад .txt або jpg.",
                "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SetControlsEnabled(false);
        _resultTextBox.Clear();

        try
        {
            _statusLabel.Text = "Виконується послідовний пошук...";
            SearchResult sequential = await Task.Run(
                () => FileSearchService.SearchSequential(directory, extension));

            _statusLabel.Text = "Виконується паралельний пошук...";
            SearchResult parallel = await Task.Run(
                () => FileSearchService.SearchParallel(directory, extension));

            _resultTextBox.Text =
                $"Розширення: {extension}{Environment.NewLine}" +
                $"Каталог: {directory}{Environment.NewLine}{Environment.NewLine}" +
                $"ПОСЛІДОВНИЙ АЛГОРИТМ{Environment.NewLine}" +
                $"Знайдено файлів: {sequential.FileCount}{Environment.NewLine}" +
                $"Час: {sequential.Elapsed.TotalMilliseconds:F3} мс{Environment.NewLine}" +
                $"Помилок доступу: {sequential.AccessErrors}{Environment.NewLine}{Environment.NewLine}" +
                $"ПАРАЛЕЛЬНИЙ АЛГОРИТМ{Environment.NewLine}" +
                $"Знайдено файлів: {parallel.FileCount}{Environment.NewLine}" +
                $"Час: {parallel.Elapsed.TotalMilliseconds:F3} мс{Environment.NewLine}" +
                $"Помилок доступу: {parallel.AccessErrors}";

            _statusLabel.Text = "Пошук завершено.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Пошук завершився помилкою.";
            MessageBox.Show(this, ex.Message, "Помилка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private static string NormalizeExtension(string value)
    {
        string extension = value.Trim();
        if (extension.StartsWith("*.", StringComparison.Ordinal)) extension = extension[1..];
        if (extension.Length > 0 && !extension.StartsWith('.')) extension = "." + extension;
        return extension;
    }

    private void SetControlsEnabled(bool enabled)
    {
        _directoryTextBox.Enabled = enabled;
        _extensionTextBox.Enabled = enabled;
        _browseButton.Enabled = enabled;
        _searchButton.Enabled = enabled;
    }
}
