using GameProtocol;

namespace GameClient;

public sealed class MainForm : Form
{
    private readonly TextBox _hostTextBox = new() { Text = "127.0.0.1", Width = 140 };
    private readonly NumericUpDown _portInput = new()
    {
        Minimum = 1,
        Maximum = 65535,
        Value = 5001,
        Width = 90
    };
    private readonly Button _connectButton = new() { Text = "Підключитися", AutoSize = true };
    private readonly NumericUpDown _guessInput = new()
    {
        Minimum = 1,
        Maximum = 100,
        Value = 50,
        Width = 100,
        Enabled = false
    };
    private readonly Button _guessButton = new()
    {
        Text = "Відправити спробу",
        AutoSize = true,
        Enabled = false
    };
    private readonly TextBox _historyTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical
    };
    private readonly Label _statusLabel = new() { Text = "Не підключено.", AutoSize = true };
    private readonly UdpGameClient _gameClient = new();
    private bool _gameOver;

    public MainForm()
    {
        Text = "Хто перший вгадає — UDP-клієнт";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(650, 430);
        MinimumSize = new Size(560, 360);

        var connectionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        connectionPanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Сервер:", AutoSize = true, Anchor = AnchorStyles.Left },
            _hostTextBox,
            new Label { Text = "Порт:", AutoSize = true, Anchor = AnchorStyles.Left },
            _portInput,
            _connectButton
        });

        var guessPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        guessPanel.Controls.AddRange(new Control[]
        {
            new Label { Text = "Число від 1 до 100:", AutoSize = true, Anchor = AnchorStyles.Left },
            _guessInput,
            _guessButton
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(connectionPanel, 0, 0);
        layout.Controls.Add(guessPanel, 0, 1);
        layout.Controls.Add(_historyTextBox, 0, 2);
        layout.Controls.Add(_statusLabel, 0, 3);
        Controls.Add(layout);

        AcceptButton = _guessButton;
        _connectButton.Click += ConnectButton_Click;
        _guessButton.Click += GuessButton_Click;
        _gameClient.ResponseReceived += GameClient_ResponseReceived;
        _gameClient.ConnectionError += GameClient_ConnectionError;
        FormClosed += MainForm_FormClosed;
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        try
        {
            await _gameClient.ConnectAsync(_hostTextBox.Text.Trim(), (int)_portInput.Value);
            _gameOver = false;
            _hostTextBox.Enabled = false;
            _portInput.Enabled = false;
            _connectButton.Enabled = false;
            _guessInput.Enabled = true;
            _guessButton.Enabled = true;
            _statusLabel.Text = "Запит на приєднання відправлено.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Помилка підключення",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void GuessButton_Click(object? sender, EventArgs e)
    {
        _guessButton.Enabled = false;
        try
        {
            int guess = (int)_guessInput.Value;
            await _gameClient.SendGuessAsync(guess);
            _historyTextBox.AppendText($"Ваша спроба: {guess}{Environment.NewLine}");
            _statusLabel.Text = "Спробу відправлено, очікування відповіді...";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Помилка відправлення",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _guessButton.Enabled = _gameClient.IsConnected && !_gameOver;
        }
    }

    private void GameClient_ResponseReceived(object? sender, GameResponse response)
    {
        if (IsDisposed || Disposing) return;
        BeginInvoke(new Action(() =>
        {
            _historyTextBox.AppendText($"Сервер: {response.Message}{Environment.NewLine}");
            _statusLabel.Text = response.Message;

            if (response.GameOver)
            {
                _gameOver = true;
                _guessInput.Enabled = false;
                _guessButton.Enabled = false;
                _statusLabel.Text = "Гру завершено. " + response.Message;
            }
        }));
    }

    private void GameClient_ConnectionError(object? sender, string error)
    {
        if (IsDisposed || Disposing) return;
        BeginInvoke(new Action(() =>
        {
            _guessInput.Enabled = false;
            _guessButton.Enabled = false;
            _statusLabel.Text = "Помилка мережі: " + error;
        }));
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        if (!_gameClient.IsConnected) _connectButton.Enabled = !busy;
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _gameClient.ResponseReceived -= GameClient_ResponseReceived;
        _gameClient.ConnectionError -= GameClient_ConnectionError;
        _gameClient.Dispose();
    }
}
