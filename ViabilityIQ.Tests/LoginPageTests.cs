
using Microsoft.Playwright;
using Xunit;

namespace ViabilityIQ.Tests;

public class LoginPageTests
{
    [Fact]
    public async Task LoginPage_ShouldRenderFormAndAntiforgeryToken()
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = true
            });

        var page = await browser.NewPageAsync(
            new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });

        try
        {
            // Open the local ViabilityIQ login page.
            var response = await page.GotoAsync(
                "https://localhost:7236/login");

            Assert.NotNull(response);
            Assert.True(response!.Ok,
                $"Login page returned HTTP {response.Status}.");

            // Verify that the login form is rendered.
            var email = page.Locator("input[name='Email']");
            var password = page.Locator("input[name='Password']");
            var submit = page.Locator("button[type='submit']");

            await email.WaitForAsync();

            Assert.True(await email.IsVisibleAsync());
            Assert.True(await password.IsVisibleAsync());
            Assert.True(await submit.IsVisibleAsync());

            // Verify that the form contains an antiforgery token.
            var token = page.Locator(
                "form input[name='__RequestVerificationToken']");

            Assert.Equal(1, await token.CountAsync());

            var tokenValue = await token.GetAttributeAsync("value");

            Assert.False(string.IsNullOrWhiteSpace(tokenValue));
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
