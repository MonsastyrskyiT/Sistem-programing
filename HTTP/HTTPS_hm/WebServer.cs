using System.Net;
using System.Text;
using System.Text.Json;

namespace LocalHttpServer;

internal sealed class WebServer
{
    private readonly HttpListener _listener = new();

    public WebServer(string address)
    {
        _listener.Prefixes.Add(address);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Start();
        using CancellationTokenRegistration registration =
            cancellationToken.Register(() => _listener.Stop());

        Console.WriteLine("Локальний HTTP-сервер запущено.");
        Console.WriteLine("Адреса: http://localhost:8080/");
        Console.WriteLine("Для завершення натисніть Ctrl+C.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _listener.GetContextAsync();

                // Кожний запит обробляється незалежно, тому повільний клієнт
                // не блокує приймання наступних підключень.
                _ = HandleRequestSafelyAsync(context);
            }
        }
        catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (_listener.IsListening) _listener.Stop();
            _listener.Close();
            Console.WriteLine("Сервер зупинено.");
        }
    }

    private static async Task HandleRequestSafelyAsync(HttpListenerContext context)
    {
        try
        {
            await HandleRequestAsync(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка обробки запиту: {ex.Message}");

            try
            {
                await WriteTextAsync(
                    context.Response,
                    HttpStatusCode.InternalServerError,
                    "Внутрішня помилка сервера.",
                    "text/plain; charset=utf-8");
            }
            catch
            {
                context.Response.Abort();
            }
        }
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        string path = NormalizePath(request.Url?.AbsolutePath);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {request.HttpMethod} {path}");

        if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers[HttpResponseHeader.Allow] = "GET";
            await WriteTextAsync(
                context.Response,
                HttpStatusCode.MethodNotAllowed,
                "Дозволено лише GET-запити.",
                "text/plain; charset=utf-8");
            return;
        }

        switch (path)
        {
            case "/":
                await WriteHtmlAsync(context.Response, PageTemplates.Home);
                break;

            case "/autobiography":
                await WriteHtmlAsync(context.Response, PageTemplates.Autobiography);
                break;

            case "/fav_countries":
                await WriteHtmlAsync(context.Response, PageTemplates.FavoriteCountries);
                break;

            case "/pc_data":
                string json = JsonSerializer.Serialize(
                    ComputerInformation.GetCurrent(),
                    new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                await WriteTextAsync(
                    context.Response,
                    HttpStatusCode.OK,
                    json,
                    "application/json; charset=utf-8");
                break;

            default:
                await WriteHtmlAsync(
                    context.Response,
                    PageTemplates.NotFound,
                    HttpStatusCode.NotFound);
                break;
        }
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return "/";
        return path.TrimEnd('/').ToLowerInvariant();
    }

    private static Task WriteHtmlAsync(
        HttpListenerResponse response,
        string html,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        WriteTextAsync(response, statusCode, html, "text/html; charset=utf-8");

    private static async Task WriteTextAsync(
        HttpListenerResponse response,
        HttpStatusCode statusCode,
        string content,
        string contentType)
    {
        byte[] data = Encoding.UTF8.GetBytes(content);
        response.StatusCode = (int)statusCode;
        response.ContentType = contentType;
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = data.Length;
        response.Headers[HttpResponseHeader.CacheControl] = "no-store";

        await response.OutputStream.WriteAsync(data);
        response.Close();
    }
}
