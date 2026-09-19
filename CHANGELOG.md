# Changelog

## 6.4.0 — 2026-09-19

- Remove PINNED banners from cards; preserve the Pinned section and manual ordering.
- Put published update dates first: compact date chips, last updated/released sorting, source-note dialog and settings for date/version/off. Date checks reuse the daily cache without scanning installed files.
- Keep version numbers secondary. Add opt-in calendar build-date hints; a later post suggests review, never proves update availability or up-to-date installation.
- Add tracking-off direct launch with no observer; preserve existing statistics. Restrict observed descendants to the game executable directory and lower observer priority only after launching the game.
- Preserve offline unnumbered-update evidence, filter promotional/development titles, and batch library saves during date checks.
- Enlarge existing icon artwork by about 8 percent inside Windows resources, with nine sizes from 16 to 256 pixels. Preserve the supplied design.
- Validate unrelated-child process exit and direct launch. Commercial-game crashes remain unconfirmed; no driver/system settings are changed.

## 6.3.0 — 2026-09-16

- Rebuild local-game comparison around independent installed and published versions. Manual/file versions reach publisher news without any Steam installation or BuildID; support official GitHub releases as an alternate source.
- Show local and published versions side by side, with edition confirmation, release notes, source dates and actionable missing-evidence explanations. Source selection leaves artwork and titles alone.
- Cache successes, empty responses and failures for at least 24 hours. Retain offline release evidence; keep known updates yellow while stale matches become neutral. Reject incompatible numbering, previews and newer unnumbered update ambiguity.
- Include manual entries in daily checks, resume on focus return, persist daily preferences immediately, and report comparison counts instead of a generic completion message.
- Preserve compact version chips, card rendering, launch tracking, tool mode and the established visual style.

## 6.2.0 — 2026-09-15

- Guided version discovery: bounded nearby version files, known Steam installations without a preassigned AppID, and Epic installation records. Confirm evidence, enter versions manually or review unresolved games in sequence.
- Tiny content-sized checkered version chips with centered upright text: green current comparison, yellow newer version, neutral unknown/stale. Click the chip or press V for details.
- Cache successful and failed API checks for at least 24 hours; honor longer server retry delays. Skip pointless build requests for executable-only versions. Automatic checks stop when the launcher loses focus or detects a tracked game.
- Tool mode in Settings and card editors: launch independently, keep the launcher open and skip play tracking. Preserve existing history while excluding tools from game analytics.
- Round only opaque square game icons, retaining transparent silhouettes.
- Reuse frozen drawing resources and badge text; avoid separate invisible controls and repeated layout work for version-chip interaction.


## 6.1.0 — 2026-09-15

- Draw static card content on lightweight surfaces, retain nearby card controls and prewarm adjacent rows during idle time.
- Rebuild Activity with period comparisons, daily rhythm, focus coverage, a 90-day calendar, rankings and session detail. Add breathing room around the focus ring and a vector refresh button.
- Add Version Watch: manual versions, executable metadata, bound Steam manifests, branch-specific cached remote builds, optional daily checks and update sorting.
- Preserve last-known version evidence when an executable or manifest disappears; reject mismatched AppIDs and incomplete installs.
- Release the initial launch process handle immediately. Direct and handoff fixture games exit cleanly; no game termination is performed. The reported commercial-game lingering process was not reproduced.


## 6.0.0 — 2026-09-12

### Performance
- Retain overlapping virtualized rows instead of rebuilding every nearby card.
- Reuse frozen decoded images with a 48 MiB / 160-entry LRU ceiling and file-change invalidation.
- Index game history, share frozen brushes and body fonts, debounce resize rebuilds, and skip unnecessary focus-triggered refreshes.
- Add a reproducible native layout benchmark.

### Tracking and history
- Check process trees more often during startup and handoff; retain validated parent lineage.
- Validate tracker process birth time to distinguish PID reuse from a live tracker.
- Record sampled foreground/background time separately.
- Reject suspend/resume gaps and allocate new playtime across local calendar days.
- Add weekly comparisons, average/longest sessions, status visibility and CSV focus columns.
- Preserve compatibility with older history records.

### Release
- Retain the approved upright typography, artwork, profile, continuous library and manual pins.
- Per-user installer/uninstaller, self-contained portable build, public source and illustrated documentation.
