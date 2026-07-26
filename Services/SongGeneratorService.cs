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
        // Advanced SVG cover with realistic design patterns: geometric shapes, varied gradients, text effects
        int s = (int)((seed ^ id) & 0x7FFFFFFF);
        var rng = new Random(s);
        int w = 400, h = 300;
        var sb = new StringBuilder();
        
        sb.Append($"<svg xmlns='http://www.w3.org/2000/svg' width='{w}' height='{h}' viewBox='0 0 {w} {h}'>");
        sb.Append("<defs>");
        
        // Define multiple gradient patterns for visual variety
        var patterns = rng.Next(0, 4);
        if (patterns == 0)
        {
            // Diagonal gradient pattern with accent color
            var c1 = (rng.Next(20, 100), rng.Next(20, 100), rng.Next(100, 200));
            var c2 = (rng.Next(100, 180), rng.Next(80, 150), rng.Next(20, 80));
            var c3 = (rng.Next(200, 255), rng.Next(150, 220), rng.Next(50, 150));
            sb.Append($"<linearGradient id='g1' x1='0%' y1='0%' x2='100%' y2='100%'>");
            sb.Append($"<stop offset='0%' stop-color='rgb({c1.Item1},{c1.Item2},{c1.Item3})'/>");
            sb.Append($"<stop offset='50%' stop-color='rgb({c2.Item1},{c2.Item2},{c2.Item3})'/>");
            sb.Append($"<stop offset='100%' stop-color='rgb({c3.Item1},{c3.Item2},{c3.Item3})'/>");
            sb.Append("</linearGradient>");
        }
        else if (patterns == 1)
        {
            // Radial gradient (spotlight effect)
            var c1 = (rng.Next(200, 255), rng.Next(100, 200), rng.Next(50, 150));
            var c2 = (rng.Next(20, 60), rng.Next(10, 50), rng.Next(60, 120));
            sb.Append($"<radialGradient id='g1' cx='50%' cy='30%' r='70%'>");
            sb.Append($"<stop offset='0%' stop-color='rgb({c1.Item1},{c1.Item2},{c1.Item3})'/>");
            sb.Append($"<stop offset='100%' stop-color='rgb({c2.Item1},{c2.Item2},{c2.Item3})'/>");
            sb.Append("</radialGradient>");
        }
        else if (patterns == 2)
        {
            // Vibrant vertical stripes
            var ca = (rng.Next(150, 255), rng.Next(30, 100), rng.Next(100, 180));
            var cb = (rng.Next(20, 80), rng.Next(150, 255), rng.Next(100, 180));
            sb.Append($"<linearGradient id='g1' x1='0%' y1='0%' x2='100%' y2='0%'>");
            sb.Append($"<stop offset='0%' stop-color='rgb({ca.Item1},{ca.Item2},{ca.Item3})'/>");
            sb.Append($"<stop offset='100%' stop-color='rgb({cb.Item1},{cb.Item2},{cb.Item3})'/>");
            sb.Append("</linearGradient>");
        }
        else
        {
            // Cool blue to warm purple gradient
            var c1 = (rng.Next(20, 100), rng.Next(120, 200), rng.Next(180, 255));
            var c2 = (rng.Next(150, 220), rng.Next(30, 100), rng.Next(180, 255));
            sb.Append($"<linearGradient id='g1' x1='0%' y1='100%' x2='100%' y2='0%'>");
            sb.Append($"<stop offset='0%' stop-color='rgb({c1.Item1},{c1.Item2},{c1.Item3})'/>");
            sb.Append($"<stop offset='100%' stop-color='rgb({c2.Item1},{c2.Item2},{c2.Item3})'/>");
            sb.Append("</linearGradient>");
        }
        sb.Append("</defs>");
        
        // Background with main gradient
        sb.Append($"<rect width='100%' height='100%' fill='url(#g1)' />");
        
        // Add geometric decorative elements (circles, polygons) for visual interest
        int shapeCount = 3 + rng.Next(0, 3);
        for (int i = 0; i < shapeCount; i++)
        {
            double opacity = 0.1 + rng.NextDouble() * 0.3;
            int cx = rng.Next(0, w);
            int cy = rng.Next(0, h);
            int radius = rng.Next(30, 150);
            int colorR = rng.Next(0, 255);
            int colorG = rng.Next(0, 255);
            int colorB = rng.Next(0, 255);
            sb.Append($"<circle cx='{cx}' cy='{cy}' r='{radius}' fill='rgba({colorR},{colorG},{colorB},{opacity:F2})' />");
        }
        
        // Add some geometric lines for accent
        int lineCount = 2 + rng.Next(0, 3);
        for (int i = 0; i < lineCount; i++)
        {
            int x1 = rng.Next(0, w);
            int y1 = rng.Next(0, h);
            int x2 = rng.Next(0, w);
            int y2 = rng.Next(0, h);
            int strokeWidth = rng.Next(1, 4);
            int opacity = rng.Next(30, 100);
            sb.Append($"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='rgba(255,255,255,{opacity / 100.0:F2})' stroke-width='{strokeWidth}' />");
        }
        
        // Title and artist text with shadows and styling
        var safeTitle = System.Security.SecurityElement.Escape(title ?? "");
        var safeArtist = System.Security.SecurityElement.Escape(artist ?? "");
        
        // Text shadow for better readability
        sb.Append($"<text x='22' y='52' font-family='Arial, sans-serif' font-size='20' font-weight='bold' fill='rgba(0,0,0,0.3)'>{safeTitle}</text>");
        sb.Append($"<text x='20' y='50' font-family='Arial, sans-serif' font-size='20' font-weight='bold' fill='white'>{safeTitle}</text>");
        
        sb.Append($"<text x='22' y='{h - 28}' font-family='Arial, sans-serif' font-size='14' fill='rgba(0,0,0,0.3)'>{safeArtist}</text>");
        sb.Append($"<text x='20' y='{h - 30}' font-family='Arial, sans-serif' font-size='14' fill='rgba(255,255,255,0.95)'>{safeArtist}</text>");
        
        sb.Append("</svg>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // Generate music with chord progressions, varied instruments, and music theory
    public byte[] GenerateAudioWav(long seed, int id, int seconds = 12)
    {
        int s = (int)((seed ^ id) & 0x7FFFFFFF);
        var rng = new Random(s);
        int sampleRate = 44100;
        int channels = 1;
        int totalSamples = sampleRate * seconds;

        // Music theory: chord progressions in a key (C major)
        double[] majorScale = { 262, 294, 330, 349, 392, 440, 494, 523 }; // C D E F G A B C
        int[] chordPattern = { 0, 4, 0, 4, 7, 4, 0, 4 }; // C-Fmaj7-C-Fmaj7-Gmaj7-Fmaj7-C-Fmaj7
        double baseTempo = 90 + (rng.Next(0, 40)); // vary tempo 90-130 BPM

        using var msWav = new MemoryStream();
        using (var waveWriter = new WaveFileWriter(msWav, new WaveFormat(sampleRate, channels)))
        {
            double t = 0;
            int sampleIdx = 0;
            while (sampleIdx < totalSamples)
            {
                // Calculate beat position
                double beatLength = (60.0 / baseTempo) * sampleRate;
                int beatNum = (int)(sampleIdx / beatLength);
                double chordIdx = beatNum % chordPattern.Length;
                double rootFreq = majorScale[(int)(chordIdx) % majorScale.Length];
                
                // Chord notes (root + 3rd + 5th harmonies)
                double[] chordNotes = {
                    rootFreq,
                    rootFreq * 1.25,  // major 3rd
                    rootFreq * 1.5    // perfect 5th
                };

                // Generate sample with multiple harmonies and slight modulation
                double sample = 0.0;
                for (int i = 0; i < chordNotes.Length; i++)
                {
                    // Add vibrato and envelope
                    double freq = chordNotes[i] * (1.0 + 0.02 * Math.Sin(2 * Math.PI * 5 * t)); // 5Hz vibrato
                    double envelope = Math.Exp(-0.3 * (t % (beatLength / sampleRate))); // decay within beat
                    sample += (0.3 / chordNotes.Length) * envelope * Math.Sin(2 * Math.PI * freq * t);
                }

                // Add passing tones and bass line
                double bassFreq = rootFreq * 0.5;
                double bassEnvelope = 0.5 * Math.Exp(-0.5 * (t % (beatLength / sampleRate)));
                sample += 0.2 * bassEnvelope * Math.Sin(2 * Math.PI * bassFreq * t);

                // Add a slightly detuned upper harmony (chorus effect)
                double upperFreq = chordNotes[0] * 1.002;
                sample += 0.15 * Math.Sin(2 * Math.PI * upperFreq * t);

                // Add a touch of random variation (humanization)
                sample += 0.02 * (rng.NextDouble() - 0.5);

                float sVal = (float)Math.Max(-1, Math.Min(1, 0.6 * sample));
                waveWriter.WriteSample(sVal);

                t += 1.0 / sampleRate;
                sampleIdx++;
            }
        }
        return msWav.ToArray();
    }

    // Keep legacy GenerateAudioWav for fallback; rename to GenerateAudioWavSimple if needed

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
