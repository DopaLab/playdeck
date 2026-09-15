<p align="center"><img src="docs/hero.png" alt="Playdeck — Less waiting. More playing." width="100%"></p>

<p align="center">
<a href="https://github.com/DopaLab/playdeck/releases/latest"><img src="https://img.shields.io/github/v/release/DopaLab/playdeck?style=for-the-badge&color=E1FF46&labelColor=222222" alt="Latest release"></a>
<img src="https://img.shields.io/badge/Windows-x64-76DDD3?style=for-the-badge&labelColor=222222" alt="Windows x64">
<img src="https://img.shields.io/badge/Native-WPF-FF957F?style=for-the-badge&labelColor=222222" alt="Native WPF">
<a href="https://ko-fi.com/fgtranime"><img src="https://img.shields.io/badge/Support-Ko--fi-FF5E5B?style=for-the-badge&labelColor=222222" alt="Support on Ko-fi"></a>
</p>

<h3 align="center">Your games deserve a better shelf.</h3>
<p align="center">A personal Windows launcher with bold cover cards, instant favorites and local play history.<br>No account. No folder crawler. No always-running launcher.</p>
<p align="center"><b><a href="https://github.com/DopaLab/playdeck/releases/latest/download/Playdeck-Setup-6.2.0.exe">↓ Install Playdeck</a> · <a href="https://github.com/DopaLab/playdeck/releases/latest/download/Playdeck-v6.2.0-win-x64.zip">Portable ZIP</a> · <a href="#your-library-in-motion">See the app</a></b></p>

## Your library, in motion

![Actual Playdeck library with demonstration games](docs/library.png)

<table>
<tr><td width="50%"><b>01 / Make it yours</b><br>Right-click a game in Explorer → Add to Playdeck. Choose a Steam match for artwork, or paste your own cover. Your profile, your order, your library.</td><td width="50%"><b>02 / Get straight to the game</b><br>Click a card to launch. Use its three dots to edit. Pin favorites to a separate shelf and drag them into order. The launcher closes after a successful launch.</td></tr>
<tr><td><b>03 / Keep your story</b><br>Playtime, 7/30/90-day comparisons, a 90-day calendar, focus ring, game rankings and session history. New sessions distinguish foreground from background time and split across midnight.</td><td><b>04 / Keep it recoverable</b><br>Archive without losing cached art or history. Trash cards for seven days, restore mistakes, or empty the trash. Installed game files are never deleted.</td></tr>
</table>

<details open><summary><b>Activity & favorites — actual application captures</b></summary>

| Your play story | Your instant-play shelf |
|:--:|:--:|
| ![Activity](docs/activity.png) | ![Pinned games](docs/pinned.png) |

</details>

<details><summary><b>Fallback artwork, settings & title matching</b></summary>

![Cards without downloaded covers](docs/fallback.png)

| Settings | Choose the right game |
|:--:|:--:|
| ![Settings](docs/settings.png) | ![Steam matching](docs/matching.png) |

</details>

The banner is AI-generated promotional art. Application images are real WPF captures with demonstration data; game artwork belongs to its respective owners.

## Built to do less work

- **Continuous, virtualized scrolling:** nearby cards are drawn on lightweight surfaces; overlapping rows survive updates and up to 72 nearby controls are reused (or more if needed for the visible viewport). Idle work prewarms adjacent rows.
- **Decoded artwork reuse:** frozen bitmaps in a bounded 48 MiB / 160-entry LRU cache. File changes invalidate entries. This is a cache budget, not a total RAM promise.
- **Indexed history:** cards look up their game's sessions instead of repeatedly scanning the entire history.
- **Quieter UI:** focus changes reload history only when the session directory changes; resizing coalesces expensive rebuilds.
- **No idle tracker:** a separate helper runs during a launched game, checkpoints every 30 seconds, and exits afterward. No boot service or scheduled task.
- **Cached metadata:** completed lookups and failures are remembered; manual Steam search results are cached for one day.

Historical v6.0 → v6.1 synthetic test (see the v6.2 follow-up in the performance document): 2,000 games, 20,000 sessions, eight preloaded covers, five refreshes and 100 scroll steps.

| WPF workload | v6.0 | v6.1 |
|---|---:|---:|
| Median library refresh | 130 ms | 40 ms |
| 95th-percentile scroll layout update | 23.6 ms | 12.3 ms |
| Managed allocation during workload | 29.8 MiB | 15.1 MiB |
| Repeated image decodes | 0 | 0 |
| Maximum realized cards | 25 | 25 |

Local synthetic layout measurements, **not GPU frame rates or universal guarantees**. Median scroll updates were 1.2 ms and 1.0 ms. Working-set snapshots were 147 and 151 MiB: this change reduces rendering work and allocation churn, not measured total RAM. See [method and raw results](docs/PERFORMANCE.md).

