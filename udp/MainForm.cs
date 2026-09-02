using System.ComponentModel;
using System.Net;

namespace UdpChat;

public sealed class MainForm : Form
{
    private readonly TextBox _usernameTextBox = new() { Width = 120 };
    private readonly NumericUpDown _localPortInput = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = 6000,
        Width = 85
    };
    private readonly Button _startButton = new() { Text = "Запустити прийом", AutoSize = true };
    private readonly TextBox _chatNameTextBox = new() { Width = 110 };
    private readonly TextBox _remoteIpTextBox = new() { Text = "127.0.0.1", Width = 110 };
    private readonly NumericUpDown _remotePortInput = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = 6001,
        Width = 85
    };
    private readonly Button _addChatButton = new() { Text = "Додати чат", AutoSize = true };
    private readonly ListBox _chatList = new() { Dock = DockStyle.Fill };
    private readonly TextBox _historyTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly TextBox _messageTextBox = new() { Dock = DockStyle.Fill };
    private readonly Button _sendButton = new()
    {
        Text = "Надіслати",
        AutoSize = true,
        Enabled = false
    };
    private readonly Label _statusLabel = new() { Text = "Приймання не запущено.", AutoSize = true };
    private readonly UdpChatService _chatService = new();
    private readonly ChatHistoryStore _historyStore = new();
    private readonly BindingList<ChatConversation> _chats;
    private readonly ChatApplicationState _state;

    public MainForm()
    {
        _state = _historyStore.Load();
        _chats = new BindingList<ChatConversation>(_state.Chats);
        _usernameTextBox.Text = _state.Username;
        if (_state.LocalPort is >= 1 and <= 65535) _localPortInput.Value = _state.LocalPort;

        Text = "UDP Chat";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 570);
        MinimumSize = new Size(760, 480);

        var settingsPanel = CreateSettingsPanel();
        var addChatPanel = CreateAddChatPanel();
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 210,
            FixedPanel = FixedPanel.Panel1
        };
        split.Panel1.Controls.Add(_chatList);
        split.Panel2.Controls.Add(_historyTextBox);

        var sendPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        sendPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sendPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sendPanel.Controls.Add(_messageTextBox, 0, 0);
        sendPanel.Controls.Add(_sendButton, 1, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 5
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(settingsPanel, 0, 0);
        layout.Controls.Add(addChatPanel, 0, 1);
        layout.Controls.Add(split, 0, 2);
        layout.Controls.Add(sendPanel, 0, 3);
        layout.Controls.Add(_statusLabel, 0, 4);
        Controls.Add(layout);

        _chatList.DataSource = _chats;
        _chatList.SelectedIndexChanged += (_, _) => RenderSelectedChat();
        _startButton.Click += StartButton_Click;
        _addChatButton.Click += AddChatButton_Click;
        _sendButton.Click += SendButton_Click;
        _chatService.DatagramReceived += ChatService_DatagramReceived;
        _chatService.ReceiveError += ChatService_ReceiveError;
        FormClosed += MainForm_FormClosed;

        if (_chats.Count > 0) _chatList.SelectedIndex = 0;
    }

    private FlowLayoutPanel CreateSettingsPanel()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        panel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Ваше ім'я:", AutoSize = true, Anchor = AnchorStyles.Left },
            _usernameTextBox,
            new Label { Text = "Локальний порт:", AutoSize = true, Anchor = AnchorStyles.Left },
            _localPortInput,
            _startButton
        });
        return panel;
    }

    private FlowLayoutPanel CreateAddChatPanel()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        panel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Назва чату:", AutoSize = true, Anchor = AnchorStyles.Left },
            _chatNameTextBox,
            new Label { Text = "IP:", AutoSize = true, Anchor = AnchorStyles.Left },
            _remoteIpTextBox,
            new Label { Text = "Порт:", AutoSize = true, Anchor = AnchorStyles.Left },
            _remotePortInput,
            _addChatButton
        });
        return panel;
    }

    private void StartButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_usernameTextBox.Text))
        {
            ShowError("Введіть ваше ім'я.");
            return;
        }

        try
        {
            _chatService.Start((int)_localPortInput.Value);
            _usernameTextBox.Enabled = false;
            _localPortInput.Enabled = false;
            _startButton.Enabled = false;
            _sendButton.Enabled = true;
            _statusLabel.Text = $"Очікування UDP-повідомлень на порту {_localPortInput.Value}.";
            SaveState();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void AddChatButton_Click(object? sender, EventArgs e)
    {
        if (!IPAddress.TryParse(_remoteIpTextBox.Text.Trim(), out IPAddress? address))
        {
            ShowError("Введіть коректну IP-адресу отримувача.");
            return;
        }

        int port = (int)_remotePortInput.Value;
        string name = _chatNameTextBox.Text.Trim();
        if (name.Length == 0) name = $"{address}:{port}";

        ChatConversation? existing = FindChat(address.ToString(), port);
        if (existing is not null)
        {
            _chatList.SelectedItem = existing;
            _statusLabel.Text = "Чат із такою адресою вже існує.";
            return;
        }

        var chat = new ChatConversation
        {
            Name = name,
            RemoteAddress = address.ToString(),
            RemotePort = port
        };
        _chats.Add(chat);
        _chatList.SelectedItem = chat;
        _chatNameTextBox.Clear();
        SaveState();
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        if (_chatList.SelectedItem is not ChatConversation chat)
        {
            ShowError("Оберіть або додайте чат.");
            return;
        }

        string senderName = _usernameTextBox.Text.Trim();
        string message = _messageTextBox.Text.Trim();
        if (senderName.Length == 0 || message.Length == 0) return;
        if (message.Length > 4000)
        {
            ShowError("Повідомлення не може перевищувати 4000 символів.");
            return;
        }

        try
        {
            _sendButton.Enabled = false;
            await _chatService.SendAsync(
                IPAddress.Parse(chat.RemoteAddress),
                chat.RemotePort,
                senderName,
                message);

            chat.Messages.Add(new StoredChatMessage
            {
                Sender = senderName,
                Text = message,
                SentAt = DateTime.Now,
                IsOutgoing = true
            });
            _messageTextBox.Clear();
            RenderSelectedChat();
            SaveState();
            _statusLabel.Text = "Повідомлення надіслано.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            _sendButton.Enabled = true;
        }
    }

    private void ChatService_DatagramReceived(object? sender, DatagramReceivedEventArgs e)
    {
        if (IsDisposed || Disposing) return;

        BeginInvoke(new Action(() =>
        {
            ChatConversation? chat = FindChat(
                e.RemoteEndPoint.Address.ToString(),
                e.RemoteEndPoint.Port);

            if (chat is null)
            {
                chat = new ChatConversation
                {
                    Name = e.RemoteEndPoint.ToString(),
                    RemoteAddress = e.RemoteEndPoint.Address.ToString(),
                    RemotePort = e.RemoteEndPoint.Port
                };
                _chats.Add(chat);
            }

            chat.Messages.Add(new StoredChatMessage
            {
                Sender = e.Packet.Sender,
                Text = e.Packet.Message,
                SentAt = e.Packet.SentAtUtc.ToLocalTime(),
                IsOutgoing = false
            });

            if (ReferenceEquals(_chatList.SelectedItem, chat)) RenderSelectedChat();
            SaveState();
            _statusLabel.Text = $"Нове повідомлення у чаті «{chat.Name}».";
        }));
    }

    private void ChatService_ReceiveError(object? sender, string error)
    {
        if (IsDisposed || Disposing) return;
        BeginInvoke(new Action(() => _statusLabel.Text = "Помилка приймання: " + error));
    }

    private ChatConversation? FindChat(string address, int port) =>
        _chats.FirstOrDefault(chat =>
            chat.RemotePort == port &&
            string.Equals(chat.RemoteAddress, address, StringComparison.OrdinalIgnoreCase));

    private void RenderSelectedChat()
    {
        _historyTextBox.Clear();
        if (_chatList.SelectedItem is not ChatConversation chat) return;

        foreach (StoredChatMessage message in chat.Messages)
        {
            string direction = message.IsOutgoing ? "Ви" : message.Sender;
            _historyTextBox.AppendText(
                $"[{message.SentAt:HH:mm:ss}] [{direction}]: {message.Text}{Environment.NewLine}");
        }

        _historyTextBox.SelectionStart = _historyTextBox.TextLength;
        _historyTextBox.ScrollToCaret();
    }

    private void SaveState()
    {
        try
        {
            _state.Username = _usernameTextBox.Text.Trim();
            _state.LocalPort = (int)_localPortInput.Value;
            _historyStore.Save(_state);
        }
        catch (IOException ex)
        {
            _statusLabel.Text = "Не вдалося зберегти історію: " + ex.Message;
        }
    }

    private void ShowError(string message) =>
        MessageBox.Show(this, message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        SaveState();
        _chatService.DatagramReceived -= ChatService_DatagramReceived;
        _chatService.ReceiveError -= ChatService_ReceiveError;
        _chatService.Dispose();
    }
}
