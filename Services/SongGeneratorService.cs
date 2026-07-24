using MusicStore.Models;
using System.Text.Json;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
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
                CoverImageUrl = $"/cover/{seed}/{globalId}"
            });
        }

        return songs;
    }

    public byte[] GenerateCoverPng(long seed, int id, string title, string artist)
    {
        int s = (int)((seed ^ id) & 0x7FFFFFFF);
        var rng = new Random(s);
        int w = 400, h = 300;
        using var img = new Image<Rgba32>(w, h);

        // simple vertical gradient
        var c1 = new Rgba32((byte)rng.Next(30,220), (byte)rng.Next(30,220), (byte)rng.Next(30,220));
        var c2 = new Rgba32((byte)rng.Next(30,220), (byte)rng.Next(30,220), (byte)rng.Next(30,220));
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            byte r = (byte)(c1.R + (c2.R - c1.R) * t);
            byte g = (byte)(c1.G + (c2.G - c1.G) * t);
            byte b = (byte)(c1.B + (c2.B - c1.B) * t);
            var rowColor = new Rgba32(r, g, b);
            for (int x = 0; x < w; x++) img[x, y] = rowColor;
        }

        // draw a few translucent rectangles by direct pixel writes
        for (int k = 0; k < 5; k++)
        {
            int rx = rng.Next(-50, w);
            int ry = rng.Next(-50, h);
            int rw = rng.Next(40, 220);
            int rh = rng.Next(30, 160);
            var col = new Rgba32((byte)rng.Next(0,255), (byte)rng.Next(0,255), (byte)rng.Next(0,255), 120);
            for (int yy = Math.Max(0, ry); yy < Math.Min(h, ry + rh); yy++)
            for (int xx = Math.Max(0, rx); xx < Math.Min(w, rx + rw); xx++)
            {
                var dst = img[xx, yy];
                // alpha blend simple
                float a = col.A / 255f;
                img[xx, yy] = new Rgba32(
                    (byte)(dst.R * (1 - a) + col.R * a),
                    (byte)(dst.G * (1 - a) + col.G * a),
                    (byte)(dst.B * (1 - a) + col.B * a),
                    255);
            }
        }

        // save without drawing text (avoids extra ImageSharp.Drawing dependency)
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    // Synthesize WAV audio and return bytes (browser will play audio/wav)
    public byte[] GenerateAudioMp3(long seed, int id, int seconds = 12)
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
}
