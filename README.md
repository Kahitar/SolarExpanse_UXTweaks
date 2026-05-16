# UXTweaks

A small quality-of-life mod for Solar Expanse.

## Features

- **Less camera jumping:** clicking a planet, moon, asteroid, or mission opens its info without suddenly zooming all the way in and locking the camera to it.
- **Double-click to focus:** double-click the same body or mission when you do want the old zoom-in-and-follow behavior.
- **Easier body info panels:** long Facilities, Resources, Rockets, Launch Vehicles, and Missions lists are shown in the main Object Info panel instead of tiny nested scroll boxes.

## Installation

1. Install BepInEx for Solar Expanse first. Use the official BepInEx installation guide:
   <https://docs.bepinex.dev/articles/user_guide/installation/index.html>
2. Download the latest `UXTweaks_v*.zip` from the UXTweaks releases page:
   <https://github.com/Kahitar/SolarExpanse_UXTweaks/releases/latest>
3. Open your Solar Expanse game folder.
4. Extract the zip into the game folder so `UXTweaks.dll` ends up here:

```text
Solar Expanse/BepInEx/plugins/UXTweaks.dll
```

5. Restart Solar Expanse.

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
