# Changelog

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
