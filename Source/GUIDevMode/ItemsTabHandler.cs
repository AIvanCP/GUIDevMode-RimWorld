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
        private Vector2 descriptionScrollPosition = Vector2.zero;
        private string selectedItemCategory = "Weapons";
        private string itemSearchFilter = "";
        private int spawnQuantity = 1;
        private string spawnQuantityText = "1";
        // continuousSpawning removed - always true by default
        private QualityCategory selectedQuality = QualityCategory.Normal;
        private ThingDef selectedStuff = null;
        
        // All tab capping options
        private int allTabItemCap = 500;
        private string allTabCapText = "500";
        private bool enableAllTabCap = true;
        
        // Selected item for display
        private ThingDef selectedItem = null;
        
        // Item categories
        private readonly string[] itemCategories = {
            "Weapons", "Apparel", "Food", "Medicine", "Drugs", 
            "Resources", "Materials", "Tools", "Buildings", "Art", "Other", "All"
        };
        
        // Cache for categorized items
        private Dictionary<string, List<ThingDef>> categorizedItems = new Dictionary<string, List<ThingDef>>();
        private List<ThingDef> allItemsUncapped = new List<ThingDef>(); // Full list for search
        private bool cacheBuilt = false;
        
        public void DrawItemsTab(Rect rect)
        {
            if (!cacheBuilt)
            {
                BuildItemCache();
                cacheBuilt = true;
            }
            
            // Split into left and right panels
            var leftRect = new Rect(rect.x, rect.y, rect.width * 0.4f, rect.height);
            var rightRect = new Rect(leftRect.xMax + 5f, rect.y, rect.width * 0.6f - 5f, rect.height);
            
            DrawLeftPanel(leftRect);
            DrawRightPanel(rightRect);
        }
        
        private void DrawLeftPanel(Rect rect)
        {
            // Adjust category panel height based on whether All tab is selected
            var categoryPanelHeight = selectedItemCategory == "All" ? 150f : 90f; // Reduced heights
            var categoryRect = new Rect(rect.x, rect.y, rect.width, categoryPanelHeight);
            var itemRect = new Rect(rect.x, categoryRect.yMax + 5f, rect.width, rect.height - categoryRect.height - 5f);
            
            DrawCategorySelection(categoryRect);
            DrawItemList(itemRect);
        }
        
        private void DrawCategorySelection(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5f);
            var currentY = innerRect.y;
            
            // Item search filter only
            var itemSearchRect = new Rect(innerRect.x, currentY, innerRect.width, 25f);
            GUI.Label(new Rect(itemSearchRect.x, itemSearchRect.y - 18f, itemSearchRect.width, 18f), "Search Items:", Text.CurFontStyle);
            itemSearchFilter = Widgets.TextField(itemSearchRect, itemSearchFilter);
            currentY = itemSearchRect.yMax + 5f;
            
            // All tab cap settings (only show when "All" is selected)
            var capSettingsHeight = 0f;
            if (selectedItemCategory == "All")
            {
                capSettingsHeight = 65f; // Reduced height
                var capSettingsRect = new Rect(innerRect.x, currentY, innerRect.width, capSettingsHeight);
                
                // Cap checkbox - more compact
                var capCheckRect = new Rect(capSettingsRect.x, capSettingsRect.y, capSettingsRect.width, 20f);
                Widgets.CheckboxLabeled(capCheckRect, $"Limit display ({allTabItemCap} items)", ref enableAllTabCap);
                
                // Cap amount input (only if cap is enabled) - more compact
                if (enableAllTabCap)
                {
                    var capInputRect = new Rect(capSettingsRect.x, capCheckRect.yMax + 2f, capSettingsRect.width * 0.5f, 22f);
                    var capLabelRect = new Rect(capInputRect.xMax + 5f, capInputRect.y, capSettingsRect.width * 0.5f - 5f, 22f);
                    
                    GUI.Label(new Rect(capSettingsRect.x, capInputRect.y - 16f, capSettingsRect.width, 14f), "Limit:", Text.CurFontStyle);
                    allTabCapText = Widgets.TextField(capInputRect, allTabCapText);
                    
                    if (int.TryParse(allTabCapText, out int parsedCap))
                    {
                        allTabItemCap = Mathf.Max(50, parsedCap); // Minimum 50 items
                    }
                    else
                    {
                        allTabItemCap = 500;
                        allTabCapText = "500";
                    }
                    
                    if (Widgets.ButtonText(capLabelRect, "Rebuild"))
                    {
                        cacheBuilt = false; // Force cache rebuild with new cap
                    }
                }
                currentY = capSettingsRect.yMax + 5f;
            }
            
            // Category buttons (no filtering, showing all categories)
            var buttonRect = new Rect(innerRect.x, currentY, innerRect.width, innerRect.height - (currentY - innerRect.y));
            
            Widgets.BeginScrollView(buttonRect, ref categoryScrollPosition, 
                new Rect(0, 0, buttonRect.width - 16f, itemCategories.Length * 30f));
            
            for (int i = 0; i < itemCategories.Length; i++)
            {
                var category = itemCategories[i];
                var categoryButtonRect = new Rect(0, i * 30f, buttonRect.width - 16f, 28f);
                
                bool isSelected = selectedItemCategory == category;
                if (isSelected)
                {
                    Widgets.DrawHighlight(categoryButtonRect);
                }
                
                // Show item count for each category
                var itemCount = categorizedItems.ContainsKey(category) ? categorizedItems[category].Count : 0;
                var categoryLabel = $"{category} ({itemCount})";
                
                if (Widgets.ButtonText(categoryButtonRect, categoryLabel))
                {
                    selectedItemCategory = category;
                    selectedItem = null; // Clear selection when changing category
                }
            }
            
            Widgets.EndScrollView();
        }
        
        private void DrawItemList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var listRect = rect.ContractedBy(5f);
            
            var items = GetFilteredItems();
            if (!items.Any())
            {
                var centerRect = new Rect(listRect.x, listRect.y + listRect.height / 2f - 10f, listRect.width, 20f);
                Widgets.Label(centerRect, "No items found");
                return;
            }
            
            // Draw scrollable list of items with icons
            var itemHeight = 32f;
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
            
            // NEW LAYOUT: Top = Large Icon, Middle = Description, Bottom = Spawn Options
            
            // TOP SECTION: Large Icon Display (128x128)
            var largeIconSize = 128f;
            var topSectionHeight = largeIconSize + 45f;
            var topSectionRect = new Rect(innerRect.x, innerRect.y, innerRect.width, topSectionHeight);
            
            // Center the large icon horizontally
            var largeIconRect = new Rect(
                topSectionRect.x + (topSectionRect.width - largeIconSize) / 2f,
                topSectionRect.y + 10f,
                largeIconSize,
                largeIconSize
            );
            
            // Draw background for large icon
            Widgets.DrawMenuSection(largeIconRect);
            var largeIconInner = largeIconRect.ContractedBy(3f);
            
            // Draw the large icon
            DrawItemIcon(selectedItem, largeIconInner);
            
            // Add item name below the large icon
            var nameRect = new Rect(topSectionRect.x, largeIconRect.yMax + 5f, topSectionRect.width, 25f);
            var nameStyle = new GUIStyle(Text.CurFontStyle);
            nameStyle.alignment = TextAnchor.MiddleCenter;
            nameStyle.fontStyle = FontStyle.Bold;
            GUI.Label(nameRect, selectedItem.LabelCap, nameStyle);
            
            // MIDDLE SECTION: Description
            var middleSectionY = topSectionRect.yMax + 10f;
            var bottomSectionHeight = 140f; // Increased from 100f to accommodate all controls
            var middleSectionHeight = innerRect.height - topSectionHeight - bottomSectionHeight - 20f;
            var middleSectionRect = new Rect(innerRect.x, middleSectionY, innerRect.width, middleSectionHeight);
            
            // Description with scrolling (removed category info to save space)
            var descRect = new Rect(middleSectionRect.x, middleSectionRect.y, middleSectionRect.width, middleSectionRect.height);
            
            if (!string.IsNullOrEmpty(selectedItem.description))
            {
                var descLabelRect = new Rect(descRect.x, descRect.y, descRect.width, 16f); // Reduced height further
                GUI.Label(descLabelRect, "Description:", Text.CurFontStyle);
                
                var scrollableDescRect = new Rect(descRect.x, descLabelRect.yMax + 2f, descRect.width, descRect.height - 18f);
                var descText = selectedItem.description;
                var textHeight = Text.CalcHeight(descText, scrollableDescRect.width - 16f);
                
                if (textHeight > scrollableDescRect.height)
                {
                    // Use scrollable view for long descriptions
                    var scrollContentRect = new Rect(0, 0, scrollableDescRect.width - 16f, textHeight);
                    Widgets.BeginScrollView(scrollableDescRect, ref descriptionScrollPosition, scrollContentRect);
                    Widgets.Label(scrollContentRect, descText);
                    Widgets.EndScrollView();
                }
                else
                {
                    // Simple label for short descriptions
                    Widgets.Label(scrollableDescRect, descText);
                }
            }
            else
            {
                var noDescRect = new Rect(descRect.x, descRect.y + 18f, descRect.width, 20f);
                GUI.Label(noDescRect, "No description available.", Text.CurFontStyle);
            }
            
            // BOTTOM SECTION: Spawn Options
            var bottomSectionY = middleSectionRect.yMax + 10f;
            var bottomSectionRect = new Rect(innerRect.x, bottomSectionY, innerRect.width, bottomSectionHeight);
            
            var listing = new Listing_Standard();
            listing.Begin(bottomSectionRect);
            
            // Quality selection for items that support it (including modded items with CompQuality)
            if (selectedItem.HasComp(typeof(CompQuality)))
            {
                listing.Label("Quality:");
                var qualityOptions = System.Enum.GetValues(typeof(QualityCategory)).Cast<QualityCategory>().ToArray();
                var qualityLabels = qualityOptions.Select(q => q.GetLabel()).ToArray();
                var currentQualityIndex = System.Array.IndexOf(qualityOptions, selectedQuality);
                
                var newQualityIndex = Mathf.Max(0, currentQualityIndex);
                if (listing.ButtonText($"Quality: {qualityLabels[newQualityIndex]}"))
                {
                    var qualityMenuOptions = qualityOptions.Select((quality, index) => 
                        new FloatMenuOption(qualityLabels[index], () => selectedQuality = quality)).ToList();
                    Find.WindowStack.Add(new FloatMenu(qualityMenuOptions));
                }
            }
            
            // Material/Stuff selection
            if (selectedItem.MadeFromStuff)
            {
                var stuffLabelRect = listing.GetRect(20f);
                var stuffDropRect = listing.GetRect(25f);
                
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
                                .Take(50))
                        {
                            stuffOptions.Add(new FloatMenuOption(stuffDef.LabelCap, () => selectedStuff = stuffDef));
                        }
                    }
                    Find.WindowStack.Add(new FloatMenu(stuffOptions));
                }
            }
            
            listing.Gap(10f);
            
            // Spawn quantity controls
            listing.Label("Spawn Quantity:");
            var quantityRect = listing.GetRect(30f);
            var quantityInputRect = new Rect(quantityRect.x, quantityRect.y, quantityRect.width * 0.6f, quantityRect.height);
            var maxButtonRect = new Rect(quantityInputRect.xMax + 5f, quantityRect.y, quantityRect.width * 0.35f, quantityRect.height);
            
            // Quantity input field
            spawnQuantityText = Widgets.TextField(quantityInputRect, spawnQuantityText);
            if (int.TryParse(spawnQuantityText, out int parsedQuantity))
            {
                spawnQuantity = Mathf.Max(1, parsedQuantity);
            }
            else
            {
                spawnQuantity = 1;
                spawnQuantityText = "1";
            }
            
            // Max button - sets quantity to item's stack limit
            if (Widgets.ButtonText(maxButtonRect, "Max"))
            {
                spawnQuantity = selectedItem.stackLimit;
                spawnQuantityText = spawnQuantity.ToString();
            }
            
            listing.Gap(5f);
            
            // Spawn button (continuous spawning is always enabled)
            if (listing.ButtonText("Spawn Item with Mouse Targeting"))
            {
                StartItemTargeting();
            }
            
            listing.End();
        }
        
        private List<ThingDef> GetFilteredItems()
        {
            if (!categorizedItems.ContainsKey(selectedItemCategory))
                return new List<ThingDef>();
            
            // For "All" category with search filter, search the full uncapped list
            if (selectedItemCategory == "All" && !string.IsNullOrEmpty(itemSearchFilter))
            {
                return allItemsUncapped.Where(item => 
                    item.LabelCap.ToString().ToLower().Contains(itemSearchFilter.ToLower())).ToList();
            }
            
            var items = categorizedItems[selectedItemCategory];
            
            if (string.IsNullOrEmpty(itemSearchFilter))
                return items;
            
            return items.Where(item => item.LabelCap.ToString().ToLower().Contains(itemSearchFilter.ToLower())).ToList();
        }
        
        private void BuildItemCache()
        {
            categorizedItems.Clear();
            allItemsUncapped.Clear();
            
            var allItems = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => {
                    // Include more item types including modded content
                    bool isValidItem = (def.category == ThingCategory.Item || 
                                       def.thingClass == typeof(MinifiedThing) ||
                                       def.building != null ||
                                       def.race != null || // Include animals/mechanoids
                                       def.plant != null || // Include plants
                                       def.IsWeapon ||
                                       def.IsApparel ||
                                       def.IsIngestible ||
                                       def.IsMedicine ||
                                       def.IsStuff);
                    
                    // Basic validity checks
                    bool hasValidProperties = !def.IsCorpse && 
                                            !def.isUnfinishedThing &&
                                            !def.destroyOnDrop &&
                                            def.label != null &&
                                            !def.label.NullOrEmpty();
                    
                    // Filter out items without graphics (big red X items)
                    bool hasValidGraphics = HasValidGraphics(def);
                    
                    return isValidItem && hasValidProperties && hasValidGraphics;
                })
                .OrderBy(def => {
                    try
                    {
                        var labelCap = def.LabelCap;
                        if (labelCap != null && !labelCap.ToString().NullOrEmpty())
                            return labelCap.ToString();
                        return def.defName ?? "";
                    }
                    catch
                    {
                        return def.defName ?? "";
                    }
                })
                .ToList();
            
            // Store full uncapped list for search functionality
            allItemsUncapped.AddRange(allItems);
            
            // Initialize all categories
            foreach (var category in itemCategories)
            {
                categorizedItems[category] = new List<ThingDef>();
            }
            
            // Categorize items
            int allTabItemCount = 0;
            foreach (var item in allItems)
            {
                var categoryName = GetItemCategory(item);
                categorizedItems[categoryName].Add(item);
                
                // Add to "All" category with cap consideration
                if (!enableAllTabCap || allTabItemCount < allTabItemCap)
                {
                    categorizedItems["All"].Add(item);
                    allTabItemCount++;
                }
            }
        }
        
        /// <summary>
        /// Checks if an item has valid graphics to avoid big red X items
        /// </summary>
        private bool HasValidGraphics(ThingDef def)
        {
            try
            {
                // First check: Does it have a valid uiIcon?
                if (def.uiIcon != null)
                    return true;
                
                // Second check: Does it have valid graphic data with existing texture?
                if (def.graphicData != null && !def.graphicData.texPath.NullOrEmpty())
                {
                    // Try to load the texture to verify it exists - more strict checking
                    var texture = ContentFinder<Texture2D>.Get(def.graphicData.texPath, false);
                    if (texture != null && texture != BaseContent.BadTex)
                        return true;
                }
                
                // Third check: Does the graphic have a valid texture?
                if (def.graphic?.MatSingle?.mainTexture != null)
                {
                    var texture = def.graphic.MatSingle.mainTexture;
                    if (texture != BaseContent.BadTex && texture.name != "ERRORTEX")
                        return true;
                }
                
                // Fourth check: Try to initialize graphic if not done yet
                if (def.graphic == null && def.graphicData != null)
                {
                    try
                    {
                        def.graphic = def.graphicData.Graphic;
                        if (def.graphic?.MatSingle?.mainTexture != null)
                        {
                            var texture = def.graphic.MatSingle.mainTexture;
                            if (texture != BaseContent.BadTex && texture.name != "ERRORTEX")
                                return true;
                        }
                    }
                    catch
                    {
                        // Failed to initialize graphic
                    }
                }
                
                // Fifth check: Special case for buildings - they might have different graphic setup
                if (def.building != null)
                {
                    // Buildings might have valid graphics even if not immediately obvious
                    // But we should still filter out ones without any texture path
                    if (def.graphicData != null && !def.graphicData.texPath.NullOrEmpty())
                    {
                        return true; // Allow buildings with texture paths
                    }
                }
                
                // If all checks fail, this item doesn't have valid graphics
                return false;
            }
            catch
            {
                // If any exception occurs, assume no valid graphics
                return false;
            }
        }
        
        private string GetItemCategory(ThingDef item)
        {
            // More comprehensive categorization including modded items
            if (item.IsWeapon) return "Weapons";
            if (item.IsApparel) return "Apparel";
            
            // Food, medicine, drugs with better detection
            if (item.IsIngestible)
            {
                if (item.IsDrug) return "Drugs";
                if (item.IsMedicine) return "Medicine";
                return "Food";
            }
            if (item.IsMedicine) return "Medicine";
            
            // Materials and stuff
            if (item.IsStuff) return "Materials";
            
            // Buildings (including modded structures)
            if (item.building != null || item.thingClass == typeof(MinifiedThing)) return "Buildings";
            
            // Animals and mechanoids
            if (item.race != null) return "Other"; // Could add "Creatures" category if desired
            
            // Plants
            if (item.plant != null) return "Other"; // Could add "Plants" category if desired
            
            // Tools and equipment (items with verbs but not weapons)
            if (item.Verbs != null && item.Verbs.Any() && !item.IsWeapon) return "Tools";
            
            // Art and decorative items
            if (item.thingCategories != null && item.thingCategories.Any(cat => 
                cat.defName.Contains("Art") || cat.defName.Contains("Furniture") || cat.defName.Contains("Decoration")))
                return "Art";
            
            // Resources (raw materials, chunks, etc.)
            if (item.category == ThingCategory.Item) return "Resources";
            
            // Fallback for everything else
            return "Other";
        }
        
        private void DrawItemIcon(ThingDef item, Rect iconRect)
        {
            Texture2D texture = null;
            
            // Try to get icon with multiple fallbacks
            if (item.uiIcon != null)
            {
                texture = item.uiIcon;
            }
            else if (item.graphic?.MatSingle?.mainTexture != null)
            {
                texture = item.graphic.MatSingle.mainTexture as Texture2D;
            }
            else
            {
                try
                {
                    if (item.graphic == null)
                    {
                        item.graphic = item.graphicData?.Graphic;
                    }
                    
                    if (item.graphic?.MatSingle?.mainTexture != null)
                    {
                        texture = item.graphic.MatSingle.mainTexture as Texture2D;
                    }
                    else if (item.uiIcon == null && item.graphicData != null)
                    {
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
        
        private void StartItemTargeting()
        {
            if (selectedItem == null) return;
            
            // Always continuous spawning mode
            Messages.Message($"Select location to spawn {selectedItem.LabelCap} (Continuous - Right-click to cancel)", MessageTypeDefOf.NeutralEvent);
            
            // Close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    var thing = ThingMaker.MakeThing(selectedItem, selectedStuff);
                    
                    // Apply quality if the item supports it
                    if (thing.TryGetComp<CompQuality>() != null)
                    {
                        thing.TryGetComp<CompQuality>().SetQuality(selectedQuality, ArtGenerationContext.Colony);
                    }
                    
                    thing.stackCount = Mathf.Min(spawnQuantity, selectedItem.stackLimit);
                    GenPlace.TryPlaceThing(thing, target.Cell, Find.CurrentMap, ThingPlaceMode.Near);
                    Messages.Message($"Spawned {thing.stackCount}x {thing.LabelCap}", MessageTypeDefOf.PositiveEvent);
                    
                    // Always restart targeting for continuous spawning
                    StartItemTargeting(); // Recursive call to continue targeting
                }
                else
                {
                    Messages.Message("Item spawning cancelled", MessageTypeDefOf.NeutralEvent);
                }
            }, null, null, null);
        }
    }
}