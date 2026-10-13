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

        var referenceMarkup = failed && !string.IsNullOrWhiteSpace(state.ErrorReference)
            ? $"<p class=\"reference\">Error reference: <strong>{HtmlEncoder.Default.Encode(state.ErrorReference)}</strong></p>"
            : string.Empty;

        var statusContent = failed
            ? $$"""
                <div class="status">Service temporarily unavailable</div>
                <h1>Viability.IQ cannot connect to its database</h1>
                <p>
                    The application is online, but the database could not be reached.
                    Please confirm that SQL Server is running and accessible, then
                    restart the application service.
                </p>
                {{referenceMarkup}}
                <button type="button" onclick="location.reload()">Refresh status</button>
                <small>No financial or account information is displayed on this page.</small>
                """
            : """
                <div class="spinner" aria-hidden="true"></div>               
                <p class="subtitle">
                    Please wait while the system initializes...
                </p>                
                """;

        // This polls the health URL without reloading the visible page.
        // Once startup is ready, the middleware lets the request continue
        // to the application's configured health endpoint.
        var startupScript = failed
            ? string.Empty
            : """
                <script>
                    (() => {
                        const checkIntervalMs = 2000;

                        async function checkDatabaseStatus() {
                            try {
                                const response = await fetch('/health', {
                                    method: 'GET',
                                    cache: 'no-store',
                                    headers: { 'Accept': 'text/plain' }
                                });

                                if (response.ok) {
                                    window.location.reload();
                                    return;
                                }
                            } catch {
                                // Keep the stable preparation screen visible.
                            }

                            window.setTimeout(checkDatabaseStatus, checkIntervalMs);
                        }

                        window.setTimeout(checkDatabaseStatus, checkIntervalMs);
                    })();
                </script>
                """;

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <meta name="robots" content="noindex,nofollow">
                <meta name="theme-color" content="#f4f7f9">
                <title>{{title}} - Viability.IQ</title>
                <style>
                    :root {
                        color-scheme: light;
                        font-family: Inter, "Segoe UI", Arial, sans-serif;
                    }

                    * {
                        box-sizing: border-box;
                    }

                    html,
                    body {
                        width: 100%;
                        min-height: 100%;
                        margin: 0;
                    }

                    body {
                        min-height: 100vh;
                        min-height: 100dvh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        padding: 24px;
                        color: #334155;
                        background: #f4f7f9;
                    }

                    main {
                        width: min(520px, 100%);
                        min-height: 340px;
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        justify-content: center;
                        padding: 40px 32px;
                        border: 1px solid #dbe4ea;
                        border-radius: 16px;
                        background: #fff;
                        box-shadow: 0 18px 45px rgba(15, 42, 67, .12);
                        text-align: center;
                    }

                    .brand {
                        margin: 0 0 30px;
                        color: #173d59;
                        font-size: clamp(26px, 5vw, 32px);
                        font-weight: 750;
                        letter-spacing: -.6px;
                    }

                    .brand span {
                        color: #2ba88f;
                        font-style: italic;
                    }

                    .spinner {
                        width: 44px;
                        height: 44px;
                        flex: 0 0 44px;
                        margin: 0 0 22px;
                        border: 4px solid #dceeea;
                        border-top-color: #2ba88f;
                        border-right-color: #2ba88f;
                        border-radius: 50%;
                        animation: viability-spin .8s linear infinite;
                    }

                    .preparing {
                        margin: 0;
                        color: #173d59;
                        font-size: 19px;
                        font-weight: 650;
                        letter-spacing: .1px;
                    }

                    .subtitle {
                        max-width: 340px;
                        margin: 12px 0 0;
                        color: #64748b;
                        font-size: 14px;
                        line-height: 1.55;
                    }

                    .attempt {
                        margin: 18px 0 0;
                        color: #94a3b8;
                        font-size: 12px;
                    }

                    .status {
                        display: inline-block;
                        margin: 0 0 14px;
                        padding: 5px 11px;
                        border-radius: 999px;
                        color: #92400e;
                        background: #fef3c7;
                        font-size: 12px;
                        font-weight: 700;
                        text-transform: uppercase;
                    }

                    h1 {
                        margin: 0 0 12px;
                        color: #173d59;
                        font-size: 24px;
                        line-height: 1.25;
                    }

                    main > p {
                        margin: 0 0 14px;
                        line-height: 1.55;
                    }

                    .reference {
                        padding: 10px;
                        border-radius: 6px;
                        background: #f1f5f9;
                        font-size: 13px;
                        overflow-wrap: anywhere;
                    }

                    button {
                        margin-top: 6px;
                        padding: 10px 16px;
                        border: 0;
                        border-radius: 6px;
                        color: #fff;
                        background: #176b63;
                        cursor: pointer;
                        font-weight: 600;
                    }

                    button:hover {
                        background: #12564f;
                    }

                    button:focus-visible {
                        outline: 3px solid #83d4c4;
                        outline-offset: 3px;
                    }

                    @keyframes viability-spin {
                        to { transform: rotate(360deg); }
                    }

                    @media (max-width: 480px) {
                        body { padding: 16px; }
                        main {
                            min-height: 300px;
                            padding: 32px 22px;
                        }
                    }

                    @media (prefers-reduced-motion: reduce) {
                        .spinner {
                            animation-duration: 2s;
                        }
                    }
                </style>
            </head>
            <body>
                <main role="status" aria-live="polite">
                    <div class="brand">Viability<span class="brand-iq fst-italic">.IQ</span></div>
                    {{statusContent}}
                </main>
                {{startupScript}}
            </body>
            </html>
            """;
    }
}
