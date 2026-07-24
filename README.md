Music Store Showroom

Authentication
- This application does not require registration or authentication. All pages and API endpoints are public and accessible without login.

Language independence
- The app supports multiple locale data files under Data/locales-*.json (e.g., en-US, de-DE).
- Songs are generated independently for each selected locale; the same song is not translated between languages.

Notes
- For security/audit: authorization middleware was removed because the app intentionally runs without auth.
- Locale files can be extended by adding new Data/locales-*.json files with arrays for firstNames, lastNames, genres, albumWords and a top-level "locale" field.
