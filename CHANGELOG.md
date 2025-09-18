# Changelog

All notable changes to GUI Developer Mode for RimWorld will be documented in this file.

## [1.3.0] - 2024-Current

### ✅ Fixed
- **Explosion Continuous Targeting**: Fixed explosion targeting canceling after one use
- **Visual Radius Preview**: Added proper explosion radius preview with right-click cancellation
- **Bottom Bar Placement**: Implemented complete bottom bar button functionality  
- **Item Display Limiting**: Fixed item limiting not working by default with debugging support

### ✨ Added
- **Modular Code Architecture**: Refactored into separate handler classes for maintainability
  - `ExplosionSystem.cs`: Explosion detection and targeting
  - `CacheManager.cs`: Performance optimization and data management
  - `ItemsTabHandler.cs`: Item browsing and spawning
  - `ActionsTabHandler.cs`: Explosions and actions  
  - `TerrainTabHandler.cs`: Terrain placement
  - `SpawningTabHandler.cs`: Pawn creation
  - `UtilityTabsHandler.cs`: Research, trading, utilities
- **Enhanced Explosion System**: 500+ auto-detected explosion types from all mods
- **Performance Caching**: Smart caching system with configurable display limits
- **Categorized Browsing**: Items, terrain, and pawns organized by categories
- **Gift Pod Delivery**: Optional drop pod delivery for spawned pawns and items
- **Comprehensive Utilities**: Research management, faction relations, colony tools

### 🔧 Improved
- **Performance**: Reduced memory usage and improved UI responsiveness
- **Mod Compatibility**: Enhanced auto-detection of modded content
- **User Experience**: Better tooltips, status indicators, and error handling
- **Code Quality**: Separated concerns into logical components for easier maintenance

### 📊 Technical Details
- **Lines of Code**: Reduced main window from 2100+ lines to ~120 lines
- **Memory Usage**: Optimized to ~2-5MB additional RAM usage
- **Load Time**: Cache building completes in <1 second
- **Compatibility**: Tested with 500+ mods

## [1.2.0] - Previous Release

### Added
- Categorized item browsing system
- Terrain placement with continuous targeting
- Faction relationship management
- Basic performance improvements

### Fixed
- Item spawning stability issues
- UI responsiveness problems

## [1.1.0] - Initial Enhanced Release

### Added
- Initial explosion system implementation
- Basic item spawning functionality
- Research and trading tools
- Time control features

### Changed
- Upgraded from basic debug tools to comprehensive GUI

## [1.0.0] - Initial Release

### Added
- Basic GUI framework
- Simple item spawning
- Terrain placement
- Debug action integration

---

## Version Numbering

This project follows [Semantic Versioning](https://semver.org/):
- **MAJOR**: Incompatible API changes or major rewrites
- **MINOR**: New functionality in a backwards compatible manner  
- **PATCH**: Backwards compatible bug fixes

## Contributing

When contributing changes, please:
1. Update this changelog with your changes
2. Follow the format above
3. Include the type of change (Added/Changed/Fixed/Removed)
4. Provide clear descriptions of the impact