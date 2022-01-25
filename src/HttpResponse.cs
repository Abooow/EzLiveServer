using System.Net;
using System.Text;

namespace EzLiveServer;

public static class HttpResponse
{
    public static async Task FromFileAsync(HttpListenerResponse httpResponse, string file, CancellationToken cancellationToken = default)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(file, cancellationToken);

        httpResponse.ContentType = GetMIMEType(Path.GetExtension(file));
        await WriteBytesAsync(httpResponse, fileBytes, cancellationToken);
    }

    public static Task NotFoundAsync(HttpListenerResponse httpResponse, string url, string notFoundHtmlFile, CancellationToken cancellationToken = default)
    {
        bool hasExtension = Path.GetExtension(url)?.Length > 0;
        httpResponse.ContentType = GetMIMEType(hasExtension ? Path.GetExtension(url) : ".html");
        httpResponse.StatusCode = 404;
        httpResponse.StatusDescription = "Not Found";

        return File.Exists(notFoundHtmlFile)
            ? FromFileAsync(httpResponse, notFoundHtmlFile, cancellationToken)
            : DefaultNotFoundAsync(httpResponse, url, cancellationToken);
    }

    private static async Task DefaultNotFoundAsync(HttpListenerResponse httpResponse, string url, CancellationToken cancellationToken = default)
    {
        if (File.Exists("./Default404NotFound.html"))
        {
            string notFoundTemplateStr = await File.ReadAllTextAsync("./Default404NotFound.html", cancellationToken);
            var templateModel = new Dictionary<string, object>() { { "Url", url } };
            string template = TemplateEngine.Run(notFoundTemplateStr, templateModel);

            await WriteBytesAsync(httpResponse, Encoding.UTF8.GetBytes(template), cancellationToken);
            return;
        }

        string templatse = $@"
            <h1>404 Not Found</h1>
            <p>
                <strong>{url}</strong> was not found.
                <a href=""#"" onclick=""location.reload(true)"">Refresh</a>
                <a href=""/"">Home</a>
            </p>";

        await WriteBytesAsync(httpResponse, Encoding.UTF8.GetBytes(templatse), cancellationToken);
    }

    private static async Task WriteBytesAsync(HttpListenerResponse httpResponse, byte[] bytes, CancellationToken cancellationToken = default)
    {
        await httpResponse.OutputStream.WriteAsync(bytes, cancellationToken);
        await httpResponse.OutputStream.FlushAsync(cancellationToken);

        httpResponse.Close();
    }

    public static string GetMIMEType(string fileExtension)
    {
        return fileExtension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".txt" => "text/plain",
            ".ico" => "text/x-icon",
            ".png" => "image/png",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/html",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            ".json" => "application/json",
            ".zip" => "application/zip",
            ".wasm" => "application/wasm",
            ".woff" => "application/font-woff",
            ".woff2" => "application/font-woff",
            _ => "application/octet-stream"
        };
    }
}
