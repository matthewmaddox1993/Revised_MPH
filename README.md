# MPH for Derail Valley

MPH is a quality-of-life mod that presents Derail Valley speed information in miles per hour.

This project is a Build 99.7 compatibility revision of the original **MPH** mod by **Zeibach**. Zeibach created the original mod and remains credited as its author. This revision updates its integration points for the current Simulator release while preserving the original intent.

## Build 99.7 support

Version 0.0.1 supports Derail Valley Simulator Build 99.7.

- Converts static and generated speed-limit signs from km/h to rounded mph.
- Recalibrates locomotive speedometer needles to read against the dial scale in mph.
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
