﻿using System.Net;
using System.Text;
using EzLiveServer.Templating;
using HtmlAgilityPack;

namespace EzLiveServer;

public static class HttpResponse
{
    public static async Task FromFileAsync(HttpListenerResponse httpResponse, string filePath, CancellationToken cancellationToken = default)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

        httpResponse.ContentType = GetMIMEType(Path.GetExtension(filePath));
        await WriteBytesAsync(httpResponse, fileBytes, cancellationToken);
    }

    public static async Task FromCodeInjectedHtmlFileAsync(HttpListenerResponse httpResponse, string filePath, string? injectedFilePath, CancellationToken cancellationToken = default)
    {
        httpResponse.ContentType = GetMIMEType(".html");

        string htmlFileContent = await File.ReadAllTextAsync(filePath, cancellationToken);
        if (injectedFilePath is null)
        {
            await WriteBytesAsync(httpResponse, Encoding.UTF8.GetBytes(htmlFileContent), cancellationToken);
            return;
        }

        string injectedHtmlFileContent = await File.ReadAllTextAsync(injectedFilePath, cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(htmlFileContent);

        var bodyNode = document.DocumentNode.SelectSingleNode("//body");
        var injectedHtmlNode = HtmlNode.CreateNode(injectedHtmlFileContent);

        bodyNode.ParentNode.AppendChild(injectedHtmlNode);

        string newHtml = document.DocumentNode.OuterHtml;
        await WriteBytesAsync(httpResponse, Encoding.UTF8.GetBytes(newHtml), cancellationToken);
    }

    public static Task NotFoundAsync(HttpListenerResponse httpResponse, string url, string? notFoundHtmlFilePath, string? defaultNotFoundHtmlFilePath, CancellationToken cancellationToken = default)
    {
        httpResponse.ContentType = "text/html";
        httpResponse.StatusCode = 404;
        httpResponse.StatusDescription = "Not Found";

        return File.Exists(notFoundHtmlFilePath)
            ? FromFileAsync(httpResponse, notFoundHtmlFilePath, cancellationToken)
            : DefaultNotFoundAsync(httpResponse, defaultNotFoundHtmlFilePath, url, cancellationToken);
    }

    public static void NotModified(HttpListenerResponse httpResponse)
    {
        httpResponse.StatusCode = 304;
        httpResponse.StatusDescription = "Not Modified";

        httpResponse.Close();
    }

    public static void LastModified(HttpListenerResponse httpResponse, DateTime dateTime)
    {
        httpResponse.Headers.Add("Last-Modified", dateTime.ToUniversalTime().ToString("R"));
        httpResponse.Headers.Add("Cache-Control", "public, max-age=0");
    }

    private static async Task DefaultNotFoundAsync(HttpListenerResponse httpResponse, string? defaultNotFoundHtmlFile, string url, CancellationToken cancellationToken = default)
    {
        if (File.Exists(defaultNotFoundHtmlFile))
        {
            string notFoundTemplateStr = await File.ReadAllTextAsync(defaultNotFoundHtmlFile, cancellationToken);
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
            ".ico" => "image/x-icon",
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
