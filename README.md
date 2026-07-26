Music Store Showroom
====================

Overview
--------
A server-side music showroom that generates deterministic, reproducible songs (audio previews, covers, metadata, and lyrics) on request. Designed for language independence and running without authentication — all public endpoints are intentionally open.

Key policies
------------
- Authentication: No registration or authentication required. Public endpoints by design.
- Language independence: Locale data (titles, artists, genres, album words, review words, lyrics words) comes from Data/locales-*.json files. Add or modify JSON locale files to add languages.
- Parameter independence: region (language), seed, and likes are independent.
  - Changing likes updates only counts (no regeneration of titles/artists/albums).
  - Changing seed or region regenerates titles, artists, albums, genres, and covers deterministically.

Endpoints (examples)
---------------------
- GET /                    — Main UI (Razor Pages)
- GET /songs?seed=42&page=1&language=en-US&LikesPerSong=3.7
- GET /audio/{seed}/{id}   — Returns audio preview (MP3 if available, WAV fallback)
- GET /cover/{language}/{seed}/{id} — SVG cover image with title/artist rendered
- GET /lyrics/{language}/{seed}/{id} — Timestamped lyrics JSON
- GET /export?seed=42&page=1&count=12&language=en-US — ZIP with MP3s for page
- GET /export-batch?seed=42&startPage=1&endPage=5&count=12&language=en-US — merged ZIP
- GET /likes?seed=42&page=1&count=12&LikesPerSong=3.7 — (likes-only update)

Developer setup (Windows)
-------------------------
1. Install .NET SDK (match GitHub Actions):
   - e.g. install .NET 8: https://dotnet.microsoft.com/en-us/download
2. Clone repository and checkout worktree branch or main:
   - git clone https://github.com/<owner>/task05MusicStoreShowroom.git
   - cd task05MusicStoreShowroom
3. Restore and build:
   - dotnet restore
   - dotnet build -c Debug
4. Run locally:
   - dotnet run --project ./task05MusicStoreShowroom.csproj
   - App serves on http://localhost:5000 by default
5. Smoke-test endpoints from PowerShell/Curl or browser (see Endpoints above)

Running tests (unit + Playwright)
---------------------------------
- Unit tests: dotnet test
- Playwright E2E: ensure Node/npm installed, then run:
  - npm install
  - npx playwright install --with-deps
  - npx playwright test

CI troubleshooting (common failures)
-----------------------------------
Symptom: ".NET Build / build (pull_request) - Failing after 1m" and tests skipped.
Steps to diagnose and fix:
1. View the GitHub Actions run logs (Actions → failing run → Logs) to see the error.
2. Reproduce locally: run dotnet build and dotnet test on the same SDK/version used by CI.
3. Common fixes:
   - Ensure workflow uses correct .NET SDK (setup-dotnet with dotnet-version: '8.0.x' if project targets net8.0)
   - Add explicit dotnet restore before build
   - Install Playwright browsers in CI: add "npm ci" and "npx playwright install --with-deps" steps
   - Install ffmpeg in CI if MP3 conversion is required: on Ubuntu runs add `sudo apt-get update && sudo apt-get install -y ffmpeg`
   - Increase job timeout if long-running conversions cause timeouts
   - Ensure tests don't rely on interactive UI or locked files (stop local server before running CI build)

Recommended GitHub Actions snippet to add dependencies and run build/tests:

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - name: Restore
        run: dotnet restore
      - name: Install Node & Playwright browsers
        uses: actions/setup-node@v4
        with:
          node-version: '18'
      - name: Install Playwright deps
        run: |
          npm ci
          npx playwright install --with-deps
      - name: Install ffmpeg
        run: sudo apt-get update && sudo apt-get install -y ffmpeg
      - name: Build
        run: dotnet build -c Release --no-restore
      - name: Run unit tests
        run: dotnet test --no-restore --verbosity normal
      - name: Run Playwright tests
        run: npx playwright test --reporter=list
