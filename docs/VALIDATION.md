# Release validation

All test runs use isolated fixtures rather than the real library.

- 55 offline core/integration checks; the network suite adds six live Steam artwork/search/provider checks.
- 47 native WPF checks cover launch/editor routing, continuous scrolling, reused rows, bounded caches, pins, trash, matching, small layouts, Activity ranges and manual version editing.
- Actual WPF captures were inspected at standard and small window sizes. Charts use vector drawing and upright bundled fonts.
- Direct and handoff fixture processes leave no running game after completion. This does not reproduce or establish the cause of a commercial game remaining open.
- The release workflow builds self-contained packages, extracts and exercises the portable ZIP, installs and exercises the installer, then uninstalls it. Each packaged executable must pass all 47 UI checks before upload.

See the public [release workflow](https://github.com/DopaLab/playdeck/actions/workflows/release.yml) for the exact commit's result. SHA256SUMS accompanies each release.

Commercial anti-cheat/elevated-game compatibility, physical high-DPI hardware, interactive installer clicks and GPU frame pacing are not established by these tests. The unsigned installer preserves user data by design.
