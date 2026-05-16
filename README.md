# UXTweaks

A BepInEx/Harmony mod for Solar Expanse focused on UI and camera quality-of-life tweaks.

## Features

- Expands Object Info window sub-lists so Facilities, Resources, Explored Resources, Rockets, Launch Vehicles, and Missions use the outer Object Info scrollbar instead of nested two-row scrollbars.
- Stops normal body clicks from forcing the map camera to zoom to and follow the clicked body while still opening the Object Info window.

## Build

Run from this directory:

```bash
mise run build
```

The build copies `UXTweaks.dll` into `$SOLAR_EXPANSE_ROOT/BepInEx/plugins`.

## Package

```bash
mise run package
```

This creates `dist/UXTweaks_v*.zip`.
