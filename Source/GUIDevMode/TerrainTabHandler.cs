using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class TerrainTabHandler
    {
        private Vector2 scrollPosition = Vector2.zero;
        private string terrainSearchFilter = "";
        private TerrainDef selectedTerrain = null;
        private Dictionary<string, bool> categoryExpanded = new Dictionary<string, bool>();
        private bool isPlacingTerrain = false;
        
        public void DrawTerrainTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Terrain Placement:");
            
            // Search field
            listing.Label("Search:");
            var newSearchText = listing.TextEntry(terrainSearchFilter);
            if (newSearchText != terrainSearchFilter)
            {
                terrainSearchFilter = newSearchText;
            }
            
            listing.Gap(5f);
            listing.End();
            
            // Two columns: List and Details
            var remainingRect = new Rect(rect.x, rect.y + 80, rect.width, rect.height - 80);
            var leftColumn = new Rect(remainingRect.x, remainingRect.y, remainingRect.width * 0.6f, remainingRect.height);
            var rightColumn = new Rect(remainingRect.x + remainingRect.width * 0.6f + 5, remainingRect.y, remainingRect.width * 0.4f - 5, remainingRect.height);
            
            DrawTerrainList(leftColumn);
            DrawTerrainDetails(rightColumn);
        }
        
        private void DrawTerrainList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5);
            var listRect = new Rect(innerRect.x, innerRect.y + 25, innerRect.width, innerRect.height - 25);
            
            // Get terrain categories
            var terrainsByCategory = CacheManager.AllTerrainCategories;
            var totalTerrainCount = 0;
            
            // Calculate total height for all categories and terrains
            var contentHeight = 0f;
            foreach (var category in terrainsByCategory)
            {
                var terrainsInCategory = CacheManager.GetTerrainByCategory(category);
                if (!string.IsNullOrEmpty(terrainSearchFilter))
                {
                    terrainsInCategory = terrainsInCategory.Where(t => 
                        (t.label?.ToLower().Contains(terrainSearchFilter.ToLower()) ?? false) ||
                        t.defName.ToLower().Contains(terrainSearchFilter.ToLower())).ToList();
                }
                
                if (terrainsInCategory.Any())
                {
                    contentHeight += 30f; // Category header
                    contentHeight += terrainsInCategory.Count * 25f; // Terrain items
                    totalTerrainCount += terrainsInCategory.Count;
                }
            }
            
            var viewRect = new Rect(0, 0, listRect.width - 20, contentHeight);
            
            GUI.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, 20), $"Terrains ({totalTerrainCount}):");
            
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            
            var y = 0f;
            foreach (var category in terrainsByCategory)
            {
                var terrainsInCategory = CacheManager.GetTerrainByCategory(category);
                if (!string.IsNullOrEmpty(terrainSearchFilter))
                {
                    terrainsInCategory = terrainsInCategory.Where(t => 
                        (t.label?.ToLower().Contains(terrainSearchFilter.ToLower()) ?? false) ||
                        t.defName.ToLower().Contains(terrainSearchFilter.ToLower())).ToList();
                }
                
                if (!terrainsInCategory.Any()) continue;
                
                // Initialize expanded state if not set
                if (!categoryExpanded.ContainsKey(category))
                    categoryExpanded[category] = false;
                
                // Category header - clickable to expand/collapse
                var categoryRect = new Rect(0, y, viewRect.width, 25f);
                var expandSymbol = categoryExpanded[category] ? "▼" : "►";
                var categoryLabel = $"{expandSymbol} {category} ({terrainsInCategory.Count})";
                
                // Draw background for category header
                GUI.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
                GUI.DrawTexture(categoryRect, BaseContent.WhiteTex);
                GUI.color = Color.white;
                
                if (GUI.Button(categoryRect, categoryLabel))
                {
                    categoryExpanded[category] = !categoryExpanded[category];
                }
                y += 30f;
                
                // Only show terrain items if category is expanded
                if (!categoryExpanded[category]) continue;
                
                // Terrain items in category
                foreach (var terrain in terrainsInCategory)
                {
                    var terrainRect = new Rect(10, y, viewRect.width - 20, 22f);
                    
                    if (selectedTerrain == terrain)
                        Widgets.DrawHighlight(terrainRect);
                    
                    var labelText = terrain.label?.CapitalizeFirst() ?? terrain.defName;
                    if (Widgets.ButtonText(terrainRect, labelText, false))
                    {
                        selectedTerrain = terrain;
                        // Just select the terrain - user will click Place button to start placement
                    }
                    
                    y += 25f;
                }
            }
            
            Widgets.EndScrollView();
        }
        
        private void DrawTerrainDetails(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5);
            
            var listing = new Listing_Standard();
            listing.Begin(innerRect);
            
            if (selectedTerrain == null)
            {
                listing.Label("Select a terrain type to view details");
            }
            else
            {
                // Terrain details with image
                Text.Font = GameFont.Medium;
                listing.Label(selectedTerrain.label?.CapitalizeFirst() ?? selectedTerrain.defName);
                Text.Font = GameFont.Small;
                
                listing.Gap(8f);
                
                // Terrain image
                var imageRect = listing.GetRect(64f);
                var imageArea = new Rect(imageRect.x, imageRect.y, 64f, 64f);
                
                try
                {
                    var texture = selectedTerrain.uiIcon;
                    if (texture != null)
                    {
                        GUI.DrawTexture(imageArea, texture);
                    }
                    else
                    {
                        // Try to get graphic texture
                        var graphic = selectedTerrain.graphic;
                        if (graphic?.MatSingle?.mainTexture != null)
                        {
                            GUI.DrawTexture(imageArea, graphic.MatSingle.mainTexture);
                        }
                        else
                        {
                            GUI.color = Color.gray;
                            Widgets.DrawBox(imageArea);
                            Widgets.Label(imageArea, "No\nImage");
                            GUI.color = Color.white;
                        }
                    }
                }
                catch
                {
                    GUI.color = Color.gray;
                    Widgets.DrawBox(imageArea);
                    Widgets.Label(imageArea, "No\nImage");
                    GUI.color = Color.white;
                }
                
                listing.Gap(8f);
                
                if (!string.IsNullOrEmpty(selectedTerrain.description))
                {
                    var descRect = listing.GetRect(60f);
                    Widgets.Label(descRect, $"Description: {selectedTerrain.description}");
                    listing.Gap(4f);
                }
                
                listing.Label($"Buildable: {(selectedTerrain.BuildableByPlayer ? "Yes" : "No")}");
                listing.Label($"Natural: {(selectedTerrain.natural ? "Yes" : "No")}");
                listing.Label($"Beauty: {selectedTerrain.GetStatValueAbstract(StatDefOf.Beauty):F1}");
                listing.Label($"Walk Speed: {selectedTerrain.GetStatValueAbstract(StatDefOf.MoveSpeed):F2}x");
                // Mod info removed - handled by other mods
                
                if (selectedTerrain.costList?.Any() == true)
                {
                    listing.Gap(4f);
                    listing.Label("Cost:");
                    foreach (var cost in selectedTerrain.costList)
                    {
                        listing.Label($"  {cost.thingDef.label}: {cost.count}");
                    }
                }
                
                listing.Gap(12f);
                
                // Confirm button in bottom right area
                var buttonRect = listing.GetRect(35f);
                if (!isPlacingTerrain)
                {
                    if (Widgets.ButtonText(buttonRect, $"Place {selectedTerrain.label}"))
                    {
                        StartContinuousTerrainPlacement(selectedTerrain);
                        // Auto-close GUI immediately when starting placement
                        Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                    }
                }
                else
                {
                    GUI.color = Color.yellow;
                    Widgets.ButtonText(buttonRect, "Placing... (Right-click to stop)");
                    GUI.color = Color.white;
                }
            }
            
            if (!isPlacingTerrain)
            {
                listing.Gap(12f);
                listing.Label("Instructions:");
                listing.Label("• Select terrain and click 'Place' button");
                listing.Label("• Place multiple in continuous mode");
                listing.Label("• Right-click to stop placement");
            }
            
            listing.End();
        }
        
        private void StartContinuousTerrainPlacement(TerrainDef terrain)
        {
            isPlacingTerrain = true;
            ContinuousTerrainPlacement(terrain);
        }
        
        private void ContinuousTerrainPlacement(TerrainDef terrain)
        {
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    Find.CurrentMap.terrainGrid.SetTerrain(target.Cell, terrain);
                    Messages.Message($"Placed {terrain.label}", MessageTypeDefOf.NeutralEvent, false);
                    
                    // Continue placement for next terrain
                    ContinuousTerrainPlacement(terrain);
                }
                else
                {
                    // Right-click cancellation
                    isPlacingTerrain = false;
                    Messages.Message("Terrain placement stopped", MessageTypeDefOf.NeutralEvent);
                    
                    // Auto-close GUI after stopping
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                }
            }, null, delegate {
                // Preview delegate - show terrain info at mouse
                var cell = UI.MouseCell();
                if (cell.InBounds(Find.CurrentMap))
                {
                    var previewText = $"Place: {terrain.label}";
                    var mousePos = Event.current.mousePosition;
                    var labelRect = new Rect(mousePos.x + 10f, mousePos.y - 20f, 150f, 20f);
                    GUI.color = Color.yellow;
                    Widgets.Label(labelRect, previewText);
                    GUI.color = Color.white;
                }
            });
        }
    }
}