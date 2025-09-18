# 🛠️ Build Instructions

## Quick Build

### Option 1: Use the Build Script (Recommended)
```powershell
# Just build
.\build.ps1

# Build and deploy to RimWorld automatically
.\build.ps1 -Deploy

# Build and deploy to specific RimWorld path
.\build.ps1 -Deploy -RimWorldPath "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"
```

### Option 2: Manual Build
```powershell
# Navigate to project root
cd "d:\path\to\GUIDevMode"

# Restore packages and build
dotnet restore .\Source\GUIDevMode\GUIDevMode.csproj
dotnet build .\Source\GUIDevMode\GUIDevMode.csproj -c Release
```

## Output Location
The compiled DLL will be created at:
- `Source\Assemblies\GUIDevMode.dll`

## Manual Installation
1. Copy the entire `GUIDevMode` folder to your RimWorld `Mods` directory:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\`
   - GOG: `C:\GOG Games\RimWorld\Mods\`

2. The final structure should be:
   ```
   RimWorld\Mods\GUIDevMode\
   ├── About\
   │   ├── About.xml
   │   ├── LoadFolders.xml
   │   └── Manifest.xml
   ├── Assemblies\
   │   └── GUIDevMode.dll
   └── Defs\
       └── KeyBindings.xml
   ```

3. Enable the mod in RimWorld's mod manager

## Development Requirements
- .NET Framework 4.7.2 Developer Pack
- Visual Studio 2019/2022 OR MSBuild Tools
- RimWorld installation (for Assembly-CSharp.dll reference)

## Troubleshooting
- **Build errors**: Ensure .NET Framework 4.7.2 is installed
- **Reference errors**: Set `RIMWORLD_PATH` environment variable
- **Permission errors**: Run PowerShell as administrator for deployment

## Testing
1. Launch RimWorld
2. Enable "GUI Developer Mode" in the mod list
3. Start a game and press F9 to open the GUI Developer Mode window
4. Test item spawning, explosions, and other features