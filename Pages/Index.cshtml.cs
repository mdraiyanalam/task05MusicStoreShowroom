using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Pages;

public class IndexModel : PageModel
{
    private readonly SongGeneratorService _generator;

    public IndexModel(SongGeneratorService generator)
    {
        _generator = generator;
    }

    [BindProperty(SupportsGet = true)]
    public string Language { get; set; } = "English";

    [BindProperty(SupportsGet = true)]
    public long Seed { get; set; } = 42L;

    [BindProperty(SupportsGet = true)]
    public double LikesPerSong { get; set; } = 3.7;

    public List<Song> Songs { get; set; } = new();
    public bool IsTableView { get; set; } = true;
    public int CurrentPage { get; set; } = 1;
    public const int PageSize = 12;
    public List<string> AvailableLocales { get; set; } = new();
    public Dictionary<string, string> LocaleDisplayNames { get; set; } = new();

    public void OnGet(int page = 1)
    {
        CurrentPage = page;
        IsTableView = true;
        LoadLocales();
        LoadData();
    }

    public void OnGetGallery(int page = 1)
    {
        IsTableView = false;
        CurrentPage = page;
        LoadLocales();
        LoadData();
    }

    private void LoadData()
    {
        Songs = _generator.GenerateSongs(Language, Seed, LikesPerSong, PageSize, CurrentPage);
    }

    private void LoadLocales()
    {
        // Hardcoded defaults and probe Data folder for locale files
        LocaleDisplayNames["en-US"] = "English (USA)";
        LocaleDisplayNames["de-DE"] = "German (Germany)";
        AvailableLocales.Add("en-US");
        AvailableLocales.Add("de-DE");

        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(dataPath)) return;
        foreach (var f in Directory.GetFiles(dataPath, "locales-*.json"))
        {
            try
            {
                var txt = System.IO.File.ReadAllText(f);
                var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(txt);
                if (doc.TryGetProperty("locale", out var el))
                {
                    var localeId = el.GetString() ?? Path.GetFileNameWithoutExtension(f);
                    if (!AvailableLocales.Contains(localeId))
                    {
                        AvailableLocales.Add(localeId);
                    }
                    if (!LocaleDisplayNames.ContainsKey(localeId))
                    {
                        LocaleDisplayNames[localeId] = localeId;
                    }
                }
            }
            catch { }
        }
    }

    public PartialViewResult OnGetSongsPartial(bool isTableView, int page = 1)
    {
        IsTableView = isTableView;
        CurrentPage = page;
        LoadData();
        return Partial("_SongListPartial", this);
    }
}