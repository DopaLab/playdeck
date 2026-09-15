# Version Watch

Use Game version in the card editor, or Settings → Version Watch. Online checks are optional; each game can opt into a daily check when the launcher opens. No resident version service is installed.

## Evidence sources

- **Manual:** enter installed and latest labels. Numeric dotted versions and prereleases can be ordered; unrelated labels are not guessed.
- **Executable:** read ProductVersion, falling back to FileVersion, from the actual tracking executable. These fields can describe an engine or launcher rather than the game's release.
- **Steam:** read appmanifest files in known Steam libraries, matching AppID and installation path. A manually chosen manifest must match the selected AppID. The installed branch is retained; incomplete installations cannot report update availability.

A missing source retains dated last-known evidence bound to that game's executable. It is not treated as a currently verified installation. Steam build IDs are never compared with executable version strings.

## Online comparison

Valve's official [GetAppBuilds endpoint](https://partner.steamgames.com/doc/webapi/ISteamApps) requires publisher credentials and is not a general consumer version service. Playdeck uses the public, third-party [SteamCMD API](https://www.steamcmd.net/) and its [open source implementation](https://github.com/steamcmd/api) for branch build metadata. It sends only the requested AppID, not paths or library contents.

Successful responses are cached locally for 24 hours. Failed requests back off for one hour and retain dated evidence without a fresh update claim. Requests are serialized, time out after 12 seconds and have a bounded response size. Protected branches are excluded. Global offline mode performs local checks only.

A higher reported build is an update hint, not a guarantee about distribution availability. Lower IDs can indicate rollback or branch changes. The provider may lag or be unavailable. No automatic game updater, patch downloader, or arbitrary website scraper is included.

Manifest parsing rejects malformed, duplicate, oversized or deeply nested input. Tests cover AppID binding, branches, incomplete installs, version ordering, cache reuse, network failure and preservation of missing-source evidence.
