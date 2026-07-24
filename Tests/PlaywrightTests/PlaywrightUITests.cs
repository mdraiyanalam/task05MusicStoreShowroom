using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Playwright;
using Xunit;

public class PlaywrightUITests : IAsyncLifetime
{
    private IPlaywright _pw = null!;
    private IBrowser _browser = null!;

    public async Task InitializeAsync()
    {
        _pw = await Playwright.CreateAsync();
        _browser = await _pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _pw.Dispose();
    }

    private async Task<IPage> OpenPage() {
        var page = await _browser.NewPageAsync(new BrowserNewPageOptions { });
        await page.GotoAsync("http://localhost:5000", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        return page;
    }

    [Fact]
    public async Task PaginationResets_OnLanguageOrSeedChange()
    {
        var page = await OpenPage();
        // ensure table loaded
        await page.EvaluateAsync("() => window.updateData && window.updateData(true)");
        await page.WaitForSelectorAsync(".pagination");

        // navigate to next page
        var next = await page.QuerySelectorAsync(".page-link[href*='page=2']");
        if (next != null) await next.ClickAsync();
        // wait for content
        await page.WaitForTimeoutAsync(500);

        // change language select -> should reset to page 1
        var sel = await page.QuerySelectorAsync("#language");
        if (sel != null) {
            // pick a different value if available
            var options = await page.EvaluateAsync<string[]>("() => Array.from(document.querySelectorAll('#language option')).map(o => o.value)");
            if (options.Length > 1) {
                var newVal = options[options.Length - 1];
                await page.SelectOptionAsync("#language", newVal);
            } else {
                // toggle seed instead
                await page.FillAsync("#seed", "123456789");
            }
        }

        // wait for update and assert pagination shows page 1
        await page.WaitForTimeoutAsync(700);
        var active = await page.InnerTextAsync(".pagination .page-item.active .page-link");
        Assert.Contains("1", active);

        await page.CloseAsync();
    }

    [Fact]
    public async Task LikesFractional_Behavior_AveragesApproximately()
    {
        var page = await OpenPage();
        // sample across many pages using the SongsPartial endpoint via fetch in page context
        const int pagesToFetch = 10;
        const int perPage = 12;
        const double likesTarget = 3.7;
        var js = @"(async () => {
                const results = [];" +
                "for (let p=1; p<= " + pagesToFetch + "; p++) {" +
                "  const url = '?handler=SongsPartial&isTableView=true&page=' + p + '&Language=en-US&Seed=424242&LikesPerSong=" + "" + likesTarget.ToString(CultureInfo.InvariantCulture) + "" + "';" +
                "  const r = await fetch(url);" +
                "  const html = await r.text();" +
                "  const doc = new DOMParser().parseFromString(html, 'text/html');" +
                "  const rows = Array.from(doc.querySelectorAll('tbody tr.song-row'));" +
                "  for (const rr of rows) {" +
                "    const likes = parseInt((rr.querySelector('.likes-count') || {}).textContent || '0', 10);" +
                "    results.push(likes);" +
                "  }" +
                "}" +
                "return results;" +
                "})();";

        var likesArray = await page.EvaluateAsync<int[]>(js);
        Assert.NotNull(likesArray);
        // compute average
        double avg = likesArray.Average();
        // allow a small tolerance around target (since random)
        Assert.InRange(avg, likesTarget - 0.6, likesTarget + 0.6);
        await page.CloseAsync();
    }

    [Fact]
    public async Task GalleryInfiniteScroll_AppendsPages()
    {
        var page = await OpenPage();
        // switch to gallery by clicking button
        var galleryBtn = await page.QuerySelectorAsync("button:has-text('Gallery')");
        if (galleryBtn != null) await galleryBtn.ClickAsync();
        await page.WaitForTimeoutAsync(500);

        // initial card count
        var count1 = await page.EvaluateAsync<int>("() => document.querySelectorAll('#content-area .card').length");
        // scroll to bottom several times to trigger loadMore
        for (int i=0;i<4;i++) {
            await page.EvaluateAsync("() => window.scrollTo(0, document.body.scrollHeight)");
            await page.WaitForTimeoutAsync(800);
        }
        var count2 = await page.EvaluateAsync<int>("() => document.querySelectorAll('#content-area .card').length");
        Assert.True(count2 > count1, $"Expected more cards after scrolling: {count1} -> {count2}");
        await page.CloseAsync();
    }

    [Fact]
    public async Task TableExpandLyricsSync_Works()
    {
        var page = await OpenPage();
        await page.EvaluateAsync("() => window.updateData && window.updateData(true)");
        await page.WaitForSelectorAsync("tbody tr.song-row");
        // click first row
        await page.ClickAsync("tbody tr.song-row");
        // wait for detail
        await page.WaitForSelectorAsync("tr.song-detail:not(.d-none) .lyrics-container");
        // ensure lyrics load
        await page.WaitForFunctionAsync("() => { const c = document.querySelector('.lyrics-container'); return c && !/Loading lyrics/i.test(c.innerText); }");
        // set player time and dispatch timeupdate to simulate playback
        await page.EvaluateAsync("() => { const player = document.querySelector('audio.song-player'); if (player) { player.currentTime = 1.5; player.dispatchEvent(new Event('timeupdate')); } }");
        // wait for active lyric
        var active = await page.WaitForSelectorAsync(".lyric-line.active-lyric", new PageWaitForSelectorOptions { Timeout = 2000 });
        Assert.NotNull(active);
        await page.CloseAsync();
    }
}
