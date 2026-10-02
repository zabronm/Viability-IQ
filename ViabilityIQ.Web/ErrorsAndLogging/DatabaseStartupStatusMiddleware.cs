using System.Text.Encodings.Web;
using ViabilityIQ.Web.Services;

namespace ViabilityIQ.Web.ErrorsAndLogging;

public sealed class DatabaseStartupStatusMiddleware
{
    private readonly RequestDelegate next;

    public DatabaseStartupStatusMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        DatabaseStartupState startupState)
    {
        if (startupState.Status == DatabaseStartupStatus.Ready)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "10";
        context.Response.Headers.CacheControl = "no-store, no-cache";

        if (context.Request.Path.StartsWithSegments("/health"))
        {
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Database unavailable");
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(BuildStatusPage(startupState));
    }

    private static string BuildStatusPage(DatabaseStartupState state)
    {
        var failed = state.Status == DatabaseStartupStatus.Failed;
        var title = failed
            ? "Database connection unavailable"
            : "Viability.IQ is starting";
        var heading = failed
            ? "Viability.IQ cannot connect to its database"
            : "Preparing Viability.IQ";
        var message = failed
            ? "The application is online, but the database could not be reached. Please confirm that SQL Server is running and accessible, then restart the application service."
            : $"Database initialization is in progress (attempt {state.Attempt} of {state.MaxAttempts}). This page will retry automatically.";
        var referenceMarkup = failed && !string.IsNullOrWhiteSpace(state.ErrorReference)
            ? $"<p class=\"reference\">Error reference: <strong>{HtmlEncoder.Default.Encode(state.ErrorReference)}</strong></p>"
            : string.Empty;
        var refreshMarkup = failed
            ? "<button type=\"button\" onclick=\"location.reload()\">Refresh status</button>"
            : """
              <button type="button" onclick="location.reload()">Check again</button>
              <script>window.setTimeout(() => location.reload(), 10000);</script>
              """;

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex,nofollow">
                <title>{{title}} - Viability.IQ</title>
                <style>
                    :root { color-scheme: light; }
                    * { box-sizing: border-box; }
                    body {
                        margin: 0;
                        min-height: 100vh;
                        display: grid;
                        place-items: center;
                        padding: 24px;
                        color: #334155;
                        background: #f4f7f9;
                        font-family: Inter, Arial, sans-serif;
                    }
                    main {
                        width: min(560px, 100%);
                        padding: 32px;
                        border: 1px solid #dbe4ea;
                        border-radius: 12px;
                        background: #fff;
                        box-shadow: 0 18px 45px rgba(15, 42, 67, .12);
                    }
                    .brand { margin-bottom: 22px; color: #173d59; font-size: 28px; font-weight: 700; }
                    .brand span { color: #2ba88f; font-style: italic; }
                    .status {
                        display: inline-block;
                        margin-bottom: 12px;
                        padding: 4px 10px;
                        border-radius: 999px;
                        color: #92400e;
                        background: #fef3c7;
                        font-size: 12px;
                        font-weight: 700;
                        text-transform: uppercase;
                    }
                    h1 { margin: 0 0 12px; color: #173d59; font-size: 24px; }
                    p { margin: 0 0 14px; line-height: 1.55; }
                    .reference {
                        padding: 10px;
                        border-radius: 6px;
                        background: #f1f5f9;
                        font-size: 13px;
                    }
                    button {
                        padding: 9px 16px;
                        border: 0;
                        border-radius: 6px;
                        color: #fff;
                        background: #176b63;
                        cursor: pointer;
                        font-weight: 600;
                    }
                    small { display: block; margin-top: 18px; color: #64748b; }
                </style>
            </head>
            <body>
                <main role="alert">
                    <div class="brand">Viability<span>.IQ</span></div>
                    <div class="status">Service temporarily unavailable</div>
                    <h1>{{heading}}</h1>
                    <p>{{message}}</p>
                    {{referenceMarkup}}
                    {{refreshMarkup}}
                    <small>No financial or account information is displayed on this page.</small>
                </main>
            </body>
            </html>
            """;
    }
}
