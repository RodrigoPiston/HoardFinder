# Changelog

## 1.4.0

- Renamed from BetrDrawerFinder to HoardFinder, since it now searches all storage and not only BetrValheim drawers.
  New plugin GUID `heido.valheim.hoardfinder`; settings from the old config file are imported on first start.
  HoardFinder refuses to load next to the old BetrDrawerFinder so the two never run together.
- Console commands renamed to `hoard_search`, `hoard_dump` and `hoard_clear`.
- First Thunderstore release: manifest, icon, user README and packaging target.
- Fixed: choosing a result with Enter or a click no longer opens chat or makes the character attack afterwards. The
  input lock now lasts until those keys are released and then clears Valheim's button states.
- Fixed: text was hard to read on the light wood panel. The window now uses the dark panel and a dark backdrop
  behind the text.

## 1.3.0

- Searches every container, not only BetrValheim drawers: chests (vanilla and modded), carts, ships, tombstones and,
  optionally, dungeon loot. BetrValheim is now optional. Containers are registered as they load (Container.Awake
  postfix), no scene scans.
- Respects ward permission and container privacy by default (`RespectStorageAccess`).
- Sort by relevance, name, amount, distance, type or mod; filter by item type, storage source and mod. Sort, type
  and source are remembered.
- Mod detection through Jotunn ModQuery (optional), shown as a tag on modded items.
- Valheim look: the window, search field and buttons use the game's own UI sprites (wood panel, text field,
  buttons), 9-sliced, with Valheim's orange and cream palette. Falls back to the flat style if a sprite is missing.
- Highlights work on any storage; carts and ships get a marker at the container instead of a full-vehicle glow.
- 29 new or updated strings in all 37 languages.

## 1.2.0

- UI translated into every Valheim language (36 plus English); follows the game's language setting live. Averia
  typeface for Latin-1 languages, Unity's default font with OS fallback for the rest.
- In-game rebinding of the open shortcut from the search window (Esc cancels, Backspace restores the default).
- Animations: window open/close transition, staggered rows, gliding selection bar, chosen-row flash, confirmation
  banner, marker pop-in and growing beams.
- Highlight collection no longer reuses the search snapshot, so the close animation never shows recycled rows.

## 1.1.0

- New window look: Valheim typeface, item icons, column headers, selection accent, hover, thousands separators,
  scrollbar, placeholder and empty states.
- Scrolling with mouse wheel and PageUp/PageDown; the selection always stays visible.
- `Shift + Enter` / `Shift + click` highlights every listed item, one color per item.
- Markers show the amount stored in each drawer; only the 8 nearest are labeled, the nearest is larger, and markers
  fade out at the end.
- The hotkey also closes the window; reopening keeps the last query, pre-selected.
- Accent-insensitive search.
- `ClearHighlightHotkey` setting and `bdf_clear` command.
- Camera zoom is blocked while the window is open.
- Performance: drawer snapshot cached for 1 s (typing only re-ranks), pre-formatted row strings, single-pass drawing
  without GUILayout, marker sorting and pruning at 10 Hz.

## 1.0.0

- Search window (`Ctrl + F`) listing items stored in BetrValheim drawers within `SearchRadius`, with total amount,
  drawer count and distance to the closest drawer.
- Case-insensitive, space-insensitive partial matching on localized names, prefab name as fallback.
- Highlight of matching drawers for `HighlightDuration` seconds: glow via MaterialMan, light beam, and screen-space
  markers visible through walls.
- `bdf_dump` and `bdf_search` console commands.
- Read-only, client-side integration through `DrawerIndex.All` and the public `Drawer` members, with logged
  fallbacks.
