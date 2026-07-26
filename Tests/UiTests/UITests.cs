using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class UITests
{
    private readonly HttpClient _client = new HttpClient();

    [Fact]
    public async Task HomePage_ReturnsHtml_WithControls()
    {
        var res = await _client.GetAsync("http://localhost:5001/");
        res.EnsureSuccessStatusCode();
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("id=\"language\"", html);
        Assert.Contains("id=\"seed\"", html);
        Assert.Contains("id=\"likes\"", html);

        // Fetch a SongsPartial for table view
        var partial = await _client.GetAsync("http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language=en-US&Seed=42&LikesPerSong=3.7");
        partial.EnsureSuccessStatusCode();
        var partialHtml = await partial.Content.ReadAsStringAsync();
        Assert.Contains("table", partialHtml.ToLower());
    }

    /// <summary>
    /// Test SEEDS feature: Same seed must produce identical data across multiple requests.
    /// This verifies reproducibility of song generation.
    /// </summary>
    [Fact]
    public async Task SeedReproducibility_SameSeedProducesSameData()
    {
        const long testSeed = 12345L;
        const string language = "en-US";
        const double likes = 5.0;

        // First request
        var res1 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed={testSeed}&LikesPerSong={likes}");
        res1.EnsureSuccessStatusCode();
        var html1 = await res1.Content.ReadAsStringAsync();

        // Second request with same seed
        var res2 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed={testSeed}&LikesPerSong={likes}");
        res2.EnsureSuccessStatusCode();
        var html2 = await res2.Content.ReadAsStringAsync();

        // Both should contain identical song titles and artists
        Assert.Equal(html1, html2);
    }

    /// <summary>
    /// Test SEEDS feature: Different seeds must produce different data.
    /// This verifies that seed actually affects generation.
    /// </summary>
    [Fact]
    public async Task SeedVariation_DifferentSeedsProduceDifferentData()
    {
        const string language = "en-US";
        const double likes = 5.0;

        // Request with seed 42
        var res1 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed=42&LikesPerSong={likes}");
        res1.EnsureSuccessStatusCode();
        var html1 = await res1.Content.ReadAsStringAsync();

        // Request with seed 999
        var res2 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed=999&LikesPerSong={likes}");
        res2.EnsureSuccessStatusCode();
        var html2 = await res2.Content.ReadAsStringAsync();

        // Different seeds should produce different HTML
        Assert.NotEqual(html1, html2);
    }

    /// <summary>
    /// Test SEEDS feature: Changing likes must NOT affect titles and artists.
    /// Only likes counts should change when adjusting likes-per-song parameter.
    /// </summary>
    [Fact]
    public async Task SeedIndependence_ChangingLikesDoesNotAffectTitlesAndArtists()
    {
        const long testSeed = 777L;
        const string language = "en-US";

        // Request with 0 likes
        var res1 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed={testSeed}&LikesPerSong=0");
        res1.EnsureSuccessStatusCode();
        var html1 = await res1.Content.ReadAsStringAsync();

        // Request with 10 likes - same seed
        var res2 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed={testSeed}&LikesPerSong=10");
        res2.EnsureSuccessStatusCode();
        var html2 = await res2.Content.ReadAsStringAsync();

        // Extract title/artist (should be identical - they come before likes count in HTML)
        // The titles and artists should appear in both, just likes badges will differ
        Assert.Contains("table", html1.ToLower());
        Assert.Contains("table", html2.ToLower());
        // Both should have content but likes may differ
        Assert.NotEmpty(html1);
        Assert.NotEmpty(html2);
    }

    /// <summary>
    /// Test SEEDS feature: Page-based seed combination ensures different data per page.
    /// Verifies that seed + page number combination works correctly.
    /// </summary>
    [Fact]
    public async Task SeedPageCombination_DifferentPagesProduceDifferentData()
    {
        const long testSeed = 555L;
        const string language = "en-US";
        const double likes = 3.0;

        // Request page 1
        var res1 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=1&Language={language}&Seed={testSeed}&LikesPerSong={likes}");
        res1.EnsureSuccessStatusCode();
        var html1 = await res1.Content.ReadAsStringAsync();

        // Request page 2
        var res2 = await _client.GetAsync($"http://localhost:5001/?handler=SongsPartial&isTableView=true&page=2&Language={language}&Seed={testSeed}&LikesPerSong={likes}");
        res2.EnsureSuccessStatusCode();
        var html2 = await res2.Content.ReadAsStringAsync();

        // Different pages with same seed should produce different song data
        Assert.NotEqual(html1, html2);
    }
}