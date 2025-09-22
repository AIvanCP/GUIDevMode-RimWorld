using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class ItemsTabHandler
    {
        // UI State
        private Vector2 categoryScrollPosition = Vector2.zero;
        private Vector2 itemScrollPosition = Vector2.zero;
        private Vector2 descriptionScrollPosition = Vector2.zero; // Add scroll position for description
        private string selectedItemCategory = "Weapons"; // Start with weapons, not "All"
        private string itemSearchFilter = "";
        private int spawnQuantity = 1;
        private QualityCategory spawnQuality = QualityCategory.Normal;
        private ThingDef selectedStuff = null;
        private bool spawnMaxStack = false;
        // Mouse targeting is now always enabled (removed useMouseTargeting option)
        
        // Selected item for display
        private ThingDef selectedItem = null;
        
        // Item categories - "All" is at the bottom for safety
        private readonly string[] itemCategories = {
            "Weapons", "Apparel", "Food", "Medicine", "Drugs", 
            "Resources", "Materials", "Tools", "Buildings", "Art", "Other", "All"
        };
        
        // Cache for categorized items with adjustable cap system
        private Dictionary<string, List<ThingDef>> categorizedItems = new Dictionary<string, List<ThingDef>>();
        private bool cacheBuilt = false;
        private int maxItemsPerCategory = 2000; // Default 2k, adjustable
        private bool showAllItems = false; // Option to show all items (no cap)
        
        public void DrawItemsTab(Rect rect)
        {
            if (!cacheBuilt)
            {
                BuildItemCache();
            }
            
            var mainRect = rect.ContractedBy(5f);
            
            // Add controls at the top for cap settings
            var topControlsRect = new Rect(mainRect.x, mainRect.y, mainRect.width, 60f);
            var contentRect = new Rect(mainRect.x, topControlsRect.yMax + 5f, mainRect.width, mainRect.height - 65f);
            
            DrawCapControls(topControlsRect);
            
            // Split into left (categories & items) and right (details & controls)
            var leftRect = new Rect(contentRect.x, contentRect.y, contentRect.width * 0.6f, contentRect.height);
            var rightRect = new Rect(leftRect.xMax + 10f, contentRect.y, contentRect.width * 0.4f - 10f, contentRect.height);
            
            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);
        }
        
        private void DrawCapControls(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            // Cap controls
            var capRect = listing.GetRect(30f);
            var labelRect = new Rect(capRect.x, capRect.y, 120f, 30f);
            var fieldRect = new Rect(labelRect.xMax + 5f, capRect.y, 80f, 30f);
            var checkRect = new Rect(fieldRect.xMax + 10f, capRect.y, 120f, 30f);
            var rebuildRect = new Rect(checkRect.xMax + 10f, capRect.y, 100f, 30f);
            
            Widgets.Label(labelRect, "Items per category:");
            string buffer = maxItemsPerCategory.ToString();
            Widgets.TextFieldNumeric(fieldRect, ref maxItemsPerCategory, ref buffer, 100, 50000);
            
            bool newShowAll = showAllItems;
            Widgets.CheckboxLabeled(checkRect, "Show All Items", ref newShowAll);
            
            if (Widgets.ButtonText(rebuildRect, "Rebuild Cache"))
            {
                RebuildCache();
            }
            
            if (newShowAll != showAllItems)
            {
                showAllItems = newShowAll;
                RebuildCache();
            }
            
            listing.End();
        }
        
        private void RebuildCache()
        {
            cacheBuilt = false;
            categorizedItems.Clear();
            selectedItem = null;
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            // Categories at the top
            var categoryRect = new Rect(rect.x, rect.y, rect.width, 120f);
            DrawCategorySelection(categoryRect);
            
            // Items list below
            var itemsRect = new Rect(rect.x, categoryRect.yMax + 5f, rect.width, rect.height - categoryRect.height - 5f);
            DrawItemsList(itemsRect);
        }
        
        private void DrawCategorySelection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5f);
            
            GUI.BeginGroup(innerRect);
            var listing = new Listing_Standard();
            listing.Begin(new Rect(0, 0, innerRect.width, innerRect.height));
            
            listing.Label("Item Categories:");
            
            // Category buttons in rows
            const float buttonWidth = 80f;
            const float buttonHeight = 24f;
            int buttonsPerRow = Mathf.FloorToInt((innerRect.width - 10f) / (buttonWidth + 5f));
            
            for (int i = 0; i < itemCategories.Length; i++)
            {
                int row = i / buttonsPerRow;
                int col = i % buttonsPerRow;
                
                var buttonRect = new Rect(col * (buttonWidth + 5f), 25f + row * (buttonHeight + 3f), buttonWidth, buttonHeight);
                
                var category = itemCategories[i];
                bool isSelected = selectedItemCategory == category;
                
                if (isSelected)
                {
                    GUI.color = Color.green;
                }
                
                if (Widgets.ButtonText(buttonRect, category))
                {
                    selectedItemCategory = category;
                    selectedItem = null; // Clear selection when changing category
                    itemSearchFilter = ""; // Clear search
                }
                
                if (isSelected)
                {
                    GUI.color = Color.white;
                }
            }
            
            listing.End();
            GUI.EndGroup();
        }
        
        private void DrawItemsList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5f);
            
            // Search box at top
            var searchRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 24f);
            var newSearchFilter = Widgets.TextField(searchRect, itemSearchFilter);
            if (newSearchFilter != itemSearchFilter)
            {
                itemSearchFilter = newSearchFilter;
                selectedItem = null; // Clear selection when searching
            }
            
            // Items list
            var listRect = new Rect(innerRect.x, searchRect.yMax + 5f, innerRect.width, innerRect.height - searchRect.height - 5f);
            
            if (!categorizedItems.ContainsKey(selectedItemCategory))
            {
                Widgets.Label(listRect, "No items in this category");
                return;
            }
            
            var items = categorizedItems[selectedItemCategory];
            if (!string.IsNullOrEmpty(itemSearchFilter))
            {
                items = items.Where(item => item.label.ToLower().Contains(itemSearchFilter.ToLower()) || 
                                          item.defName.ToLower().Contains(itemSearchFilter.ToLower())).ToList();
            }
            
            // Draw scrollable list of items with icons
            var itemHeight = 32f; // Increased height to accommodate icons
            var scrollRect = new Rect(0, 0, listRect.width - 16f, items.Count * itemHeight);
            Widgets.BeginScrollView(listRect, ref itemScrollPosition, scrollRect);
            
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var itemRect = new Rect(0, i * itemHeight, scrollRect.width, itemHeight);
                
                bool isSelected = selectedItem == item;
                if (isSelected)
                {
                    Widgets.DrawHighlight(itemRect);
                }
                
                if (Widgets.ButtonInvisible(itemRect))
                {
                    selectedItem = item;
                }
                
                // Draw item icon (small)
                var iconSize = 24f;
                var iconRect = new Rect(itemRect.x + 4f, itemRect.y + 4f, iconSize, iconSize);
                
                // Draw icon using improved method with proper texture handling
                DrawItemIcon(item, iconRect);
                
                // Item name positioned after icon
                var textRect = new Rect(iconRect.xMax + 8f, itemRect.y, itemRect.width - iconRect.width - 12f, itemRect.height);
                Widgets.Label(textRect, item.LabelCap);
            }
            
            Widgets.EndScrollView();
        }
        
        private void DrawRightPanel(Rect rect)
        {
            if (selectedItem == null)
            {
                Widgets.DrawMenuSection(rect);
                var centerRect = new Rect(rect.x + 10f, rect.y + rect.height / 2f - 10f, rect.width - 20f, 20f);
                Widgets.Label(centerRect, "Select an item to see details");
                return;
            }
            
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5f);
            
            var listing = new Listing_Standard();
            listing.Begin(innerRect);
            
            // Item icon and basic info - enhanced icon display with better fallbacks
            var iconRect = new Rect(innerRect.x, innerRect.y, 64f, 64f);
            
            // Draw background for icon
            Widgets.DrawMenuSection(iconRect);
            var iconInnerRect = iconRect.ContractedBy(2f);
            
            // Use the improved icon drawing method
            DrawItemIcon(selectedItem, iconInnerRect);
            
            var infoRect = new Rect(iconRect.xMax + 10f, innerRect.y, innerRect.width - iconRect.width - 10f, 64f);
            GUI.BeginGroup(infoRect);
            
            var infoListing = new Listing_Standard();
            infoListing.Begin(new Rect(0, 0, infoRect.width, infoRect.height));
            infoListing.Label($"Name: {selectedItem.LabelCap}");
            // Mod info removed - handled by other mods
            infoListing.Label($"Category: {selectedItem.category}");
            infoListing.End();
            
            GUI.EndGroup();
            
            // Skip past the icon area
            listing.GapLine(70f);
            
            // Description with proper height calculation and scrolling
            if (!string.IsNullOrEmpty(selectedItem.description))
            {
                listing.Label("Description:");
                
                // Calculate proper height for description text
                var descWidth = listing.ColumnWidth;
                float fullDescHeight = Text.CalcHeight(selectedItem.description, descWidth - 16f);
                float maxDescHeight = 120f; // Maximum height before scrolling
                
                var descRect = listing.GetRect(Mathf.Min(fullDescHeight + 10f, maxDescHeight));
                
                // Always use scrollable text area - this fixes the scrolling issue
                if (fullDescHeight > maxDescHeight - 10f)
                {
                    // Need scrolling - create content area larger than visible area
                    var contentRect = new Rect(0, 0, descWidth - 16f, fullDescHeight);
                    Widgets.BeginScrollView(descRect, ref descriptionScrollPosition, contentRect);
                    Widgets.Label(contentRect, selectedItem.description);
                    Widgets.EndScrollView();
                }
                else
                {
                    // Short description - no scrolling needed
                    Widgets.Label(descRect, selectedItem.description);
                }
                listing.Gap(10f);
            }
            
            // Spawn controls
            listing.GapLine();
            listing.Label("Spawn Options:");
            
            // Quantity controls
            var quantityRect = listing.GetRect(30f);
            var qtyLabelRect = new Rect(quantityRect.x, quantityRect.y, 50f, quantityRect.height);
            var qtyFieldRect = new Rect(qtyLabelRect.xMax + 5f, quantityRect.y, 80f, quantityRect.height);
            var maxStackRect = new Rect(qtyFieldRect.xMax + 10f, quantityRect.y, 100f, quantityRect.height);
            
            Widgets.Label(qtyLabelRect, "Qty:");
            string buffer = spawnQuantity.ToString();
            Widgets.TextFieldNumeric(qtyFieldRect, ref spawnQuantity, ref buffer, 1, 10000);
            Widgets.CheckboxLabeled(maxStackRect, "Max Stack", ref spawnMaxStack);
            
            // Quality selection for items that support it
            if (selectedItem.HasComp(typeof(CompQuality)))
            {
                listing.Gap(5f);
                var qualityRect = listing.GetRect(30f);
                var qualityLabelRect = new Rect(qualityRect.x, qualityRect.y, 60f, qualityRect.height);
                var qualityDropRect = new Rect(qualityLabelRect.xMax + 5f, qualityRect.y, 120f, qualityRect.height);
                
                Widgets.Label(qualityLabelRect, "Quality:");
                if (Widgets.ButtonText(qualityDropRect, spawnQuality.GetLabel()))
                {
                    var qualityOptions = new List<FloatMenuOption>();
                    foreach (QualityCategory quality in System.Enum.GetValues(typeof(QualityCategory)))
                    {
                        if (quality == QualityCategory.Awful) continue; // Skip awful quality
                        qualityOptions.Add(new FloatMenuOption(quality.GetLabel(), () => spawnQuality = quality));
                    }
                    Find.WindowStack.Add(new FloatMenu(qualityOptions));
                }
            }
            
            // Stuff/Material selection for items that need it
            if (selectedItem.MadeFromStuff)
            {
                listing.Gap(5f);
                var stuffRect = listing.GetRect(30f);
                var stuffLabelRect = new Rect(stuffRect.x, stuffRect.y, 60f, stuffRect.height);
                var stuffDropRect = new Rect(stuffLabelRect.xMax + 5f, stuffRect.y, 120f, stuffRect.height);
                
                Widgets.Label(stuffLabelRect, "Material:");
                var stuffLabel = selectedStuff?.LabelCap ?? "None";
                if (Widgets.ButtonText(stuffDropRect, stuffLabel))
                {
                    var stuffOptions = new List<FloatMenuOption>();
                    stuffOptions.Add(new FloatMenuOption("None", () => selectedStuff = null));
                    
                    var stuffCategories = selectedItem.stuffCategories;
                    if (stuffCategories != null)
                    {
                        foreach (var stuffDef in DefDatabase<ThingDef>.AllDefsListForReading
                                .Where(def => def.IsStuff && stuffCategories.Any(cat => def.stuffProps.categories.Contains(cat)))
                                .Take(50)) // Limit to prevent lag
                        {
                            stuffOptions.Add(new FloatMenuOption(stuffDef.LabelCap, () => selectedStuff = stuffDef));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(stuffOptions));
                }
            }
            
            listing.Gap(10f);
            
            // Spawn buttons - only mouse targeting button (no dropdown/normal spawn anymore)
            if (listing.ButtonText("Spawn Item with Mouse Targeting"))
            {
                StartItemTargeting();
            }
            
            listing.End();
        }
        
        private void BuildItemCache()
        {
            categorizedItems.Clear();
            
            // Get all items including modded ones - more comprehensive filter
            var allItems = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => 
                    (def.category == ThingCategory.Item || 
                     def.thingClass == typeof(MinifiedThing) ||
                     def.building != null) &&
                    !def.IsCorpse && 
                    !def.isUnfinishedThing &&
                    !def.destroyOnDrop &&
                    def.label != null)
                .OrderBy(def => {
                    try
                    {
                        var labelCap = def.LabelCap;
                        if (labelCap != null && !labelCap.ToString().NullOrEmpty())
                        {
                            return labelCap.ToString();
                        }
                    }
                    catch
                    {
                        // Fallback if LabelCap fails
                    }
                    return def.defName ?? "";
                })
                .ToList();
            
            // Initialize category lists
            foreach (var category in itemCategories)
            {
                categorizedItems[category] = new List<ThingDef>();
            }
            
            int effectiveLimit = showAllItems ? int.MaxValue : maxItemsPerCategory;
            
            // Categorize items with cap system
            foreach (var item in allItems)
            {
                var categoryName = GetItemCategoryName(item);
                
                // Add to specific category (with cap)
                if (categorizedItems[categoryName].Count < effectiveLimit)
                {
                    categorizedItems[categoryName].Add(item);
                }
                
                // Add to "All" category (with cap)
                if (categorizedItems["All"].Count < effectiveLimit)
                {
                    categorizedItems["All"].Add(item);
                }
            }
            
            cacheBuilt = true;
            Log.Message($"[GUI Dev Mode] Built item cache with {allItems.Count} total items, limit: {(showAllItems ? "unlimited" : maxItemsPerCategory.ToString())} per category");
            
            // Log category counts for debugging
            foreach (var category in itemCategories)
            {
                Log.Message($"[GUI Dev Mode] Category '{category}': {categorizedItems[category].Count} items");
            }
        }
        
        private string GetItemCategoryName(ThingDef item)
        {
            // Check weapons first
            if (item.IsWeapon) return "Weapons";
            
            // Check apparel
            if (item.IsApparel) return "Apparel";
            
            // Check ingestibles (food and drugs)
            if (item.IsIngestible)
            {
                // Check for drugs first (more specific)
                if (item.IsDrug || 
                    item.ingestible?.drugCategory != DrugCategory.None ||
                    item.defName.ToLowerInvariant().Contains("drug") ||
                    item.defName.ToLowerInvariant().Contains("alcohol") ||
                    item.defName.ToLowerInvariant().Contains("beer") ||
                    item.defName.ToLowerInvariant().Contains("smoke") ||
                    item.defName.ToLowerInvariant().Contains("joint") ||
                    item.defName.ToLowerInvariant().Contains("pill") ||
                    item.defName.ToLowerInvariant().Contains("stim") ||
                    item.defName.ToLowerInvariant().Contains("cocaine") ||
                    item.defName.ToLowerInvariant().Contains("morphine") ||
                    item.defName.ToLowerInvariant().Contains("opium") ||
                    item.defName.ToLowerInvariant().Contains("caffeine") ||
                    item.defName.ToLowerInvariant().Contains("nicotine") ||
                    item.defName.ToLowerInvariant().Contains("psychite") ||
                    item.defName.ToLowerInvariant().Contains("luciferium") ||
                    item.defName.ToLowerInvariant().Contains("ambrosia") ||
                    item.defName.ToLowerInvariant().Contains("yayo") ||
                    item.defName.ToLowerInvariant().Contains("flake") ||
                    item.defName.ToLowerInvariant().Contains("wake") ||
                    item.defName.ToLowerInvariant().Contains("tea") ||
                    item.defName.ToLowerInvariant().Contains("wine") ||
                    item.defName.ToLowerInvariant().Contains("whiskey") ||
                    item.defName.ToLowerInvariant().Contains("rum") ||
                    item.label?.ToLowerInvariant().Contains("drug") == true ||
                    item.label?.ToLowerInvariant().Contains("alcohol") == true ||
                    item.description?.ToLowerInvariant().Contains("drug") == true ||
                    item.description?.ToLowerInvariant().Contains("addiction") == true)
                {
                    return "Drugs";
                }
                
                // Otherwise it's food
                return "Food";
            }
            
            // Check medicine
            if (item.IsMedicine || item.IsNaturalOrgan) return "Medicine";
            
            // Check materials/stuff
            if (item.IsStuff) return "Materials";
            
            // Check buildings (including minified)
            if (item.building != null || item.thingClass == typeof(MinifiedThing)) return "Buildings";
            
            // Check art
            if (item.IsArt) return "Art";
            
            // Check tools by category names
            if (item.thingCategories?.Any(cat => 
                cat.defName.ToLowerInvariant().Contains("tool") ||
                cat.defName.ToLowerInvariant().Contains("equipment") ||
                cat.defName.ToLowerInvariant().Contains("utility")) == true) 
                return "Tools";
            
            // Resource-like items (stackable, not food/medicine/drug)
            if (item.stackLimit > 1 && !item.IsIngestible && !item.IsMedicine && !item.IsDrug)
                return "Resources";
            
            return "Other";
        }
        
        private void StartItemTargeting()
        {
            if (selectedItem == null) return;
            
            // Auto-close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Messages.Message($"Targeting {selectedItem.LabelCap} - Right-click to cancel", MessageTypeDefOf.NeutralEvent);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    var quantity = spawnMaxStack ? selectedItem.stackLimit : spawnQuantity;
                    var stuff = selectedItem.MadeFromStuff ? selectedStuff : null;
                    
                    Thing thing = ThingMaker.MakeThing(selectedItem, stuff);
                    
                    if (thing.def.HasComp(typeof(CompQuality)) && thing.TryGetComp<CompQuality>() != null)
                    {
                        thing.TryGetComp<CompQuality>().SetQuality(spawnQuality, ArtGenerationContext.Colony);
                    }
                    
                    thing.stackCount = quantity;
                    
                    GenPlace.TryPlaceThing(thing, target.Cell, Find.CurrentMap, ThingPlaceMode.Near);
                    Messages.Message($"Spawned {quantity}x {thing.LabelCap} at {target.Cell}", MessageTypeDefOf.PositiveEvent);
                    
                    // Continue targeting for more items (continuous spawning)
                    StartItemTargeting();
                }
                else
                {
                    Messages.Message("Item spawning cancelled", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        /// <summary>
        /// Draws an item icon with improved fallback logic and proper texture handling
        /// </summary>
        private void DrawItemIcon(ThingDef item, Rect iconRect)
        {
            Texture2D texture = null;
            
            // First try: Direct uiIcon
            if (item.uiIcon != null)
            {
                texture = item.uiIcon;
            }
            // Second try: Graphic material texture
            else if (item.graphic?.MatSingle?.mainTexture != null)
            {
                texture = item.graphic.MatSingle.mainTexture as Texture2D;
            }
            // Third try: Force graphic initialization and try again
            else
            {
                try
                {
                    // Try to initialize the graphic if it's not already done
                    if (item.graphic == null)
                    {
                        item.graphic = item.graphicData?.Graphic;
                    }
                    
                    if (item.graphic?.MatSingle?.mainTexture != null)
                    {
                        texture = item.graphic.MatSingle.mainTexture as Texture2D;
                    }
                    // Fourth try: Use the default item texture
                    else if (item.uiIcon == null && item.graphicData != null)
                    {
                        // Force load the UI icon
                        var iconPath = item.graphicData.texPath;
                        if (!iconPath.NullOrEmpty())
                        {
                            texture = ContentFinder<Texture2D>.Get(iconPath, false);
                        }
                    }
                }
                catch
                {
                    // Failed to get texture, will use fallback
                }
            }
            
            // Draw the texture or fallback
            if (texture != null)
            {
                GUI.DrawTexture(iconRect, texture);
            }
            else
            {
                // Fallback: Draw a gray placeholder
                GUI.color = Color.gray;
                GUI.DrawTexture(iconRect, BaseContent.WhiteTex);
                GUI.color = Color.white;
                
                // Add a small "?" in the center to indicate missing icon
                var style = new GUIStyle(Text.CurFontStyle);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = Mathf.RoundToInt(iconRect.height * 0.6f);
                GUI.Label(iconRect, "?", style);
            }
        }
    }
}