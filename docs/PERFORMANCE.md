# Performance measurements

## v6.1 follow-up — 2026-09-15

Same machine and fixture described below, comparing the published v6.0 build with v6.1. Raw results: [v6.0](v60.bench.json), [v6.1](v61.bench.json). Refresh median: 130.13 → 40.25 ms. Scroll P95: 23.65 → 12.30 ms. Scroll median: 1.20 → 1.00 ms. Managed allocations: 29.77 → 15.10 MiB. Both realized 25 cards and decoded zero additional warm images. Working set: 147.38 → 151.22 MiB.

Static card content now uses one drawing surface. An LRU retains 72 nearby card controls, expanding if the visible viewport requires more. Idle callbacks prepare adjacent rows. The synthetic test does not fully exercise idle prewarming and does not measure GPU frame pacing.

## Historical v5 → v6 measurements

Measured locally on Windows x64, Intel Core i5-3470 at 3.20 GHz, on 2026-09-12. These are synthetic WPF UI-thread layout timings, not GPU frame pacing, physical input latency or universal hardware guarantees.

The baseline uses v5 source with only Benchmark.cs and the benchmark capture entry point added. v6 uses the same workload. Both are Release, self-contained win-x64, ReadyToRun builds at 1240 x 820.

## Fixture and method

- 2,000 game entries; 20,000 in-memory sessions, ten per game.
- Eight distinct cover files from the demonstration library, already read during initial screen construction.
- Five complete Render + UpdateLayout passes.
- 100 scroll offsets: 0–49 times 110 pixels, then the reverse.
- Dispatcher background turns between samples.
- Report median refresh, median and 95th-percentile scroll layout duration; process-wide managed allocation deltas; repeated image decodes; maximum realized card count.
- Managed allocation is cumulative churn, not retained heap. Working set is a single process snapshot and includes framework/runtime resources.
- The optimized cache records zero extra decodes because those eight images are already warm. This does not imply unique artwork is free to load.

Raw measurements: [v5 baseline](baseline.bench.json), [initial optimized run](optimized.bench.json), [final build](final.bench.json). These historical results predate the v6.1 README comparison. No averages across selectively chosen machines.

An additional [idle/startup sample](idle.json), using eight demonstration games, reached Windows input-idle after 2.16 seconds on a warm launch. It used 0.109 CPU seconds over the next ten wall-clock seconds, with a 128 MiB process working-set snapshot. This includes framework overhead and is not a cold-start or steady-state guarantee. Closed UI uses no resources; during play the separate tracker remains until the game ends.

## Reproduce

Build the app. Put eight JPEG covers named 0.jpg through 7.jpg in DATA/demo-art, then run:

```powershell
./Portable/Playdeck.exe --demo --data "C:/path/to/DATA" --capture "C:/path/to/run.bench.json"
```

Use a separate test data directory. The benchmark uses an in-memory synthetic library, never your actual library. Keep window dimensions, images, hardware and power configuration the same between builds.

The benchmark source is in src/Playdeck/Benchmark.cs. It intentionally retains the slowest row-boundary work in the reported distribution. v6's ordinary median scroll step is slightly higher than the baseline; the large improvement is refresh cost and the expensive scroll tail.

## Resource boundaries

The decoded-image cache holds at most 160 entries and a calculated 48 MiB of pixel data. Visible WPF image references and runtime/GPU resources can exist outside that cache. Only nearby cards are instantiated. The UI closes after a successful launch; the tracker exits after the tracked game ends. There is no resident tracker when idle.