```

How to merge a feature branch to main (locally & on remote)
---------------------------------------------------------
Option A — Fast merge via GitHub UI (recommended for PR reviews):
1. Push feature branch: git push -u origin feature-branch
2. Open PR on GitHub, review, and click Merge ("Merge pull request")
3. Pull main locally: git checkout main && git pull origin main

Option B — Local merge and push (command-line):
1. Update local main: git fetch origin && git checkout main && git pull origin main
2. Merge feature branch: git merge --no-ff feature-branch
3. Resolve conflicts if any, then commit
4. Push main: git push origin main

Option C — Rebase workflow (clean history):
1. git checkout feature-branch
2. git fetch origin
3. git rebase origin/main
4. Resolve conflicts and continue rebase
5. git checkout main && git merge --ff-only feature-branch
6. git push origin main

How to pull changes on your laptop and test
------------------------------------------
1. git checkout main
2. git pull origin main
3. dotnet restore
4. dotnet build -c Debug
5. dotnet run --project ./task05MusicStoreShowroom.csproj
6. Check endpoints in browser or with curl/powershell
7. Run unit tests: dotnet test
8. Run Playwright tests (if present): npm ci && npx playwright install && npx playwright test

Fixing the failing CI build in your screenshot (quick checklist)
----------------------------------------------------------------
1. Open the failing workflow run and inspect the step that failed for stack traces.
2. If the failure is "dotnet build" reproduce by running `dotnet build` locally and fix compile errors.
3. If tests are skipped, ensure tests are present and the workflow does not conditionally skip them. Add `dotnet test` step.
4. If Playwright tests are skipped because browsers are missing, add `npx playwright install` to the workflow.
5. If mp3 conversion fails due to missing ffmpeg, add install step in CI or make code gracefully fall back to WAV without failing tests.

If you'd like, next actions I can take now
-----------------------------------------
- Update README.md directly in the repo (done) and open a PR + merge it — proceed now.
- Inspect the failing CI run logs and propose exact code/workflow fixes.
- Add Playwright install and ffmpeg steps to the CI workflow and open a PR to fix the build.

Which of the above should I do next? (pick one)
- Open README PR and merge (recommended)
- Inspect CI logs and propose a specific fix
- Add Playwright/ffmpeg to CI workflow and open PR to repair build
- Do all three (takes longer)


User experience — What you'll see and how to use the Music Store Showroom
---------------------------------------------------------------------------
This single-page app presents a horizontal toolbar with three controls: Language, Seed, and Likes-per-song. Controls update the displayed data immediately (no submit buttons). Use the seed field to enter a 64-bit integer or click the circular ↻ button to generate a random seed.

- Table View: paginated. Click table rows to expand/collapse a detailed panel with a larger cover, preview player, synced lyrics, and a short review. Pagination resets to page 1 when language or seed change.
- Gallery View: infinite scroll (batches/pages are loaded as you scroll). Changing seed or language resets scroll to the top and the gallery batch counter.
- Likes-per-song: accepts fractional values (0–10). Adjusting likes updates only the like counts; titles, artists, albums and covers remain stable unless seed or language changes.
- Export: Export (current page) and Export All (batch ZIP) buttons create server-side ZIP archives of MP3 files. Per-row "Export Song" is available in expanded table details.

Everything is generated server-side on demand; the browser requests a single page/batch from the server and renders it. No user account is required.

Developer Statement / Technical limitations — Why the generated lyrics sound noisy and what would be required to improve them
-------------------------------------------------------------------------------------------------------
User-facing summary

The app generates plausible-looking song metadata and short lyric lines, but the lyrics may sound noisy, repetitive, or unnatural. This is expected: the lyric generator composes lines by sampling words from locale-specific lists and placing them into simple templates. It is intentionally offline, deterministic, and lightweight so the entire app remains reproducible and runs without external APIs.

Technical explanation

The current lyric generator does not produce real, coherent song lyrics. It works by randomly selecting words from locale-specific word lists and arranging them using simple hardcoded templates. Because of this approach:

- There is no language model (Transformer, RNN, or LLM)
- There is no understanding of meaning, grammar, or context
- There are no rhyme, meter, or syllable-count constraints
- There is no alignment with musical phrase structure

As a result, the output is only syntactically plausible but semantically noisy and unnatural.

What would be required to generate realistic lyrics

To produce high-quality, natural-sounding lyrics, the system would need one or more of the following:

- A trained language model (Transformer / RNN) or an external LLM (e.g. GPT)
- Explicit rhyme and meter rules + syllable counting
- Templates tied to actual musical phrase lengths
- A properly licensed lyrical corpus for training or few-shot prompting

Trade-offs the developer must accept

Approach                 | Realism | Cost / Complexity | Licensing Risk
------------------------ | ------- | ----------------- | --------------
Current (word lists)     | Low     | Very low          | None
Rule-based + templates   | Medium  | Medium            | Low
Local language model     | High    | High (training + compute) | Medium
External LLM API (GPT)   | Very High | Very High (API cost + latency) | High

Developer decision

At this stage of the project the decision was to keep lyric generation lightweight, deterministic, and offline using deterministic word-list composition. Realistic lyric generation is intentionally left as a future improvement requiring significantly more compute, model infra, and careful copyright/licensing handling.


Technical notes and next steps (if you want improved lyrics)
-----------------------------------------------------------
- Integrate an LLM via an API (fast to prototype but adds cost and dependency and requires careful prompt design and licensing review).
- Build or adapt a small local language model and add rhyme/meter heuristics (costly in infrastructure and engineering effort).
- Implement rhyme and syllable-counting rules and tie templates to musical phrase lengths for a middleground improvement with moderate cost.


File and data notes
-------------------
- Locale text must come from Data/locales-*.json. Do not hardcode region-specific strings in code.
- Music/audio is generated deterministically from the combined seed+page and record index so the same inputs always produce identical audio output.


If you'd like, I can now:
- Open a small PR with this README change and merge it (recommended)
- Replace IHtmlHelper.Partial usage with the <partial> tag helper to address MVC1000 warnings
- Add Playwright and ffmpeg install steps to CI and open a PR to repair the failing build

Which action should I take next?