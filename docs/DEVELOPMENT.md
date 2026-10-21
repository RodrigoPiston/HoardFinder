# Development

## Building

```bash
dotnet build -c Release
```

The project builds against the local game in `~/.local/share/Steam/steamapps/common/Valheim` and BepInEx from the
r2modman `Default` profile. Override with `-p:ValheimDir=...` and `-p:BepInExDir=...` (or `VALHEIM_DIR` /
`BEPINEX_DIR`). Output: `bin/Release/HoardFinder.dll`.

- `-p:Deploy=true` copies the DLL into `BepInEx/plugins/HoardFinder` of that profile.
- `-t:Package` builds the Thunderstore zip, see below.

## Thunderstore package

```bash
dotnet build -c Release -t:Package -p:ScreenshotBaseUrl=https://raw.githubusercontent.com/<user>/<repo>/main/screenshots
```

Result: `bin/thunderstore/HoardFinder-<version>.zip`, laid out as Thunderstore expects:

```
manifest.json
icon.png        256x256
README.md       this repository's README, screenshot links made absolute
CHANGELOG.md
plugins/HoardFinder.dll
```

Thunderstore only renders images from absolute URLs, so `ScreenshotBaseUrl` must point at a public copy of the
`screenshots/` folder (a GitHub repository is the usual choice). Without it the package still builds, with a
warning, and the screenshots will not show on the mod page. `screenshots/CAPTURE.md` lists the four images the
README expects.

Before uploading:

1. Bump `<Version>` in `HoardFinder.csproj`, `Version` in `Plugin.cs` and `version_number` in
   `thunderstore/manifest.json`. The package target refuses to build when the csproj and manifest disagree.
2. Add the release notes to `CHANGELOG.md`.
3. Set `website_url` in `thunderstore/manifest.json` to the project page (it may stay empty).
4. Upload the zip at <https://thunderstore.io/c/valheim/create/> under your team.

## Layout

| Folder | Contents |
|---|---|
| `Storage/` | Storage sources (containers, drawers), access checks, the combined index |
| `Drawers/` | BetrValheim integration by reflection |
| `Search/` | Item names, categories, mod lookup, grouping, filtering and sorting |
| `Highlighting/` | Glow, beams and screen markers |
| `UI/` | IMGUI window, Valheim skin, toolbar, animations, toast, hotkey capture |
| `I18n/` | One `Language.txt` per Valheim language, embedded in the DLL |
| `Patches/` | Input lock while the window is open, container registry |
| `Config/` | BepInEx config and the BetrDrawerFinder settings import |

## How it reads storage

- **Containers.** A `Container.Awake` postfix registers every container as it loads, so there is no scene scan.
  `Container` reloads its inventory from the replicated ZDO every second on every client, so `GetInventory()` is
  current without opening it. Carts and ships are detected through `m_rootObjectOverride`, `Ship` and `Vagon`;
  tombstones through `TombStone`.
- **BetrValheim drawers.** The public `BetrValheim.Drawers.Drawer` type exposes `ItemName` and `Count`, and every
  loaded drawer is in the internal `DrawerIndex.All` list. Both are resolved once and the getters are compiled to
  delegates. Fallbacks, logged once: a throttled `FindObjectsByType` if the registry is gone, the ZDO keys
  `ew_drawer_item` / `ew_drawer_count` if the properties are gone.
- **Access.** Ward permission through `PrivateArea.CheckAccess`, and the container privacy rule vanilla uses
  (public, or private and built by the local player).
- **Mods.** Jotunn `ModQuery`, enabled by reflection in `Awake`, answers which plugin added each prefab.

Storage is read in a snapshot rebuilt at most once per second, when the player moves more than a meter, or when the
source filter changes. Typing, sorting and the type and mod filters only re-rank the cached groups.

More detail, including the decompiled APIs, in [FINDINGS.md](FINDINGS.md).
