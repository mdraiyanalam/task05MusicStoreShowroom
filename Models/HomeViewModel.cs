using MusicStore.Models;

namespace MusicStore.Models;

public class HomeViewModel
{
    public const int DefaultPageSize = 12;

    public string Language { get; set; } = "en-US";
    public long Seed { get; set; } = 42L;
    public double LikesPerSong { get; set; } = 3.7;
    public List<Song> Songs { get; set; } = new();
    public bool IsTableView { get; set; } = true;
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
    public List<string> AvailableLocales { get; set; } = new();
    public Dictionary<string, string> LocaleDisplayNames { get; set; } = new();
}
