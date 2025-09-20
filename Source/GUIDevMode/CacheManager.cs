using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public static class CacheManager
    {
        // Main cache dictionaries
        private static Dictionary<string, List<ThingDef>> cachedItemsByCategory = new Dictionary<string, List<ThingDef>>();
        private static Dictionary<string, List<ThingDef>> cachedBuildingsByCategory = new Dictionary<string, List<ThingDef>>();
        private static Dictionary<string, List<TerrainDef>> cachedTerrainByCategory = new Dictionary<string, List<TerrainDef>>();
        private static Dictionary<string, List<PawnKindDef>> cachedPawnsByCategory = new Dictionary<string, List<PawnKindDef>>();
        
        // Mod-based caches
        private static Dictionary<string, Dictionary<string, List<ThingDef>>> cachedItemsByMod = new Dictionary<string, Dictionary<string, List<ThingDef>>>();
        private static Dictionary<string, List<ThingDef>> cachedItemsByModFlat = new Dictionary<string, List<ThingDef>>();
        
        // Category lists
        private static List<string> allItemCategories = new List<string>();
        private static List<string> allBuildingCategories = new List<string>();
        private static List<string> allTerrainCategories = new List<string>();
        private static List<string> allPawnCategories = new List<string>();
        private static List<string> allModNames = new List<string>();
        
        // Cache expiry tracking
        private static int lastCacheUpdateTick = -1;
        private static int cacheExpiryInterval = 60000; // 1 in-game day
        
        public static List<string> AllItemCategories => allItemCategories;
        public static List<string> AllBuildingCategories => allBuildingCategories;
        public static List<string> AllTerrainCategories => allTerrainCategories;
        public static List<string> AllPawnCategories => allPawnCategories;
        public static List<string> AllModNames => allModNames;
        
        public static void RefreshAllCaches()
        {
            if (Find.TickManager?.TicksGame == lastCacheUpdateTick) return;
            
            Log.Message("[GUI Dev Mode] Refreshing all caches...");
            
            RefreshItemCache();
            RefreshModBasedItemCache();
            RefreshBuildingCache();
            RefreshTerrainCache();
            RefreshPawnCache();
            
            lastCacheUpdateTick = Find.TickManager?.TicksGame ?? 0;
            Log.Message($"[GUI Dev Mode] Cache refresh complete. Found {allItemCategories.Count} item categories, {allModNames.Count} mods with items");
        }
        
        public static bool IsCacheExpired()
        {
            return Find.TickManager?.TicksGame > lastCacheUpdateTick + cacheExpiryInterval;
        }
        
        private static void RefreshItemCache()
        {
            cachedItemsByCategory.Clear();
            allItemCategories.Clear();
            
            var allItems = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.category == ThingCategory.Item && def.BaseMarketValue > 0)
                .OrderBy(def => def.label);
            
            foreach (var item in allItems)
            {
                var category = GetItemCategory(item);
                if (!cachedItemsByCategory.ContainsKey(category))
                {
                    cachedItemsByCategory[category] = new List<ThingDef>();
                    allItemCategories.Add(category);
                }
                cachedItemsByCategory[category].Add(item);
            }
            
            // Sort categories
            allItemCategories.Sort();
        }
        
        private static void RefreshModBasedItemCache()
        {
            cachedItemsByMod.Clear();
            cachedItemsByModFlat.Clear();
            allModNames.Clear();
            
            var allItems = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.category == ThingCategory.Item && def.BaseMarketValue > 0)
                .OrderBy(def => def.label);
            
            foreach (var item in allItems)
            {
                var modName = GetModName(item);
                var category = GetItemCategory(item);
                
                // Organize by mod first, then by category within mod
                if (!cachedItemsByMod.ContainsKey(modName))
                {
                    cachedItemsByMod[modName] = new Dictionary<string, List<ThingDef>>();
                    cachedItemsByModFlat[modName] = new List<ThingDef>();
                    allModNames.Add(modName);
                }
                
                if (!cachedItemsByMod[modName].ContainsKey(category))
                {
                    cachedItemsByMod[modName][category] = new List<ThingDef>();
                }
                
                cachedItemsByMod[modName][category].Add(item);
                cachedItemsByModFlat[modName].Add(item);
            }
            
            allModNames.Sort();
        }
        
        private static void RefreshBuildingCache()
        {
            cachedBuildingsByCategory.Clear();
            allBuildingCategories.Clear();
            
            var allBuildings = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.category == ThingCategory.Building && def.BuildableByPlayer)
                .OrderBy(def => def.label);
            
            foreach (var building in allBuildings)
            {
                var category = GetBuildingCategory(building);
                if (!cachedBuildingsByCategory.ContainsKey(category))
                {
                    cachedBuildingsByCategory[category] = new List<ThingDef>();
                    allBuildingCategories.Add(category);
                }
                cachedBuildingsByCategory[category].Add(building);
            }
            
            allBuildingCategories.Sort();
        }
        
        private static void RefreshTerrainCache()
        {
            cachedTerrainByCategory.Clear();
            allTerrainCategories.Clear();
            
            var allTerrain = DefDatabase<TerrainDef>.AllDefs
                .Where(def => def.designationCategory != null || def.BuildableByPlayer)
                .OrderBy(def => def.label);
            
            foreach (var terrain in allTerrain)
            {
                var category = GetTerrainCategory(terrain);
                if (!cachedTerrainByCategory.ContainsKey(category))
                {
                    cachedTerrainByCategory[category] = new List<TerrainDef>();
                    allTerrainCategories.Add(category);
                }
                cachedTerrainByCategory[category].Add(terrain);
            }
            
            allTerrainCategories.Sort();
        }
        
        private static void RefreshPawnCache()
        {
            cachedPawnsByCategory.Clear();
            allPawnCategories.Clear();
            
            var allPawns = DefDatabase<PawnKindDef>.AllDefs
                .Where(def => def.race != null)
                .OrderBy(def => def.label);
            
            foreach (var pawn in allPawns)
            {
                var category = GetPawnCategory(pawn);
                if (!cachedPawnsByCategory.ContainsKey(category))
                {
                    cachedPawnsByCategory[category] = new List<PawnKindDef>();
                    allPawnCategories.Add(category);
                }
                cachedPawnsByCategory[category].Add(pawn);
            }
            
            allPawnCategories.Sort();
        }
        
        public static List<ThingDef> GetItemsByCategory(string category)
        {
            if (IsCacheExpired()) RefreshAllCaches();

            if (!cachedItemsByCategory.ContainsKey(category))
                return new List<ThingDef>();
                
            var allItems = cachedItemsByCategory[category];
            var settings = GUIDevModeMod.Settings;
            
            if (settings.limitItemDisplay && allItems.Count > settings.itemDisplayLimit)
            {
                return allItems.Take(settings.itemDisplayLimit).ToList();
            }
            
            return allItems;
        }
        
        public static List<ThingDef> GetFullItemsByCategory(string category)
        {
            if (IsCacheExpired()) RefreshAllCaches();
            
            return cachedItemsByCategory.ContainsKey(category) 
                ? cachedItemsByCategory[category] 
                : new List<ThingDef>();
        }
        
        public static List<ThingDef> GetBuildingsByCategory(string category)
        {
            if (IsCacheExpired()) RefreshAllCaches();

            if (!cachedBuildingsByCategory.ContainsKey(category))
                return new List<ThingDef>();
                
            var allBuildings = cachedBuildingsByCategory[category];
            var settings = GUIDevModeMod.Settings;
            
            if (settings.limitItemDisplay && allBuildings.Count > settings.itemDisplayLimit)
            {
                return allBuildings.Take(settings.itemDisplayLimit).ToList();
            }
            
            return allBuildings;
        }
        
        public static List<TerrainDef> GetTerrainByCategory(string category)
        {
            if (IsCacheExpired()) RefreshAllCaches();

            if (!cachedTerrainByCategory.ContainsKey(category))
                return new List<TerrainDef>();
                
            var allTerrain = cachedTerrainByCategory[category];
            var settings = GUIDevModeMod.Settings;
            
            if (settings.limitItemDisplay && allTerrain.Count > settings.itemDisplayLimit)
            {
                return allTerrain.Take(settings.itemDisplayLimit).ToList();
            }
            
            return allTerrain;
        }
        
        public static List<PawnKindDef> GetPawnsByCategory(string category)
        {
            if (IsCacheExpired()) RefreshAllCaches();

            if (!cachedPawnsByCategory.ContainsKey(category))
                return new List<PawnKindDef>();
                
            var allPawns = cachedPawnsByCategory[category];
            var settings = GUIDevModeMod.Settings;
            
            if (settings.limitItemDisplay && allPawns.Count > settings.itemDisplayLimit)
            {
                return allPawns.Take(settings.itemDisplayLimit).ToList();
            }
            
            return allPawns;
        }
        
        private static string GetItemCategory(ThingDef item)
        {
            if (item.IsWeapon) return "Weapons";
            if (item.IsApparel) return "Apparel";
            if (item.IsIngestible) return "Food & Drugs";
            if (item.IsMedicine) return "Medicine";
            if (item.building != null) return "Building Materials";
            if (item.stuffProps != null) return "Materials";
            if (item.techLevel == TechLevel.Spacer) return "Spacer Tech";
            if (item.techLevel == TechLevel.Industrial) return "Industrial";
            if (item.techLevel == TechLevel.Medieval) return "Medieval";
            if (item.techLevel == TechLevel.Neolithic) return "Neolithic";
            if (item.IsArt) return "Art";
            if (item.comps?.Any(c => c is CompProperties_Power) == true) return "Electronics";
            return "Miscellaneous";
        }
        
        private static string GetModName(Def def)
        {
            if (def?.modContentPack?.PackageId != null)
            {
                var modName = def.modContentPack.Name;
                if (string.IsNullOrEmpty(modName))
                    modName = def.modContentPack.PackageId;
                return modName;
            }
            return "Core";
        }
        
        // Mod-based item retrieval methods
        public static Dictionary<string, List<ThingDef>> GetItemCategoriesForMod(string modName)
        {
            return cachedItemsByMod.ContainsKey(modName) ? cachedItemsByMod[modName] : new Dictionary<string, List<ThingDef>>();
        }
        
        public static List<ThingDef> GetItemsFromModCategory(string modName, string category)
        {
            if (cachedItemsByMod.ContainsKey(modName) && cachedItemsByMod[modName].ContainsKey(category))
                return cachedItemsByMod[modName][category];
            return new List<ThingDef>();
        }
        
        public static List<ThingDef> GetAllItemsFromMod(string modName)
        {
            return cachedItemsByModFlat.ContainsKey(modName) ? cachedItemsByModFlat[modName] : new List<ThingDef>();
        }
        
        private static string GetBuildingCategory(ThingDef building)
        {
            if (building.designationCategory != null)
                return building.designationCategory.label;
            if (building.building?.isNaturalRock == true) return "Natural";
            if (building.building?.isResourceRock == true) return "Resources";
            return "Uncategorized";
        }
        
        private static string GetTerrainCategory(TerrainDef terrain)
        {
            if (terrain.designationCategory != null)
                return terrain.designationCategory.label;
            if (terrain.natural) return "Natural";
            if (terrain.BuildableByPlayer) return "Constructed";
            return "Special";
        }
        
        private static string GetPawnCategory(PawnKindDef pawn)
        {
            if (pawn.RaceProps.Humanlike) return "Humans";
            if (pawn.RaceProps.Animal) return "Animals";
            if (pawn.RaceProps.IsMechanoid) return "Mechanoids";
            if (pawn.RaceProps.FleshType == FleshTypeDefOf.Insectoid) return "Insects";
            return "Other";
        }
        
        public static void ClearCache()
        {
            cachedItemsByCategory.Clear();
            cachedBuildingsByCategory.Clear();
            cachedTerrainByCategory.Clear();
            cachedPawnsByCategory.Clear();
            allItemCategories.Clear();
            allBuildingCategories.Clear();
            allTerrainCategories.Clear();
            allPawnCategories.Clear();
            lastCacheUpdateTick = -1;
            Log.Message("[GUI Dev Mode] All caches cleared");
        }
    }
}