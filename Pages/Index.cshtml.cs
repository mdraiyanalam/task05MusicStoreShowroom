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
    public int Seed { get; set; } = 42;

    [BindProperty(SupportsGet = true)]
    public int LikesPerSong { get; set; } = 1000;

    public List<Song> Songs { get; set; } = new();
    public bool IsTableView { get; set; } = true;
    public int CurrentPage { get; set; } = 1;
    public const int PageSize = 12;

    public void OnGet(int page = 1)
    {
        CurrentPage = page;
        IsTableView = true;
        LoadData();
    }

    public void OnGetGallery(int page = 1)
    {
        IsTableView = false;
        CurrentPage = page;
        LoadData();
    }

    private void LoadData()
    {
        Songs = _generator.GenerateSongs(Language, Seed, LikesPerSong, PageSize, CurrentPage);
    }

    public PartialViewResult OnGetSongsPartial(bool isTableView, int page = 1)
    {
        IsTableView = isTableView;
        CurrentPage = page;
        LoadData();
        return Partial("_SongListPartial", this);
    }
}