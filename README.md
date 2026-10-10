# MPH for Derail Valley

MPH is a quality-of-life mod that presents Derail Valley speed information in miles per hour.

This project is a Build 99.7 compatibility revision of the original **MPH** mod by **Zeibach**. Zeibach created the original mod and remains credited as its author. This revision updates its integration points for the current Simulator release while preserving the original intent.

## Build 99.7 support

Version 0.0.4 supports Derail Valley Simulator Build 99.7.

- Converts static and generated speed-limit signs from their tens-of-km/h values to rounded mph.
- Recalibrates locomotive speedometer needles to read against the dial scale in mph.
- Converts the shop Digital Speedometer and digital speed indicators on Custom Car Loader 3.x and other modded locomotives.
- Supports the standard locomotives and Custom Car Loader 3.x locomotives that expose a `LocoIndicatorReader` speed gauge.
- Supports normal, yellow, and legacy speed-limit sign variants.

For example, a sign showing `6` (60 km/h) is displayed as `35` mph.

## Important note about cab gauges

The number markings and `km/h` label on locomotive speedometer faces are baked into the cab artwork. This mod does not replace those textures. Instead, it recalibrates the needle so that the numbered scale is read as mph—the behavior of the original MPH mod.

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) for Derail Valley.
2. Create or open the `Mods\Mph` folder in your Derail Valley installation.
3. Copy these files from the Release build output into that folder:
   - `Mph.dll`
   - `info.json`
4. Fully restart Derail Valley.

The release output is located at `bin\Release\netstandard2.0` after building the project.
The Release build also creates `Revised_Mph-<Version>.zip`, using the `Version`
from `info.json`; the archive contains the installable DLL and `info.json`.

## Changelog

### 0.0.4

- Fixed lag spikes caused by a full-world sign scan running on every scene load while moving. Text conversion is now time-budgeted across frames to keep it smooth.

### 0.0.3

- Fixed speed-limit sign conversion by interpreting each sign value as tens of km/h before converting to rounded mph. This prevents lower speed limits from incorrectly displaying as 0 mph.

### 0.0.2

- Added compatibility with Derail Valley Simulator Build 99.7.
- Added conversion for static and generated speed-limit signs, locomotive speedometers, and supported digital speed displays.
- Added support for standard locomotives, Custom Car Loader 3.x locomotives, and normal, yellow, and legacy speed-limit sign variants.

## Building from source

The project targets .NET Standard 2.0 and compiles against the local Derail Valley installation. By default, it uses:

```text
D:\Games\Derail Valley
```

If your game is installed elsewhere, override `DerailValleyInstallPath` when building:

```powershell
dotnet build --configuration Release -p:DerailValleyInstallPath="C:\Path\To\Derail Valley"
```

## Credits

- **Zeibach** — original MPH mod author and concept.
- Build 99.7 compatibility revision — this project revision.

Original mod page: [MPH on Nexus Mods](https://www.nexusmods.com/derailvalley/mods/401)

## License

See [LICENSE](LICENSE).
