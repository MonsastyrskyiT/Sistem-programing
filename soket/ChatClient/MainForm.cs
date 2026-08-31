using ChatProtocol;

namespace ChatClient;

public sealed class MainForm : Form
{
    private readonly TextBox _hostTextBox = new() { Text = "127.0.0.1", Width = 120 };
    private readonly NumericUpDown _portInput = new() { Minimum = 1, Maximum = 65535, Value = 5000, Width = 90 };
    private readonly TextBox _usernameTextBox = new() { Width = 140 };
    private readonly Button _connectButton = new() { Text = "Підключитися", AutoSize = true };
    private readonly TextBox _historyTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly TextBox _messageTextBox = new() { Dock = DockStyle.Fill };
    private readonly Button _sendButton = new() { Text = "Надіслати", AutoSize = true, Enabled = false };
    private readonly Button _updateButton = new() { Text = "Оновити", AutoSize = true, Enabled = false };
    private readonly Label _statusLabel = new() { Text = "Не підключено.", AutoSize = true };
    private readonly TcpChatClient _chatClient = new();
    private long _lastMessageId;

    public MainForm()
    {
        Text = "TCP-чат";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(780, 520);
        MinimumSize = new Size(680, 430);

        var connectionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        connectionPanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Сервер:", AutoSize = true, Anchor = AnchorStyles.Left },
            _hostTextBox,
            new Label { Text = "Порт:", AutoSize = true, Anchor = AnchorStyles.Left },
            _portInput,
            new Label { Text = "Юзернейм:", AutoSize = true, Anchor = AnchorStyles.Left },
            _usernameTextBox,
            _connectButton
        });

        var messagePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        messagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        messagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        messagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        messagePanel.Controls.Add(_messageTextBox, 0, 0);
        messagePanel.Controls.Add(_sendButton, 1, 0);
        messagePanel.Controls.Add(_updateButton, 2, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(connectionPanel, 0, 0);
        layout.Controls.Add(_historyTextBox, 0, 1);
        layout.Controls.Add(messagePanel, 0, 2);
        layout.Controls.Add(_statusLabel, 0, 3);
        Controls.Add(layout);

        _connectButton.Click += ConnectButton_Click;
        _sendButton.Click += SendButton_Click;
        _updateButton.Click += UpdateButton_Click;
        FormClosed += (_, _) => _chatClient.Dispose();
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        string username = _usernameTextBox.Text.Trim();
        if (username.Length == 0)
        {
            ShowError("Введіть юзернейм.");
            return;
        }

        SetBusy(true);
        try
        {
            await _chatClient.ConnectAsync(_hostTextBox.Text.Trim(), (int)_portInput.Value, username);
            _lastMessageId = 0;
            _historyTextBox.Clear();
            SetConnected(true);
            _statusLabel.Text = $"Підключено як {username}.";
            await RefreshMessagesAsync();
        }
        catch (Exception ex)
        {
            SetConnected(false);
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        string message = _messageTextBox.Text.Trim();
        if (message.Length == 0) return;

        SetBusy(true);
        try
        {
            await _chatClient.SendMessageAsync(message);
            _messageTextBox.Clear();
            await RefreshMessagesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void UpdateButton_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await RefreshMessagesAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshMessagesAsync()
    {
        List<ChatMessage> messages = await _chatClient.GetNewMessagesAsync(_lastMessageId);
        foreach (ChatMessage message in messages)
        {
            _historyTextBox.AppendText($"[{message.Username}]: {message.Text}{Environment.NewLine}");
            _lastMessageId = Math.Max(_lastMessageId, message.Id);
        }

        _statusLabel.Text = messages.Count == 0
            ? "Нових повідомлень немає."
            : $"Отримано нових повідомлень: {messages.Count}.";
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        _connectButton.Enabled = !busy && !_chatClient.IsConnected;
        _sendButton.Enabled = !busy && _chatClient.IsConnected;
        _updateButton.Enabled = !busy && _chatClient.IsConnected;
    }

    private void SetConnected(bool connected)
    {
        _hostTextBox.Enabled = !connected;
        _portInput.Enabled = !connected;
        _usernameTextBox.Enabled = !connected;
        _connectButton.Enabled = !connected;
        _sendButton.Enabled = connected;
        _updateButton.Enabled = connected;
    }

    private void ShowError(string message)
    {
        _statusLabel.Text = "Помилка.";
        MessageBox.Show(this, message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
