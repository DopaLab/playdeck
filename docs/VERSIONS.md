# Local game release tracking

## Use it

1. Click a version chip. Your installed copy and the published release are shown side by side.
2. Enter the version shown inside your game. Alternatively expand Find my installed version, scan the bounded local locations and confirm a version file.
3. Confirm that the release source matches your PC edition and numbering, then Save & compare.

An existing cover match suggests a publisher feed; it does not prove the installation came from Steam. Release source lets you choose a different game feed without changing artwork, or enter the game's official GitHub repository. Advanced allows a manually researched latest-version note for unsupported publishers. Clear that note to resume online comparison.

## Independent evidence

Installed versions can come from any local installation: a menu/About-screen value, a confirmed version file or executable metadata. Executable values may describe an engine, so they need confirmation. Small nearby files and known Epic/Steam records can be suggested without recursively scanning drives. Missing files retain dated last-known evidence but do not establish a current installation.

Published versions come from Valve's official [ISteamNews API](https://partner.steamgames.com/doc/webapi/ISteamNews), restricted to publisher announcements, or the official [GitHub releases API](https://docs.github.com/en/rest/releases/releases) for a user-selected repository. Neither requires Steam to manage the game. The client sends only a game AppID or repository name, never installation paths or the library file. Opening release notes is an explicit user action.

Steam publisher feeds are not a universal latest-version endpoint. The parser uses explicit dotted release labels in recent titles, excludes preview/future/console/DLC titles and ambiguous multiple labels, and keeps the date and source link. A newer unnumbered update blocks a conclusive comparison. It reads at most 100 publisher posts or 30 GitHub releases; missing/older/out-of-window evidence stays uncertain. GitHub draft and prerelease entries are excluded. Different segment counts remain uncomparable rather than treating engine/build numbers as release numbers. Source confirmation is necessary because storefronts and editions can update differently. This cannot guarantee coverage for every local game.

Legacy Steam manifests still compare build IDs only against the same branch through the third-party [SteamCMD API](https://www.steamcmd.net/). Build IDs never participate in human release-label comparisons.

## Caching and resource use

Successful, empty and failed responses are persisted per release identity for at least 24 hours; longer Retry-After values are honored. Requests are serialized, paced and bounded to 2 MiB/12 seconds. Changing the installed version recalculates against cached release evidence. Offline checks preserve cached evidence. No network or parsing happens in card rendering.

Daily checks are opt-in, include manual installed versions, run only while the launcher is active and outside tracked play, and resume after focus returns. A five-minute UI timer checks whether work is due; it performs no network request for a fresh cache. Closing the launcher cancels pending work. No new background service is installed.

Yellow means a compatible newer numbered release was found from a confirmed source; it can remain yellow with a cached-evidence annotation. Green means a recent match to that source, not proof that every storefront is current. Grey asks for a source, an installed version, confirmation or a fresh check. Tiny chips retain their existing content-sized drawing and hit target.

## Validation and coverage

Read-only audit on 16 September 2026: 65 active games, 43 with numbered release evidence, 54 with readable installed evidence, zero provider errors. Eighteen had labels accepted by the general numeric comparator, before stricter shape/edition/source checks. These counts are evidence availability, not 43 or 18 verified comparisons. No real-library version settings were changed by the audit.

Regression tests exercise local manual-version and file-version lookups, no-Steam GitHub comparison, missing installed evidence, source identity changes, cache reuse/failure/backoff/offline behavior, numbering mismatches, previews, future posts, newer unnumbered updates, and tool exclusion. A live check uses a standalone local-game fixture, not a fabricated Steam manifest.
