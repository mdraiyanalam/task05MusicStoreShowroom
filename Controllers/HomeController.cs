using Microsoft.AspNetCore.Mvc;
using MusicStore.Models;
using MusicStore.Services;

namespace MusicStore.Controllers;

public class HomeController : Controller
{
    private readonly SongGeneratorService _generator;

    public HomeController(SongGeneratorService generator)
    {
        _generator = generator;
    }

    [HttpGet("/")]
    public IActionResult Index(string language = "en-US", long seed = 42, double likesPerSong = 3.7, int page = 1, bool isTableView = true)
    {
        var model = BuildModel(language, seed, likesPerSong, page, isTableView);
        return View(model);
    }

    [HttpGet]
    public IActionResult Gallery(string language = "en-US", long seed = 42, double likesPerSong = 3.7, int page = 1)
    {
        var model = BuildModel(language, seed, likesPerSong, page, false);
        return View("Index", model);
    }

    [HttpGet]
    public PartialViewResult SongsPartial(bool isTableView = true, int page = 1, string language = "en-US", long seed = 42, double LikesPerSong = 3.7)
    {
        var model = BuildModel(language, seed, LikesPerSong, page, isTableView);
        return PartialView("_SongListPartial", model);
    }

    private HomeViewModel BuildModel(string language, long seed, double likesPerSong, int page, bool isTableView)
    {
        var model = new HomeViewModel();
        model.Language = language;
        model.Seed = seed;
        model.LikesPerSong = likesPerSong;
        model.CurrentPage = page;
        model.IsTableView = isTableView;
        model.PageSize = HomeViewModel.DefaultPageSize;

        // Load locales
        model.AvailableLocales = new List<string>();
        model.LocaleDisplayNames = new Dictionary<string, string>();
        model.LocaleDisplayNames["en-US"] = "English (USA)";
        model.LocaleDisplayNames["de-DE"] = "German (Germany)";
        model.AvailableLocales.Add("en-US");
        model.AvailableLocales.Add("de-DE");

        var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (Directory.Exists(dataPath))
        {
            foreach (var f in Directory.GetFiles(dataPath, "locales-*.json"))
            {
                try
                {
                    var txt = System.IO.File.ReadAllText(f);
                    var doc = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(txt);
                    if (doc.TryGetProperty("locale", out var el))
                    {
                        var localeId = el.GetString() ?? Path.GetFileNameWithoutExtension(f);
                        if (!model.AvailableLocales.Contains(localeId)) model.AvailableLocales.Add(localeId);
                        if (!model.LocaleDisplayNames.ContainsKey(localeId)) model.LocaleDisplayNames[localeId] = localeId;
                    }
                }
                catch { }
            }
        }

        // Generate songs
        model.Songs = _generator.GenerateSongs(model.Language, model.Seed, model.LikesPerSong, model.PageSize, model.CurrentPage);

        return model;
    }
}
