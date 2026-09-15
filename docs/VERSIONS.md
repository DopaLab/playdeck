# Version Watch

Click the small version chip, press V on a focused card, or open Settings → Version Watch. Choose Find version files, select evidence and confirm Use selected. Review next game moves through unresolved cards; Enter version opens a simple manual form. Online checks are optional; each game can opt into a daily check when the launcher opens. No resident version service is installed.

## Card status

The compact chip shows only the version (Steam build IDs use a b prefix). Green means a matching comparison within the past 24 hours; yellow means a newer comparable version was reported. Grey means unknown, unverified, unavailable or stale. The tooltip and guided dialog retain the full source/status. Manual latest-version notes must be recently confirmed to produce green; rereading an old note does not renew its authority.

## Evidence sources

- **Local files:** version.txt, version, build.txt, build.version, version.json, gameinfo.json, app.info and selected GOG info fields near the game. Epic .item records are matched by installation path. These are suggestions requiring confirmation; engine/schema versions must not be guessed as game releases. GOG info schema version is explicitly excluded. Confirmed files can be reread later.
- **Manual:** enter installed and latest labels. Numeric dotted versions and prereleases can be ordered; unrelated labels are not guessed.
- **Executable:** read ProductVersion, falling back to FileVersion, from the actual tracking executable. These fields can describe an engine or launcher rather than the game's release.
- **Steam:** read appmanifest files in known Steam libraries, matching AppID and installation path. A manually chosen manifest must match the selected AppID. The installed branch is retained; incomplete installations cannot report update availability.

A missing source retains dated last-known evidence bound to that game's executable. It is not treated as a currently verified installation. Steam build IDs are never compared with executable version strings.

## Online comparison

Valve's official [GetAppBuilds endpoint](https://partner.steamgames.com/doc/webapi/ISteamApps) requires publisher credentials and is not a general consumer version service. Playdeck uses the public, third-party [SteamCMD API](https://www.steamcmd.net/) and its [open source implementation](https://github.com/steamcmd/api) for branch build metadata. It sends only the requested AppID, not paths or library contents.

Successful responses are cached locally for 24 hours. Failed requests back off for at least 24 hours and retain dated evidence without a fresh update claim. Longer Retry-After values are respected. Different uncached requests are paced at least one second apart. Requests are serialized, time out after 12 seconds and have a bounded response size. Protected branches are excluded. Global offline mode performs local checks only.

A higher reported build is an update hint, not a guarantee about distribution availability. Lower IDs can indicate rollback or branch changes. The provider may lag or be unavailable. No automatic game updater, patch downloader, or arbitrary website scraper is included.

Manifest parsing rejects malformed, duplicate, oversized or deeply nested input. Tests cover AppID binding, branches, incomplete installs, version ordering, cache reuse, network failure and preservation of missing-source evidence.


## Bounds and limitations

Discovery is read-only and off the UI thread. It checks at most four nearby ancestor directories (stopping at shared game folders), up to six candidate directories, 512 Steam manifests across up to 16 known libraries, and 256 Epic records. It never recursively searches drives. Individual evidence files are limited to 256 KiB; parsed Steam evidence is cached with file-change invalidation, at most 512 entries / 4 MiB of source bytes (parsed objects use additional memory).

Confirmed non-Steam versions still need a manually entered latest version: there is no universal public API for every game's human-readable release label. Conflicting or unavailable evidence remains neutral. Only Steam build IDs on the same branch receive automatic online comparisons. No new API key or account is required.

Automatic checks are optional when Playdeck opens and stop on focus loss, launch or a known active play session. They do not install a service or continue after the launcher closes. Tools are excluded from library-wide automatic checks.
