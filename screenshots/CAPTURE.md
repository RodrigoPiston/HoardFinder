# Screenshots for the mod page

Save these as PNG in this folder with exactly these names; the README links to them. 1920x1080 works well. Crop to
the interesting part if the rest of the screen is empty, but keep at least 1280 px of width. Hide the HUD clutter
you do not need (`Ctrl + F3` hides the HUD in vanilla).

| File | What to show |
|---|---|
| `window.png` | The search window open over your base with a query typed, for example `iron`, and several results. |
| `filters.png` | The window sorted by Mod with the Type filter on Materials, so modded items and their mod tags show. |
| `highlight.png` | A highlight active with a wall between you and the chests, so the markers show through it. |
| `multi.png` | `Shift + Enter` on a broad search (`ore` or `metal`) so several colors show at once. |

Optional extras: a non-English language (Settings, Misc, Language) and the in-game shortcut change.

Thunderstore only shows images from absolute URLs. Push this folder to a public repository and build the package
with `-p:ScreenshotBaseUrl=<url of this folder>` (see `docs/DEVELOPMENT.md`).
