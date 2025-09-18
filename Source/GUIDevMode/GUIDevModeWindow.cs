using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace GUIDevMode
{
    public enum DevToolCategory
    {
        Items,
        Terrain,
        Actions,
        Spawning,
        Factions,
        Incidents,
        Utilities,
        Research,
        Trading
    }
    
    public class GUIDevModeWindow : Window
    {
        public override Vector2 InitialSize => new Vector2(1000f, 700f);
        
        private DevToolCategory currentCategory = DevToolCategory.Items;
        
        // Tab handlers
        private ItemsTabHandler itemsTabHandler;
        private ActionsTabHandler actionsTabHandler;
        private TerrainTabHandler terrainTabHandler;
        private SpawningTabHandler spawningTabHandler;
        private UtilityTabsHandler utilityTabsHandler;
        
        public GUIDevModeWindow()
        {
            doCloseButton = true;
            doCloseX = true;
            resizeable = true;
            draggable = true;
            forcePause = false;
            absorbInputAroundWindow = false;
            
            // Initialize tab handlers
            itemsTabHandler = new ItemsTabHandler();
            actionsTabHandler = new ActionsTabHandler();
            terrainTabHandler = new TerrainTabHandler();
            spawningTabHandler = new SpawningTabHandler();
            utilityTabsHandler = new UtilityTabsHandler();
            
            // Initialize systems
            CacheManager.RefreshAllCaches();
            ExplosionSystem.RefreshExplosionTypes();
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            // Tab buttons at the top
            var tabRect = new Rect(inRect.x, inRect.y, inRect.width, 40);
            DrawCategoryTabs(tabRect);
            
            // Content area below tabs
            var contentRect = new Rect(inRect.x, inRect.y + 45, inRect.width, inRect.height - 45);
            
            switch (currentCategory)
            {
                case DevToolCategory.Items:
                    itemsTabHandler.DrawItemsTab(contentRect);
                    break;
                case DevToolCategory.Terrain:
                    terrainTabHandler.DrawTerrainTab(contentRect);
                    break;
                case DevToolCategory.Actions:
                    actionsTabHandler.DrawActionsTab(contentRect);
                    break;
                case DevToolCategory.Spawning:
                    spawningTabHandler.DrawSpawningTab(contentRect);
                    break;
                case DevToolCategory.Factions:
                    utilityTabsHandler.DrawFactionsTab(contentRect);
                    break;
                case DevToolCategory.Incidents:
                    utilityTabsHandler.DrawIncidentsTab(contentRect);
                    break;
                case DevToolCategory.Utilities:
                    utilityTabsHandler.DrawUtilitiesTab(contentRect);
                    break;
                case DevToolCategory.Research:
                    utilityTabsHandler.DrawResearchTab(contentRect);
                    break;
                case DevToolCategory.Trading:
                    utilityTabsHandler.DrawTradingTab(contentRect);
                    break;
            }
        }
        
        private void DrawCategoryTabs(Rect rect)
        {
            var tabWidth = rect.width / 9;
            var currentX = rect.x;
            
            var categories = new[] { DevToolCategory.Items, DevToolCategory.Terrain, DevToolCategory.Actions, DevToolCategory.Spawning, DevToolCategory.Factions, DevToolCategory.Incidents, DevToolCategory.Utilities, DevToolCategory.Research, DevToolCategory.Trading };
            var categoryNames = new[] { "Items", "Terrain", "Actions", "Spawning", "Factions", "Incidents", "Utilities", "Research", "Trading" };
            
            for (int i = 0; i < categories.Length; i++)
            {
                var tabRect = new Rect(currentX, rect.y, tabWidth, rect.height);
                var isSelected = currentCategory == categories[i];
                
                if (isSelected)
                    GUI.backgroundColor = Color.yellow;
                else
                    GUI.backgroundColor = Color.white;
                    
                if (Widgets.ButtonText(tabRect, categoryNames[i]))
                {
                    currentCategory = categories[i];
                    switch (currentCategory)
                    {
                        case DevToolCategory.Items:
                        case DevToolCategory.Spawning:
                            CacheManager.RefreshAllCaches();
                            break;
                        case DevToolCategory.Terrain:
                            CacheManager.RefreshAllCaches(); // Terrain uses cache too
                            break;
                        case DevToolCategory.Actions:
                            ExplosionSystem.RefreshExplosionTypes();
                            break;
                    }
                }
                
                currentX += tabWidth;
            }
            GUI.backgroundColor = Color.white;
        }
        
        // Keep only the static explosion preview method as it's called by the targeting system
        public static void DrawExplosionRadiusPreviewStatic()
        {
            // Delegate to the new ExplosionSystem
            ExplosionSystem.DrawExplosionRadiusPreviewStatic();
        }
        
        // Cache invalidation for when new content is loaded
        public static void InvalidateCache()
        {
            CacheManager.ClearCache();
        }
    }
}