using MusicStore.Models;

namespace MusicStore.Services;

public class SongGeneratorService
{
    public List<Song> GenerateSongs(string language, int seed, int likesPerSong, int count, int page)
    {
        var rng = new Random(seed + page * 17); // deterministic but different per page

        var genres = new[] { "Pop", "Rock", "Hip-Hop", "Folk", "Electronic", "Indie", "Classical" };
        var adjectives = new[] { "Dream", "Midnight", "Electric", "Golden", "Silent", "Wild", "Summer", "Broken", "Eternal" };
        var nouns = new[] { "Love", "Heart", "Fire", "Sky", "River", "Echo", "Shadow", "Star", "Moon" };

        var songs = new List<Song>();

        for (int i = 0; i < count; i++)
        {
            var globalId = (page - 1) * count + i + 1;
            var adj = adjectives[rng.Next(adjectives.Length)];
            var noun = nouns[rng.Next(nouns.Length)];

            songs.Add(new Song
            {
                Id = globalId,
                Title = $"{adj} {noun}",
                Artist = $"Artist {rng.Next(10, 999)}",
                Language = language,
                Genre = genres[rng.Next(genres.Length)],
                Likes = likesPerSong + rng.Next(-300, 500),
                AudioPreviewUrl = $"https://www.soundhelix.com/examples/mp3/SoundHelix-Song-{rng.Next(1, 18)}.mp3",
                CoverImageUrl = $"https://picsum.photos/id/{rng.Next(100, 400)}/400/300"
            });
        }

        return songs;
    }
}