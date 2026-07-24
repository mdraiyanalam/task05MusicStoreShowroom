using Microsoft.AspNetCore.Mvc;
using MusicStore.Services;
using MusicStore.Models;

namespace MusicStore.Controllers;

public class HomeController : Controller
{
    private readonly SongGeneratorService _svc;
    public HomeController(SongGeneratorService svc)
    {
        _svc = svc;
    }

    // Minimal MVC entry that redirects to existing Razor Page index for now
    public IActionResult Index()
    {
        return RedirectToPage("/Index");
    }

    // Optional: provide an API-style action to render the songs partial HTML so MVC views can reuse
    [HttpGet]
    public IActionResult SongsPartial(bool isTableView = true, int page = 1, string Language = "en-US", long Seed = 42, double LikesPerSong = 3.7)
    {
        var model = new IndexViewModel();
        model.Language = Language;
        model.Seed = Seed;
        model.LikesPerSong = LikesPerSong;
        model.CurrentPage = page;
        model.IsTableView = isTableView;
        model.PageSize = IndexViewModel.DefaultPageSize;
        model.Songs = _svc.GenerateSongs(Language, Seed, LikesPerSong, model.PageSize, page);
        return PartialView("_SongListPartial", model);
    }
}
