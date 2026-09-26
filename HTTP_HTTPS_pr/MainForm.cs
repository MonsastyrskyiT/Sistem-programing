using System.Net;
using System.Net.Http.Headers;

namespace HttpViewer;

public sealed class MainForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly TextBox _uriTextBox = new()
    {
        Dock = DockStyle.Fill,
        Text = "http://localhost:8080/"
    };
    private readonly Button _sendButton = new()
    {
        Text = "Надіслати запит",
        AutoSize = true
    };
    private readonly TextBox _resultTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font("Consolas", 10)
    };
    private readonly Label _statusLabel = new()
    {
        Text = "Введіть URI та натисніть кнопку.",
        AutoSize = true
    };
    private CancellationTokenSource? _requestCancellation;

    public MainForm()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
        };
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("HttpViewerHomework", "1.0"));

        Text = "HTTP/HTTPS — перегляд сирої відповіді";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(650, 420);

        var requestPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            AutoSize = true
        };
        requestPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        requestPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        requestPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        requestPanel.Controls.Add(new Label
        {
            Text = "URI:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        requestPanel.Controls.Add(_uriTextBox, 1, 0);
        requestPanel.Controls.Add(_sendButton, 2, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(requestPanel, 0, 0);
        layout.Controls.Add(_resultTextBox, 0, 1);
        layout.Controls.Add(_statusLabel, 0, 2);
        Controls.Add(layout);

        AcceptButton = _sendButton;
        _sendButton.Click += SendButton_Click;
        FormClosed += MainForm_FormClosed;
    }

    private async void SendButton_Click(object? sender, EventArgs e)
    {
        if (!TryGetHttpUri(_uriTextBox.Text, out Uri? uri))
        {
            ShowError("Введіть коректний абсолютний HTTP або HTTPS URI.");
            return;
        }

        _requestCancellation?.Cancel();
        _requestCancellation?.Dispose();
        var requestCancellation = new CancellationTokenSource();
        _requestCancellation = requestCancellation;

        SetRequestInProgress(true);
        _resultTextBox.Clear();
        _statusLabel.Text = $"Виконується GET-запит до {uri}...";

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                requestCancellation.Token);

            string content = await response.Content.ReadAsStringAsync(requestCancellation.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Сервер повернув помилку {(int)response.StatusCode} " +
                    $"({response.ReasonPhrase}).");
            }

            _resultTextBox.Text = content;
            _statusLabel.Text =
                $"Отримано {(int)response.StatusCode} {response.ReasonPhrase}; " +
                $"символів: {content.Length}.";
        }
        catch (OperationCanceledException) when (requestCancellation.IsCancellationRequested)
        {
            _statusLabel.Text = "Запит скасовано.";
        }
        catch (TaskCanceledException)
        {
            ShowError("Перевищено час очікування відповіді сервера (30 секунд).");
        }
        catch (HttpRequestException ex)
        {
            ShowError("Помилка HTTP-запиту: " + ex.Message);
        }
        catch (Exception ex)
        {
            ShowError("Сталася помилка: " + ex.Message);
        }
        finally
        {
            SetRequestInProgress(false);
        }
    }

    private static bool TryGetHttpUri(string value, out Uri? uri)
    {
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out uri)) return false;

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
               uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private void SetRequestInProgress(bool inProgress)
    {
        _uriTextBox.Enabled = !inProgress;
        _sendButton.Enabled = !inProgress;
        UseWaitCursor = inProgress;
    }

    private void ShowError(string message)
    {
        _statusLabel.Text = "Запит завершився помилкою.";
        MessageBox.Show(
            this,
            message,
            "Помилка",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void MainForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _requestCancellation?.Cancel();
        _requestCancellation?.Dispose();
        _httpClient.Dispose();
    }
}
