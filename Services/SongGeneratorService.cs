using MusicStore.Models;
using System.Text.Json;
using System.IO;
using System.Text;
using NAudio.Wave;

namespace MusicStore.Services;

public class SongGeneratorService
{
    private readonly string _dataPath;
    private readonly Dictionary<string, JsonElement> _locales = new();

    public SongGeneratorService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data");
        LoadLocales();
    }

    private void LoadLocales()
    {
        if (!Directory.Exists(_dataPath)) return;
        foreach (var f in Directory.GetFiles(_dataPath, "locales-*.json"))
        {
            try
            {
                var t = System.IO.File.ReadAllText(f);
                var doc = JsonSerializer.Deserialize<JsonElement>(t);
                if (doc.TryGetProperty("locale", out var localeEl))
                {
                    var key = localeEl.GetString() ?? Path.GetFileNameWithoutExtension(f);
                    _locales[key] = doc;
                }
            }
            catch { }
        }
    }

    private JsonElement? GetLocale(string language)
    {
        if (string.IsNullOrEmpty(language)) return _locales.Values.FirstOrDefault();
        var match = _locales.Keys.FirstOrDefault(k => k.Contains(language, StringComparison.OrdinalIgnoreCase) || k.StartsWith(language, StringComparison.OrdinalIgnoreCase));
        if (match != null) return _locales[match];
        return _locales.Values.FirstOrDefault();
    }

    public List<Song> GenerateSongs(string language, long seed, double likesPerSong, int count, int page)
    {
        int contentSeed = (int)((seed ^ (seed >> 32) ^ (long)page * 397) & 0x7FFFFFFF);
        var contentRng = new Random(contentSeed);

        var locale = GetLocale(language);

        string[] genres = locale.HasValue && locale.Value.TryGetProperty("genres", out var g) ? g.EnumerateArray().Select(x => x.GetString() ?? "Pop").ToArray() : new[] { "Pop", "Rock", "Folk" };
        string[] firsts = locale.HasValue && locale.Value.TryGetProperty("firstNames", out var fn) ? fn.EnumerateArray().Select(x => x.GetString() ?? "A").ToArray() : new[] { "A" };
        string[] lasts = locale.HasValue && locale.Value.TryGetProperty("lastNames", out var ln) ? ln.EnumerateArray().Select(x => x.GetString() ?? "Z").ToArray() : new[] { "Z" };
        string[] albums = locale.HasValue && locale.Value.TryGetProperty("albumWords", out var aw) ? aw.EnumerateArray().Select(x => x.GetString() ?? "Single").ToArray() : new[] { "Single" };

        var songs = new List<Song>();

        for (int i = 0; i < count; i++)
        {
            var globalId = (page - 1) * count + i + 1;
            var title = $"{firsts[contentRng.Next(firsts.Length)]} {lasts[contentRng.Next(lasts.Length)]}";
            var artist = (contentRng.NextDouble() < 0.4)
                ? $"{firsts[contentRng.Next(firsts.Length)]} {lasts[contentRng.Next(lasts.Length)]}"
                : $"{firsts[contentRng.Next(firsts.Length)]} & {lasts[contentRng.Next(lasts.Length)]}";

            var genre = genres[contentRng.Next(genres.Length)];
            var album = albums[contentRng.Next(albums.Length)];

            int likes;
            if (likesPerSong <= 0)
            {
                likes = 0;
            }
            else if (likesPerSong >= 10)
            {
                likes = 10;
            }
            else
            {
                int likesSeed = (int)((seed ^ ((long)globalId * 16777619L) ^ 0x9E3779B9L) & 0x7FFFFFFF);
                var likesRng = new Random(likesSeed);
                int baseLikes = (int)Math.Floor(likesPerSong);
                double frac = likesPerSong - baseLikes;
                likes = baseLikes + (likesRng.NextDouble() < frac ? 1 : 0);
            }

                // generate a short review sentence
                var reviewAdj = new[] { "captivating", "raw", "melodic", "experimental", "nostalgic", "energetic", "soothing", "haunting" };
                var reviewNouns = new[] { "performance", "sound", "production", "arrangement", "vocals", "melody" };
                var review = $"A {reviewAdj[contentRng.Next(reviewAdj.Length)]} {reviewNouns[contentRng.Next(reviewNouns.Length)]} that feels {reviewAdj[contentRng.Next(reviewAdj.Length)]}.";

                songs.Add(new Song
                {
                    Id = globalId,
                    Title = title,
                    Artist = artist,
                    Album = album,
                    Language = language,
                    Genre = genre,
                    Likes = likes,
                    AudioPreviewUrl = $"/audio/{seed}/{globalId}",
                    CoverImageUrl = $"/cover/{(string.IsNullOrEmpty(language)?"en-US":language)}/{seed}/{globalId}",
                LyricsUrl = $"/lyrics/{(string.IsNullOrEmpty(language)?"en-US":language)}/{seed}/{globalId}",
                Review = review
                });
        }

        return songs;
    }

    public byte[] GenerateCoverSvg(long seed, int id, string title, string artist)
    {
        // produce a simple SVG with gradient, rectangles and text; deterministic based on seed+id
        int s = (int)((seed ^ id) & 0x7FFFFFFF);
        var rng = new Random(s);
        int w = 400, h = 300;
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{w}' height='{h}' viewBox='0 0 {w} {h}'>");
        // gradient
        var c1 = (rng.Next(40,200), rng.Next(40,200), rng.Next(40,200));
        var c2 = (rng.Next(40,200), rng.Next(40,200), rng.Next(40,200));
        sb.Append($"<defs><linearGradient id='g' x1='0' y1='0' x2='0' y2='1'><stop offset='0' stop-color='rgb({c1.Item1},{c1.Item2},{c1.Item3})'/><stop offset='1' stop-color='rgb({c2.Item1},{c2.Item2},{c2.Item3})'/></linearGradient></defs>");
        sb.Append($"<rect width='100%' height='100%' fill='url(#g)' />");
        // random translucent rectangles
        for (int i = 0; i < 5; i++)
        {
            var rx = rng.Next(-50, w);
            var ry = rng.Next(-50, h);
            var rw = rng.Next(40, 220);
            var rh = rng.Next(30, 160);
            var cr = rng.Next(0,255); var cg = rng.Next(0,255); var cb = rng.Next(0,255); var a = 0.15 + rng.NextDouble()*0.4;
            sb.Append($"<rect x='{rx}' y='{ry}' width='{rw}' height='{rh}' fill='rgba({cr},{cg},{cb},{a:F2})' />");
        }
        // title & artist text
        var safeTitle = System.Security.SecurityElement.Escape(title ?? "");
        var safeArtist = System.Security.SecurityElement.Escape(artist ?? "");
        sb.Append($"<text x='20' y='50' font-family='Arial, sans-serif' font-size='20' fill='white'>{safeTitle}</text>");
        sb.Append($"<text x='20' y='{h - 30}' font-family='Arial, sans-serif' font-size='14' fill='rgba(255,255,255,0.85)'>{safeArtist}</text>");
        sb.Append("</svg>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // Synthesize WAV audio and return bytes (browser will play audio/wav)
    public byte[] GenerateAudioWav(long seed, int id, int seconds = 12)
    {
        int s = (int)((seed ^ id) & 0x7FFFFFFF);
        var rng = new Random(s);
        int sampleRate = 44100;
        int channels = 1;
        int totalSamples = sampleRate * seconds;

        using var msWav = new MemoryStream();
        using (var waveWriter = new WaveFileWriter(msWav, new WaveFormat(sampleRate, channels)))
        {
            double phase = 0;
            for (int n = 0; n < totalSamples; n++)
            {
                double t = (double)n / sampleRate;
                double baseFreq = 220 + (rng.NextDouble() * 440);
                double freq = baseFreq * (1 + 0.5 * Math.Sin(2 * Math.PI * 0.1 * t + id));
                double sample = 0.0;
                sample += 0.6 * Math.Sin(2 * Math.PI * freq * t + phase);
                sample += 0.3 * Math.Sin(2 * Math.PI * (freq * 0.5) * t + phase * 0.5);
                double env = 1.0 - Math.Exp(-3.0 * t);
                float sVal = (float)(0.6 * env * sample);
                waveWriter.WriteSample(sVal);
            }
        }
        return msWav.ToArray();
    }

    // Generate MP3 bytes by calling ffmpeg if available; falls back to WAV bytes if ffmpeg not found
    public byte[] GenerateAudioMp3(long seed, int id, int seconds = 12)
    {
        var wav = GenerateAudioWav(seed, id, seconds);
        try
        {
            var tempWav = Path.Combine(Path.GetTempPath(), $"song_{seed}_{id}_{Guid.NewGuid()}.wav");
            var tempMp3 = Path.Combine(Path.GetTempPath(), $"song_{seed}_{id}_{Guid.NewGuid()}.mp3");
            File.WriteAllBytes(tempWav, wav);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{tempWav}\" -b:a 128k \"{tempMp3}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                p.WaitForExit(10000);
            }
            if (File.Exists(tempMp3))
            {
                var mp3 = File.ReadAllBytes(tempMp3);
                try { File.Delete(tempWav); } catch { }
                try { File.Delete(tempMp3); } catch { }
                return mp3;
            }
        }
        catch { }
        // fallback to wav
        return wav;
    }

    // Generate simple timestamped lyrics using locale word lists
    public List<(double time, string line)> GenerateLyrics(string language, long seed, int id, int seconds = 12)
    {
        var locale = GetLocale(language);
        string[] words = locale.HasValue && locale.Value.TryGetProperty("lyricsWords", out var lw) ? lw.EnumerateArray().Select(x => x.GetString() ?? "la").ToArray() : new[] { "la", "na", "da", "oh" };
        int s = (int)((seed ^ id ^ (long)seconds) & 0x7FFFFFFF);
        var rng = new Random(s);
        var lines = new List<(double time, string line)>();
        int lineCount = Math.Max(4, seconds / 3);
        for (int i = 0; i < lineCount; i++)
        {
            double t = Math.Round( (double)i * (seconds / (double)lineCount), 2);
            int wordsPerLine = 3 + rng.Next(0,4);
            var parts = new List<string>();
            for (int w = 0; w < wordsPerLine; w++) parts.Add(words[rng.Next(words.Length)]);
            lines.Add((t, string.Join(' ', parts)));
        }
        return lines;
    }

    // Return a single song metadata by global id, using a consistent page size (12) so generator output matches page-based lists
    public Song GenerateSongById(string language, long seed, int id, int pageSize = 12)
    {
        if (id <= 0) return null!;
        int page = ((id - 1) / pageSize) + 1;
        int indexInPage = (id - 1) % pageSize;
        var list = GenerateSongs(language, seed, 0, pageSize, page);
        return list.FirstOrDefault(s => s.Id == id) ?? list.ElementAtOrDefault(indexInPage)!;
    }
}
