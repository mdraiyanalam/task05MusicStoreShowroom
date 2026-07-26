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
