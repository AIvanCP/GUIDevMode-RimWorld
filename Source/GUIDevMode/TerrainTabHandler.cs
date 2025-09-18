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
                
                // Category header
                var categoryRect = new Rect(0, y, viewRect.width, 25f);
                GUI.color = Color.yellow;
                Widgets.Label(categoryRect, $"▼ {category} ({terrainsInCategory.Count})");
                GUI.color = Color.white;
                y += 30f;
                
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
                        StartContinuousTerrainPlacement(terrain);
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
                // Terrain details
                Text.Font = GameFont.Medium;
                listing.Label(selectedTerrain.label?.CapitalizeFirst() ?? selectedTerrain.defName);
                Text.Font = GameFont.Small;
                
                listing.Gap(8f);
                
                if (!string.IsNullOrEmpty(selectedTerrain.description))
                {
                    listing.Label($"Description: {selectedTerrain.description}");
                    listing.Gap(4f);
                }
                
                listing.Label($"Mod: {selectedTerrain.modContentPack?.Name ?? "Core"}");
                listing.Label($"Buildable: {(selectedTerrain.BuildableByPlayer ? "Yes" : "No")}");
                listing.Label($"Natural: {(selectedTerrain.natural ? "Yes" : "No")}");
                
                if (selectedTerrain.costList?.Any() == true)
                {
                    listing.Gap(4f);
                    listing.Label("Cost:");
                    foreach (var cost in selectedTerrain.costList)
                    {
                        listing.Label($"  {cost.thingDef.label}: {cost.count}");
                    }
                }
            }
            
            listing.Gap(12f);
            listing.Label("Instructions:");
            listing.Label("• Click terrain to start continuous placement");
            listing.Label("• Right-click to stop placement");
            listing.Label("• Terrain replaces existing ground");
            
            listing.End();
        }
        
        private void StartContinuousTerrainPlacement(TerrainDef terrain)
        {
            ContinuousTerrainTargeting(terrain);
        }
        
        private void ContinuousTerrainTargeting(TerrainDef terrain)
        {
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    Find.CurrentMap.terrainGrid.SetTerrain(target.Cell, terrain);
                    Messages.Message($"Placed {terrain.label}", MessageTypeDefOf.NeutralEvent, false);
                    ContinuousTerrainTargeting(terrain); // Continue until right-click
                }
                else
                {
                    Messages.Message("Terrain placement stopped", MessageTypeDefOf.NeutralEvent);
                }
            }, null, delegate { 
                Messages.Message("Terrain placement stopped", MessageTypeDefOf.NeutralEvent); 
            });
        }
    }
}