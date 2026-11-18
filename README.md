# RankHud

A minimal, lightweight HUD overlay for **Schedule I** (Steam, Unity/IL2CPP, MelonLoader).

Top-right corner of the screen shows:

```
              Wed 2:32 PM
    Hustler 2, 66% to Hustler 3
```

- **Clock** — in-game time, day of week, 12-hour format with AM/PM
- **Rank progress** — current rank + level and exact % toward the next level (5 levels per rank, Street Rat → Kingpin)

No keybinds, no config file, no menus. Install and forget.

## Installation

1. Install [MelonLoader](https://melon.wiki) (IL2CPP build) if you don't have it.
2. Extract into your Schedule I folder (the one containing `Schedule I.exe`):

```
Schedule I/
├── Mods/
│   └── RankHud.dll      <- this mod
├── Schedule I.exe
└── ...
```

3. Launch the game. The overlay appears once you load into a save (it hides itself on the main menu).

To uninstall, delete `Mods/RankHud.dll`.

## Building from source

The mod is a single C# file compiled against the game's MelonLoader-generated interop assemblies.

Requirements:
- The game's `MelonLoader/Il2CppAssemblies/` (generated on first MelonLoader launch)
- `MelonLoader/net6/MelonLoader.dll`
- A C# compiler (any Roslyn-based `csc`, C# 12 works)

Compile (response file with `/r:` references to every DLL in `MelonLoader/Il2CppAssemblies/`, `MelonLoader/net6/`, and a .NET runtime shared framework, plus `/nostdlib+`):

```
csc /noconfig /nostdlib+ /target:library /out:RankHud.dll RankHud.cs @refs.rsp
```

Drop the resulting `RankHud.dll` into `Mods/`.

## How it works

- `OnUpdate()` — resolves `LevelManager` and `TimeManager` once (cached), reads:
  - `TimeManager.NormalizedTimeOfDay` (0..1, midnight..midnight) → clock
  - `LevelManager.GetFullRank()` → `ERank` + tier → rank line
  - `LevelManager.XP` / `XPToNextTier` → progress %
- `OnGUI()` — draws two right-aligned labels via Unity IMGUI. No canvas/TMP injection, so it survives game updates well.

The level number auto-corrects if the game ever reports 0-based tiers, and all data reads are wrapped in try/catch with bounded logging (`RankHud: ...` lines in `MelonLoader/Latest.log`) so failures are diagnosable without log spam.

## Compatibility

- Tested on Schedule I `0.4.7f12`
- Read-only — never writes to saves
- Coexists with other MelonLoader mods

## License

MIT
