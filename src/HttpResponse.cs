using System.Net;

namespace EzLiveServer;

public static class HttpResponse
{
    public static async Task FromFileAsync(HttpListenerResponse httpResponse, string file, CancellationToken cancellationToken = default)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(file, cancellationToken);

        httpResponse.ContentType = GetMIMEType(Path.GetExtension(file));
        await httpResponse.OutputStream.WriteAsync(fileBytes, cancellationToken);
        await httpResponse.OutputStream.FlushAsync(cancellationToken);

        httpResponse.Close();
    }

    public static Task NotFoundAsync(HttpListenerResponse httpResponse, string url, string notFoundHtmlFile, CancellationToken cancellationToken = default)
    {
        bool hasExtension = Path.GetExtension(url)?.Length > 0;
        httpResponse.ContentType = GetMIMEType(hasExtension ? Path.GetExtension(url) : ".html");
        httpResponse.StatusCode = 404;
        httpResponse.StatusDescription = "Not Found";

        return File.Exists(notFoundHtmlFile)
            ? FromFileAsync(httpResponse, notFoundHtmlFile, cancellationToken)
            : FromFileAsync(httpResponse, "./Default404NotFound.html", cancellationToken);
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
