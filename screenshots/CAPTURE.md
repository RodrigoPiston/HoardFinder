# Screenshots for the mod page

Save these as PNG in this folder with exactly these names; the README links to them. 1920x1080 works well. Crop to
the interesting part if the rest of the screen is empty, but keep at least 1280 px of width. Hide the HUD clutter
you do not need (`Ctrl + F3` hides the HUD in vanilla).

| File | What to show |
|---|---|
| `window.png` | The search window open over your base with a query typed, for example `iron`, and several results. |
| `filters.png` | The window sorted by Mod with the Mod filter on one mod, so the mod tags show. |
| `highlight.png` | A highlight active on a wall of drawers, with the markers showing amount and distance. |
| `storage.png` | A highlight that covers a chest and a drawer at once, with beams. |

Optional extras: a non-English language (Settings, Misc, Language) and the in-game shortcut change.

Thunderstore only shows images from absolute URLs. Push this folder to a public repository and build the package
with `-p:ScreenshotBaseUrl=<url of this folder>` (see `docs/DEVELOPMENT.md`).
