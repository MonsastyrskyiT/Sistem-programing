using ExamChat.Protocol;

namespace ExamChat.Client;

public sealed class MainForm : Form
{
    private readonly TcpChatClient _client = new();
    private readonly TextBox _host = new() { Text = "127.0.0.1", Width = 110 };
    private readonly NumericUpDown _port = new() { Minimum = 1, Maximum = 65535, Value = 5004, Width = 80 };
    private readonly Button _connect = new() { Text = "Підключитися", AutoSize = true };
    private readonly TextBox _username = new() { Width = 120 };
    private readonly TextBox _password = new() { Width = 120, UseSystemPasswordChar = true };
    private readonly Button _register = new() { Text = "Реєстрація", AutoSize = true, Enabled = false };
    private readonly Button _login = new() { Text = "Увійти", AutoSize = true, Enabled = false };
    private readonly Label _status = new() { Text = "Не підключено.", AutoSize = true };
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill, Enabled = false };

    private readonly ListBox _onlineUsers = new() { Dock = DockStyle.Fill };
    private readonly TextBox _privateTarget = new() { Width = 130, PlaceholderText = "Юзернейм отримувача" };
    private readonly TextBox _privateHistory = HistoryBox();
    private readonly TextBox _privateMessage = new() { Dock = DockStyle.Fill };
    private readonly Button _sendPrivate = new() { Text = "Надіслати особисто", AutoSize = true };
    private readonly Button _refreshUsers = new() { Text = "Оновити користувачів", AutoSize = true };

    private readonly ComboBox _groups = new() { Width = 170, DropDownStyle = ComboBoxStyle.DropDown };
    private readonly Button _createGroup = new() { Text = "Створити", AutoSize = true };
    private readonly Button _joinGroup = new() { Text = "Приєднатися", AutoSize = true };
    private readonly Button _refreshGroups = new() { Text = "Оновити", AutoSize = true };
    private readonly TextBox _groupHistory = HistoryBox();
    private readonly TextBox _groupMessage = new() { Dock = DockStyle.Fill };
    private readonly Button _sendGroup = new() { Text = "Надіслати в групу", AutoSize = true };
    private string? _currentUsername;

    public MainForm()
    {
        Text = "Екзаменаційний чат";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(940, 650);
        MinimumSize = new Size(780, 520);

        var connectionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        connectionPanel.Controls.AddRange(new Control[]
        {
            LabelFor("Сервер:"), _host, LabelFor("Порт:"), _port, _connect,
            LabelFor("Юзернейм:"), _username, LabelFor("Пароль:"), _password,
            _register, _login
        });

        _tabs.TabPages.Add(CreatePrivateTab());
        _tabs.TabPages.Add(CreateGroupsTab());

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(connectionPanel, 0, 0);
        root.Controls.Add(_tabs, 0, 1);
        root.Controls.Add(_status, 0, 2);
        Controls.Add(root);

        _connect.Click += Connect_Click;
        _register.Click += Register_Click;
        _login.Click += Login_Click;
        _refreshUsers.Click += RefreshUsers_Click;
        _onlineUsers.DoubleClick += (_, _) =>
        {
            if (_onlineUsers.SelectedItem is string user) _privateTarget.Text = user;
        };
        _sendPrivate.Click += SendPrivate_Click;
        _createGroup.Click += CreateGroup_Click;
        _joinGroup.Click += JoinGroup_Click;
        _refreshGroups.Click += RefreshGroups_Click;
        _sendGroup.Click += SendGroup_Click;
        _client.EventReceived += Client_EventReceived;
        _client.ConnectionLost += Client_ConnectionLost;
        FormClosed += MainForm_FormClosed;
    }

    private TabPage CreatePrivateTab()
    {
        var page = new TabPage("Особисті повідомлення");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 210 };

        var usersPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        usersPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        usersPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        usersPanel.Controls.Add(_onlineUsers, 0, 0);
        usersPanel.Controls.Add(_refreshUsers, 0, 1);
        split.Panel1.Controls.Add(usersPanel);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.Controls.Add(_privateTarget, 0, 0);
        right.Controls.Add(_privateHistory, 0, 1);
        right.Controls.Add(CreateSendPanel(_privateMessage, _sendPrivate), 0, 2);
        split.Panel2.Controls.Add(right);
        page.Controls.Add(split);
        return page;
    }

    private TabPage CreateGroupsTab()
    {
        var page = new TabPage("Групові чати");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var groupControls = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        groupControls.Controls.AddRange(new Control[]
        {
            LabelFor("Назва групи:"), _groups, _createGroup, _joinGroup, _refreshGroups
        });
        layout.Controls.Add(groupControls, 0, 0);
        layout.Controls.Add(_groupHistory, 0, 1);
        layout.Controls.Add(CreateSendPanel(_groupMessage, _sendGroup), 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private async void Connect_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            await _client.ConnectAsync(_host.Text.Trim(), (int)_port.Value);
            _connect.Enabled = false;
            _host.Enabled = false;
            _port.Enabled = false;
            _register.Enabled = true;
            _login.Enabled = true;
            _status.Text = "З'єднання встановлено. Зареєструйтесь або увійдіть.";
        });
    }

    private async void Register_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            ServerMessage response = await _client.SendRequestAsync(AuthRequest(RequestTypes.Register));
            EnsureSuccess(response);
            _status.Text = response.Text;
            MessageBox.Show(this, response.Text, "Реєстрація", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });
    }

    private async void Login_Click(object? sender, EventArgs e)
    {
        await RunUiActionAsync(async () =>
        {
            ServerMessage response = await _client.SendRequestAsync(AuthRequest(RequestTypes.Login));
            EnsureSuccess(response);
            _currentUsername = _username.Text.Trim();
            _tabs.Enabled = true;
            _username.Enabled = false;
            _password.Enabled = false;
            _register.Enabled = false;
            _login.Enabled = false;
            _status.Text = response.Text;
            await RefreshUsersAsync();
            await RefreshGroupsAsync();
        });
    }

    private async void RefreshUsers_Click(object? sender, EventArgs e) =>
        await RunUiActionAsync(RefreshUsersAsync);

    private async Task RefreshUsersAsync()
    {
        ServerMessage response = await _client.SendRequestAsync(new ClientRequest
        {
            Type = RequestTypes.ListUsers
        });
        EnsureSuccess(response);
        _onlineUsers.Items.Clear();
        _onlineUsers.Items.AddRange(response.Items.Where(name =>
            !string.Equals(name, _currentUsername, StringComparison.OrdinalIgnoreCase)).ToArray());
    }

    private async void SendPrivate_Click(object? sender, EventArgs e)
    {
        string target = _privateTarget.Text.Trim();
        string text = _privateMessage.Text.Trim();
        if (target.Length == 0 || text.Length == 0) return;

        await RunUiActionAsync(async () =>
        {
            ServerMessage response = await _client.SendRequestAsync(new ClientRequest
            {
                Type = RequestTypes.PrivateMessage,
                TargetUsername = target,
                Text = text
            });
            EnsureSuccess(response);
            Append(_privateHistory, $"[Ви → {target}]: {text}");
            _privateMessage.Clear();
            _status.Text = response.Text;
        });
    }

    private async void CreateGroup_Click(object? sender, EventArgs e) =>
        await ChangeGroupAsync(RequestTypes.CreateGroup);

    private async void JoinGroup_Click(object? sender, EventArgs e) =>
        await ChangeGroupAsync(RequestTypes.JoinGroup);

    private async Task ChangeGroupAsync(string type)
    {
        string group = _groups.Text.Trim();
        if (group.Length == 0) return;

        await RunUiActionAsync(async () =>
        {
            ServerMessage response = await _client.SendRequestAsync(new ClientRequest
            {
                Type = type,
                GroupName = group
            });
            EnsureSuccess(response);
            _status.Text = response.Text;
            await RefreshGroupsAsync();
            _groups.Text = group;
        });
    }

    private async void RefreshGroups_Click(object? sender, EventArgs e) =>
        await RunUiActionAsync(RefreshGroupsAsync);

    private async Task RefreshGroupsAsync()
    {
        string current = _groups.Text;
        ServerMessage response = await _client.SendRequestAsync(new ClientRequest
        {
            Type = RequestTypes.ListGroups
        });
        EnsureSuccess(response);
        _groups.Items.Clear();
        _groups.Items.AddRange(response.Items.ToArray());
        _groups.Text = current;
    }

    private async void SendGroup_Click(object? sender, EventArgs e)
    {
        string group = _groups.Text.Trim();
        string text = _groupMessage.Text.Trim();
        if (group.Length == 0 || text.Length == 0) return;

        await RunUiActionAsync(async () =>
        {
            ServerMessage response = await _client.SendRequestAsync(new ClientRequest
            {
                Type = RequestTypes.GroupMessage,
                GroupName = group,
                Text = text
            });
            EnsureSuccess(response);
            _groupMessage.Clear();
            _status.Text = response.Text;
        });
    }

    private void Client_EventReceived(object? sender, ServerMessage message)
    {
        if (IsDisposed || Disposing) return;
        BeginInvoke(new Action(() =>
        {
            switch (message.EventType)
            {
                case EventTypes.PrivateMessage:
                    Append(_privateHistory, $"[{message.Sender} → Ви]: {message.Text}");
                    break;
                case EventTypes.GroupMessage:
                    Append(_groupHistory, $"[{message.GroupName}] [{message.Sender}]: {message.Text}");
                    break;
                case EventTypes.UserStatus:
                    _status.Text = message.Text;
                    break;
            }
        }));
    }

    private void Client_ConnectionLost(object? sender, string error)
    {
        if (IsDisposed || Disposing) return;
        BeginInvoke(new Action(() =>
        {
            _tabs.Enabled = false;
            _status.Text = "З'єднання втрачено: " + error;
            MessageBox.Show(this, _status.Text, "Мережа", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }));
    }

    private ClientRequest AuthRequest(string type) => new()
    {
        Type = type,
        Username = _username.Text.Trim(),
        Password = _password.Text
    };

    private async Task RunUiActionAsync(Func<Task> action)
    {
        UseWaitCursor = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Помилка", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static void EnsureSuccess(ServerMessage response)
    {
        if (!response.Success)
            throw new InvalidOperationException(response.Error ?? "Сервер відхилив запит.");
    }

    private static TextBox HistoryBox() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical
    };

    private static Label LabelFor(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left
    };

    private static Control CreateSendPanel(TextBox textBox, Button button)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(textBox, 0, 0);
        panel.Controls.Add(button, 1, 0);
        return panel;
    }

    private static void Append(TextBox box, string text)
    {
        box.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _client.EventReceived -= Client_EventReceived;
        _client.ConnectionLost -= Client_ConnectionLost;
        _client.Dispose();
    }
}
