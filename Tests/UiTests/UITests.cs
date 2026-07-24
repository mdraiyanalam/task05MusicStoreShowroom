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
}