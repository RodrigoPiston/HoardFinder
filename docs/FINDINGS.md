# BetrValheim integration findings

Inspected: `BetrValheim.dll` 5.1.5 (DezzyCode, r2modman profile `Default`), cross-checked against 4.47.1,
`assembly_valheim.dll` (current Steam build), `Jotunn.dll` 2.30, BepInEx 5.4.23.

## Drawer class

`BetrValheim.Drawers.Drawer : MonoBehaviour, Hoverable, Interactable` — **public**, and the only public type
in the assembly. Added with `AddComponent<Drawer>()` to every drawer piece prefab (`EW_Drawer`, `DV_Drawer_*`)
in `DrawerPiece.Register()`.

## Stored item

`public string ItemName { get; }` → reads `ZDO.GetString("ew_drawer_item")`. Value is the item **prefab name**
(e.g. `IronScrap`), or `null` when the drawer is unassigned. Localized name comes from
`ObjectDB.instance.GetItemPrefab(name).GetComponent<ItemDrop>().m_itemData.m_shared.m_name` →
`Localization.instance.Localize(...)`, exactly as `Drawer.GetHoverText()` does.

## Stored quantity

`public int Count { get; }` → `ZDO.GetInt("ew_drawer_count")`, 0 when the ZDO is invalid.

Both ZDO keys are private constants, identical in 4.47.1 and 5.1.5.

## World position

`drawer.transform.position` (the component sits on the piece root, the same object as its `ZNetView`).
Renderers are children of that root (`DrawerModel.Build`).

## Registry

`internal static class BetrValheim.Drawers.DrawerIndex`:

- `public static List<Drawer> All` — every drawer instance currently loaded; `Drawer.Awake` adds,
  `Drawer.OnDestroy` removes. Destroyed entries are pruned lazily, so consumers must null-check.
- `CollectNearby` / `Enumerate` / `Withdraw` — internal helpers, `Withdraw` mutates.

The class is internal, so access needs reflection (resolved once, a single static property getter).

## Client/server behavior

- Drawer data lives entirely in the piece ZDO. ZDOs of loaded zones are replicated to every client, so a client
  can read item and count of every drawer around it without RPC.
- The `Drawer` component exists on clients for every instantiated piece (ZNetScene instantiates it).
- Writes (`Deposit`, `Withdraw`, `Release`) claim ZDO ownership. This mod never calls them.
- BetrValheim declares `NetworkCompatibility(EveryoneMustHaveMod)`. A plugin without the Jotunn attribute is
  treated by Jotunn as `NotEnforced` (`Jotunn.Utils.ModModule` ctor), so a client-only plugin does not trip the
  version check.

## Public API for other mods

None besides the `Drawer` component itself. No ItemDrawers-style API. The Innangard BetrValheim Drawer Bridge is
not installed locally and its source is not published; its Thunderstore description says it uses "the actual
drawer API and capacity checks", which matches the public `Drawer` members.

## Valheim hooks used

- `TextInput.IsVisible()` (public static) gates `PlayerController.TakeInput`, `Player.TakeInput`, chat opening,
  the Escape menu, minimap input and mouse capture. Forcing it true while the search window is open is the same
  state Valheim uses for sign text entry.
- `MaterialMan.instance.SetValue/ResetValue(go, ShaderProps._EmissionColor, ...)` is Valheim's own
  property-block highlight (used by `WearNTear.Highlight`). No material is cloned or mutated.

## Recommended integration strategy

1. Find BetrValheim through `Chainloader.PluginInfos["dezzycode.valheim.betrvalheim"]` (soft dependency).
2. Resolve `Drawer` type, `DrawerIndex.All` getter, `Drawer.ItemName` and `Drawer.Count` getters once and compile
   them to delegates.
3. Read-only: iterate the registry at search time, filter by radius, read item and count.
4. Fallbacks, each logged once: no `DrawerIndex` → throttled `FindObjectsOfType(Drawer)` (at most every 5 s,
   only when searching); no `ItemName`/`Count` properties → read the two ZDO keys directly.
5. Client-side only.

## Containers (added in 1.3)

- `Container.Awake` creates the inventory only when the ZNetView has a ZDO (no ghosts), and starts
  `InvokeRepeating("CheckForChanges", 0, 1)`. `Load()` reloads the inventory from `ZDOVars.s_items` whenever the ZDO
  data revision changes, on every client. `GetInventory()` is therefore current client-side without opening.
- Access: `Container.CheckAccess` is private; its logic is Public = yes, Private = `Piece.GetCreator()` equals the
  local player id, Group = no. Wards: `PrivateArea.CheckAccess(point, radius, flash, wardCheck)`.
- Carts and ships set `m_rootObjectOverride` to the vehicle's ZNetView. Tombstones carry a `TombStone` component.
- BetrValheim drawers strip their `Container` (`DrawerPiece.Strip`), so they are never counted twice.

## Valheim UI sprites

The game UI uses a `SpriteAtlas` named `UIAtlas` (loaded from the `SoftRef` bundles). Jotunn's Valheim-style
panels use `woodpanel_settings`, `woodpanel_trophys`, `text_field`, `button` and `button_highlight` from it.

## Item source mod

Jotunn `ModQuery.Enable()` (before `FejdStartup.Awake`) records which plugin added each prefab, including non-Jotunn
mods; `ModQuery.GetPrefab(name).SourceMod` returns the `BepInPlugin`. Vanilla prefabs return null.