## Know which version you have

Click a card's small version chip (or press **V** on a focused card), or open **Settings → Version Watch**.

**Find version files → select evidence → Use selected.** Playdeck checks nearby version files and known Steam/Epic installation records. Enter a version from the game's menu when no reliable evidence exists. Review next game helps work through unresolved cards.

Small checkered chips show the version: **green** for a recent matching comparison, **yellow** for a newer version, and **grey** for uncertain or stale evidence. They do not stretch across the artwork.

- Read confirmed version files on later checks, record versions manually, read executable metadata, or identify a Steam installation even before an AppID has been assigned.
- Compare matching Steam builds through the third-party SteamCMD API. Results are cached for 24 hours, with a minimum 24-hour retry delay after failures.
- Opt individual games into checks when Playdeck opens. Update badges and the **Version updates** sort bring reported newer builds forward.
- Preserve dated last-known evidence when a source disappears. No game files are modified or updates installed.

Executable version labels and Steam build IDs are different systems. Playdeck never compares them against each other; arbitrary releases need manual latest-version input. Public branch data can lag, and protected branches may be unavailable. [Methods, privacy and limitations](docs/VERSIONS.md).

![Guided version review with demonstration evidence](docs/version-guide.png)

## Install & play

1. Download the installer from [Releases](https://github.com/DopaLab/playdeck/releases/latest). Windows x64; the .NET runtime is included.
2. Install for your Windows account. Start menu and Explorer integration are included; the desktop shortcut is optional.
3. Right-click a game executable or shortcut → **Add to Playdeck**. On Windows 11, use **Show more options**. File picking and drag-and-drop also work.
4. Review the name, choose a cover match if needed, and click its card to play.

Uninstall through **Windows Settings → Apps**. Data in `%LOCALAPPDATA%\Playdeck` is intentionally retained, including during upgrades. The portable ZIP uses that same location. This is an **unsigned** community build; release hashes are supplied for integrity checking.

## A few useful details

| Action | How |
|---|---|
| Edit a card | Three dots in its upper corner |
| Paste a cover | Ctrl+V in the editor |
| Correct a title / find artwork | Find cover or Choose Steam match; select a result to apply it |
| Rearrange favorites | Drag cards in Pinned; other library sorts work independently |
| Remove junk cards | Select mode, or hold Delete while clicking a card |
| Start a fresh library | Settings; old cards move to recoverable trash |
| Export history | Settings → CSV, including focus time and session status |
| Customize profile | Click the player badge |
| Gaming utility / mod manager | Settings → Manage tools, or Mark as tool in its card editor |

### Tracking & privacy

**Tool mode** opens utilities independently, keeps Playdeck open and creates no play sessions. Existing tool history is retained but excluded from game analytics. Square opaque game icons receive rounded corners; transparent or shaped icons keep their outlines.

Only games launched **through Playdeck** are tracked. Process lifetime includes pauses; foreground time means the game owned the foreground window, not proof of active input. Sampling is every two seconds, so durations are approximate. Sleep gaps of ten seconds or more are excluded. Interrupted runs retain the last checkpoint, potentially losing about 30 seconds. Older sessions keep their original totals and use their start date in daily charts.

Exact executable identity and descendants help follow ordinary launcher handoffs. Very short bootstrap processes, elevated/protected games, anti-cheat and storefront reuse may require setting the actual tracking executable. Commercial-game compatibility is not universal. Launch-only and undetected sessions do not invent playtime.

Library, profile, artwork and history live locally. Metadata lookup contacts Steam services/CDNs; enabled version checks also send the game AppID to the third-party SteamCMD API. Online access can be disabled in settings. No telemetry, cloud account or library upload. Empty Trash removes card records; raw sessions, artwork and backups may remain. It is not secure erasure.

## Build it yourself

Requires Windows, [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), and [Inno Setup 6.7+](https://jrsoftware.org/isinfo.php) for the installer.

```powershell
./build.ps1            # Self-contained portable build + regression tests
./build.ps1 -Installer # Also compile the installer
```

The UI and tracker publish separately. Only tracker-specific binaries are copied into the app directory, preserving WPF's WindowsBase runtime. See [validation](docs/VALIDATION.md), [changelog](CHANGELOG.md), [MIT code license](LICENSE) and [asset notices](THIRD_PARTY_NOTICES.md).

<p align="center"><b>Made for the moment you actually want to play.</b><br><a href="https://ko-fi.com/fgtranime">☕ Support development on Ko-fi</a> · <a href="https://github.com/DopaLab/playdeck/issues">Report a bug</a></p>
