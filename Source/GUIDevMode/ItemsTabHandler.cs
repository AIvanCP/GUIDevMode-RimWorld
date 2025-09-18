using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class ItemsTabHandler
    {
        private Vector2 categoryScrollPosition = Vector2.zero;
        private Vector2 itemScrollPosition = Vector2.zero;
        private string selectedItemCategory = "";
        private string itemSearchFilter = "";
        private int spawnQuantity = 1;
        private QualityCategory spawnQuality = QualityCategory.Normal;
        private ThingDef selectedStuff = null;
        
        // UI state
        private bool showItemDetails = false;
        private ThingDef selectedItemForDetails = null;
        
        public void DrawItemsTab(Rect rect)
        {
            // Ensure cache is ready
            if (CacheManager.IsCacheExpired())
                CacheManager.RefreshAllCaches();
            
            var mainRect = rect.ContractedBy(4f);
            
            // Split into categories (left) and items (right)
            var categoriesRect = new Rect(mainRect.x, mainRect.y, mainRect.width * 0.3f, mainRect.height);
            var itemsRect = new Rect(categoriesRect.xMax + 8f, mainRect.y, mainRect.width * 0.7f - 8f, mainRect.height);
            
            DrawCategoriesSection(categoriesRect);
            DrawItemsSection(itemsRect);
        }
        
        private void DrawCategoriesSection(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Item Categories:");
            
            // Item display limit controls
            var limitRect = listing.GetRect(30f);
            Widgets.Label(limitRect.LeftPart(0.6f), $"Limit: {CacheManager.ItemDisplayLimit}");
            if (Widgets.ButtonText(limitRect.RightPart(0.4f), "Reset"))
            {
                CacheManager.ResetItemDisplayLimit();
            }
            
            var sliderRect = listing.GetRect(20f);
            var newLimit = (int)Widgets.HorizontalSlider(sliderRect, CacheManager.ItemDisplayLimit, 10, 5000);
            if (newLimit != CacheManager.ItemDisplayLimit)
            {
                CacheManager.ItemDisplayLimit = newLimit;
                Log.Message($"[GUI Dev Mode] Item display limit changed to {newLimit}");
            }
            
            listing.Gap(8f);
            
            // Categories list
            var categoriesListRect = listing.GetRect(rect.height - 100f);
            var viewRect = new Rect(0, 0, categoriesListRect.width - 16f, CacheManager.AllItemCategories.Count * 25f);
            
            Widgets.BeginScrollView(categoriesListRect, ref categoryScrollPosition, viewRect);
            
            var y = 0f;
            foreach (var category in CacheManager.AllItemCategories)
            {
                var categoryRect = new Rect(0, y, viewRect.width, 24f);
                var isSelected = selectedItemCategory == category;
                
                if (isSelected)
                    Widgets.DrawHighlight(categoryRect);
                
                var itemCount = CacheManager.GetItemsByCategory(category).Count;
                var labelText = $"{category} ({itemCount})";
                
                if (Widgets.ButtonText(categoryRect, labelText, false))
                {
                    selectedItemCategory = category;
                    itemScrollPosition = Vector2.zero; // Reset item scroll when category changes
                }
                
                y += 25f;
            }
            
            Widgets.EndScrollView();
            
            listing.End();
        }
        
        private void DrawItemsSection(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            // Header with selected category
            if (string.IsNullOrEmpty(selectedItemCategory))
            {
                listing.Label("Select a category to view items");
                listing.End();
                return;
            }
            
            listing.Label($"Items in: {selectedItemCategory}");
            
            // Search filter
            var searchRect = listing.GetRect(30f);
            Widgets.Label(searchRect.LeftPart(0.15f), "Filter:");
            itemSearchFilter = Widgets.TextField(searchRect.RightPart(0.85f), itemSearchFilter);
            
            // Spawn controls
            DrawSpawnControls(listing);
            
            listing.Gap(8f);
            
            // Items list
            var items = CacheManager.GetItemsByCategory(selectedItemCategory);
            if (!string.IsNullOrEmpty(itemSearchFilter))
            {
                items = items.Where(item => 
                    (item.label?.ToLower().Contains(itemSearchFilter.ToLower()) ?? false) ||
                    item.defName.ToLower().Contains(itemSearchFilter.ToLower())).ToList();
            }
            
            var itemsListRect = listing.GetRect(rect.height - 140f);
            var viewRect = new Rect(0, 0, itemsListRect.width - 16f, items.Count * 25f);
            
            Widgets.BeginScrollView(itemsListRect, ref itemScrollPosition, viewRect);
            
            var y = 0f;
            foreach (var item in items)
            {
                var itemRect = new Rect(0, y, viewRect.width, 24f);
                DrawItemButton(itemRect, item);
                y += 25f;
            }
            
            Widgets.EndScrollView();
            
            listing.End();
        }
        
        private void DrawSpawnControls(Listing_Standard listing)
        {
            // Quantity control
            var quantityRect = listing.GetRect(30f);
            Widgets.Label(quantityRect.LeftPart(0.3f), $"Quantity: {spawnQuantity}");
            spawnQuantity = (int)Widgets.HorizontalSlider(quantityRect.RightPart(0.7f), spawnQuantity, 1, 100);
            
            // Quality control
            var qualityRect = listing.GetRect(30f);
            Widgets.Label(qualityRect.LeftPart(0.3f), $"Quality: {spawnQuality}");
            if (Widgets.ButtonText(qualityRect.RightPart(0.7f), spawnQuality.GetLabel()))
            {
                CycleQuality();
            }
            
            // Stuff selection for items that can have materials
            var stuffRect = listing.GetRect(30f);
            Widgets.Label(stuffRect.LeftPart(0.3f), "Material:");
            var stuffText = selectedStuff?.label ?? "Default";
            if (Widgets.ButtonText(stuffRect.RightPart(0.7f), stuffText))
            {
                CycleStuff();
            }
        }
        
        private void DrawItemButton(Rect rect, ThingDef item)
        {
            var buttonRect = rect.LeftPart(0.8f);
            var infoRect = rect.RightPart(0.2f);
            
            var labelText = item.label ?? item.defName;
            if (Widgets.ButtonText(buttonRect, labelText))
            {
                SpawnItem(item);
            }
            
            // Info button
            if (Widgets.ButtonText(infoRect, "?"))
            {
                selectedItemForDetails = item;
                showItemDetails = !showItemDetails;
            }
            
            // Tooltip
            if (Mouse.IsOver(rect))
            {
                var tooltip = $"{item.label}\n{item.description}";
                if (item.BaseMarketValue > 0)
                    tooltip += $"\nValue: ${item.BaseMarketValue:F0}";
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }
        
        private void SpawnItem(ThingDef item)
        {
            if (Find.CurrentMap == null) return;
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    var stuff = selectedStuff;
                    if (stuff != null && !item.MadeFromStuff)
                        stuff = null; // Clear stuff if item can't use materials
                    
                    var thing = ThingMaker.MakeThing(item, stuff);
                    thing.stackCount = spawnQuantity;
                    
                    // Apply quality if applicable
                    if (thing.TryGetComp<CompQuality>() != null)
                    {
                        thing.TryGetComp<CompQuality>().SetQuality(spawnQuality, ArtGenerationContext.Colony);
                    }
                    
                    GenPlace.TryPlaceThing(thing, target.Cell, Find.CurrentMap, ThingPlaceMode.Near);
                    
                    var materialText = stuff != null ? $" ({stuff.label})" : "";
                    var qualityText = thing.TryGetComp<CompQuality>() != null ? $" [{spawnQuality.GetLabel()}]" : "";
                    Messages.Message($"Spawned {spawnQuantity}x {item.label}{materialText}{qualityText}", 
                        MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        private void CycleQuality()
        {
            var qualities = System.Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>().ToArray();
            var currentIndex = System.Array.IndexOf(qualities, spawnQuality);
            currentIndex = (currentIndex + 1) % qualities.Length;
            spawnQuality = qualities[currentIndex];
        }
        
        private void CycleStuff()
        {
            var stuffs = DefDatabase<ThingDef>.AllDefs
                .Where(def => def.IsStuff && def.stuffProps != null)
                .OrderBy(def => def.label)
                .ToList();
            
            if (!stuffs.Any()) return;
            
            if (selectedStuff == null)
            {
                selectedStuff = stuffs.First();
            }
            else
            {
                var currentIndex = stuffs.IndexOf(selectedStuff);
                if (currentIndex >= 0)
                {
                    currentIndex = (currentIndex + 1) % (stuffs.Count + 1); // +1 for "none" option
                    selectedStuff = currentIndex == stuffs.Count ? null : stuffs[currentIndex];
                }
                else
                {
                    selectedStuff = stuffs.First();
                }
            }
        }
    }
}