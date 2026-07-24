using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

public class PlaywrightUITests
{
    [Fact]
    public async Task ToolbarAndTable_ShouldLoad()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://localhost:5000");

        Assert.True(await page.Locator("#language").IsVisibleAsync());
        Assert.True(await page.Locator("#seed").IsVisibleAsync());
        Assert.True(await page.Locator("#likes").IsVisibleAsync());

        // trigger data refresh and wait for table
        await page.EvaluateAsync("() => window.updateData && window.updateData(true)");
        await page.WaitForSelectorAsync("table");
        Assert.True(await page.Locator("table").IsVisibleAsync());
    }
}
