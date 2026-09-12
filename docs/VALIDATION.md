# Release validation

Validation is performed against isolated fixture data, not the user's library.

- 41 core/integration checks passed, including live Steam search and artwork caching, storage/backups, title matching, process tracking and launcher-to-child handoff.
- 43 native WPF smoke checks passed, including virtual scrolling, centered controls, small-window layouts, custom artwork, matching selection, trash, decoded-image reuse and enforced cache limits.
- Native screenshots were rendered at standard and small window sizes.
- Installer validation passed: fresh installation, same-identity upgrade, version 6 file metadata, exact Explorer command registration, all 43 UI checks against the installed executable, uninstall removal of executable files and owned Explorer verbs, and preservation of user data and unowned files.
- The normal per-user v5 installation was upgraded to v6. The existing real library file remained byte-identical across the complete installer test sequence.
- The final portable ZIP was extracted into a fresh directory. Its executable hash matched the published build input, and all 43 UI checks passed from the extracted location.
- An independent [GitHub-hosted Windows build](https://github.com/DopaLab/playdeck/actions/runs/34718817804) passed publishing and offline regression tests from the public source.

The test harness uses actual short-lived Windows fixture processes. It does not prove compatibility with every commercial game, anti-cheat or elevated launcher. Physical drag gestures, external clipboard apps, high-DPI hardware, the interactive installer wizard and GPU frame pacing have not been manually exercised in this release.
