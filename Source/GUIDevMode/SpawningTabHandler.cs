using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class SpawningTabHandler
    {
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 pawnsScrollPosition = Vector2.zero;
        private string selectedPawnCategory = "";
        private string pawnSearchFilter = "";
        private PawnKindDef selectedRace = null;
        private bool spawnAsFriendly = true;
        
        // Gift pods settings
        private bool useGiftPods = false;
        private bool giftPodsWithItems = true;
        
        public void DrawSpawningTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Pawn Spawning Options:");
            listing.Gap(5f);
            
            // Faction selection
            listing.CheckboxLabeled("Spawn as friendly (unchecked = hostile)", ref spawnAsFriendly);
            listing.CheckboxLabeled("Use gift pods for delivery", ref useGiftPods);
            
            if (useGiftPods)
            {
                listing.CheckboxLabeled("Include items in gift pods", ref giftPodsWithItems);
            }
            
            listing.Gap(8f);
            listing.End();
            
            // Split into categories and pawns
            var remainingRect = new Rect(rect.x, rect.y + 100, rect.width, rect.height - 140);
            var categoriesRect = new Rect(remainingRect.x, remainingRect.y, remainingRect.width * 0.3f, remainingRect.height);
            var pawnsRect = new Rect(categoriesRect.xMax + 8f, remainingRect.y, remainingRect.width * 0.7f - 8f, remainingRect.height);
            
            DrawPawnCategoriesSection(categoriesRect);
            DrawPawnsSection(pawnsRect);
            
            // Quick spawn buttons at bottom
            var bottomRect = new Rect(rect.x, rect.yMax - 35, rect.width, 30);
            DrawQuickSpawnButtons(bottomRect);
        }
        
        private void DrawPawnCategoriesSection(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Pawn Categories:");
            
            // Categories list
            var categoriesListRect = listing.GetRect(rect.height - 40f);
            var viewRect = new Rect(0, 0, categoriesListRect.width - 16f, CacheManager.AllPawnCategories.Count * 25f);
            
            Widgets.BeginScrollView(categoriesListRect, ref scrollPosition, viewRect);
            
            var y = 0f;
            foreach (var category in CacheManager.AllPawnCategories)
            {
                var categoryRect = new Rect(0, y, viewRect.width, 24f);
                var isSelected = selectedPawnCategory == category;
                
                if (isSelected)
                    Widgets.DrawHighlight(categoryRect);
                
                var pawnCount = CacheManager.GetPawnsByCategory(category).Count;
                var labelText = $"{category} ({pawnCount})";
                
                if (Widgets.ButtonText(categoryRect, labelText, false))
                {
                    selectedPawnCategory = category;
                    pawnsScrollPosition = Vector2.zero; // Reset pawn scroll when category changes
                }
                
                y += 25f;
            }
            
            Widgets.EndScrollView();
            listing.End();
        }
        
        private void DrawPawnsSection(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            // Header with selected category
            if (string.IsNullOrEmpty(selectedPawnCategory))
            {
                listing.Label("Select a category to view pawns");
                listing.End();
                return;
            }
            
            listing.Label($"Pawns in: {selectedPawnCategory}");
            
            // Search filter
            var searchRect = listing.GetRect(30f);
            Widgets.Label(searchRect.LeftPart(0.15f), "Filter:");
            pawnSearchFilter = Widgets.TextField(searchRect.RightPart(0.85f), pawnSearchFilter);
            
            listing.Gap(8f);
            
            // Pawns list
            var pawns = CacheManager.GetPawnsByCategory(selectedPawnCategory);
            if (!string.IsNullOrEmpty(pawnSearchFilter))
            {
                pawns = pawns.Where(pawn => 
                    (pawn.label?.ToLower().Contains(pawnSearchFilter.ToLower()) ?? false) ||
                    pawn.defName.ToLower().Contains(pawnSearchFilter.ToLower())).ToList();
            }
            
            var pawnsListRect = listing.GetRect(rect.height - 80f);
            var viewRect = new Rect(0, 0, pawnsListRect.width - 16f, pawns.Count * 25f);
            
            Widgets.BeginScrollView(pawnsListRect, ref pawnsScrollPosition, viewRect);
            
            var y = 0f;
            foreach (var pawn in pawns)
            {
                var pawnRect = new Rect(0, y, viewRect.width, 24f);
                DrawPawnButton(pawnRect, pawn);
                y += 25f;
            }
            
            Widgets.EndScrollView();
            
            listing.End();
        }
        
        private void DrawPawnButton(Rect rect, PawnKindDef pawn)
        {
            var buttonRect = rect.LeftPart(0.8f);
            var detailsRect = rect.RightPart(0.2f);
            
            var labelText = pawn.label?.CapitalizeFirst() ?? pawn.defName;
            if (Widgets.ButtonText(buttonRect, labelText))
            {
                selectedRace = pawn;
                SpawnPawnWithOptions(pawn);
            }
            
            // Quick info
            var infoText = pawn.RaceProps.Humanlike ? "H" : 
                          pawn.RaceProps.Animal ? "A" : 
                          pawn.RaceProps.IsMechanoid ? "M" : "?";
            Widgets.Label(detailsRect, infoText);
            
            // Tooltip
            if (Mouse.IsOver(rect))
            {
                var tooltip = $"{pawn.label}\n{pawn.race?.description ?? ""}";
                if (pawn.modContentPack?.Name != null)
                    tooltip += $"\nMod: {pawn.modContentPack.Name}";
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }
        
        private void DrawQuickSpawnButtons(Rect rect)
        {
            var buttonWidth = rect.width / 4f - 2f;
            var currentX = rect.x;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Human"))
            {
                SpawnPawnWithOptions(PawnKindDefOf.Colonist);
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Random Animal"))
            {
                SpawnRandomAnimal();
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Selected Race"))
            {
                var raceToSpawn = selectedRace ?? PawnKindDefOf.Colonist;
                SpawnPawnWithOptions(raceToSpawn);
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Gift Pods"))
            {
                SpawnGiftPods();
            }
        }
        
        private void SpawnPawnWithOptions(PawnKindDef pawnKind)
        {
            if (useGiftPods)
            {
                SpawnPawnWithGiftPods(pawnKind);
            }
            else
            {
                SpawnPawnDirect(pawnKind);
            }
        }
        
        private void SpawnPawnDirect(PawnKindDef pawnKind)
        {
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    var faction = spawnAsFriendly ? Faction.OfPlayer : Find.FactionManager.RandomEnemyFaction();
                    var pawn = PawnGenerator.GeneratePawn(pawnKind, faction);
                    GenSpawn.Spawn(pawn, target.Cell, Find.CurrentMap);
                    
                    var factionText = spawnAsFriendly ? "friendly" : "hostile";
                    Messages.Message($"Spawned {factionText} {pawn.LabelShort}", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        private void SpawnPawnWithGiftPods(PawnKindDef pawnKind)
        {
            // Check for drop zones first
            var dropZones = Find.CurrentMap.zoneManager.AllZones.OfType<Zone_Stockpile>()
                .Where(z => z.GetSlotGroup()?.Settings?.filter?.Allows(ThingDefOf.Silver) == true).ToList();
            
            if (dropZones.Any())
            {
                // Use drop zone targeting like orbital traders
                var targetingParams = TargetingParameters.ForDropPodsDestination();
                Find.Targeter.BeginTargeting(targetingParams, target => {
                    if (target.IsValid)
                    {
                        var faction = spawnAsFriendly ? Faction.OfPlayer : Find.FactionManager.RandomEnemyFaction();
                        var pawn = PawnGenerator.GeneratePawn(pawnKind, faction);
                        
                        var pods = new List<Thing> { pawn };
                        
                        if (giftPodsWithItems)
                        {
                            // Add some random items to the gift pods
                            var items = new[] { ThingDefOf.Silver, ThingDefOf.Steel, ThingDefOf.ComponentIndustrial };
                            foreach (var itemDef in items)
                            {
                                var item = ThingMaker.MakeThing(itemDef);
                                item.stackCount = Rand.Range(10, 50);
                                pods.Add(item);
                            }
                        }
                        
                        // Create visual landing indicator
                        CreateDropPodLandingIndicator(target.Cell);
                        
                        DropPodUtility.DropThingsNear(target.Cell, Find.CurrentMap, pods);
                        
                        var factionText = spawnAsFriendly ? "friendly" : "hostile";
                        Messages.Message($"Gift pods delivered {factionText} {pawn.LabelShort} to drop zone", MessageTypeDefOf.PositiveEvent);
                    }
                }, null, delegate {
                    // Draw drop zone highlighting
                    DrawDropZoneHighlights();
                });
            }
            else
            {
                // Fallback to cell targeting if no drop zones
                Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                    if (target.IsValid)
                    {
                        var faction = spawnAsFriendly ? Faction.OfPlayer : Find.FactionManager.RandomEnemyFaction();
                        var pawn = PawnGenerator.GeneratePawn(pawnKind, faction);
                        
                        var pods = new List<Thing> { pawn };
                        
                        if (giftPodsWithItems)
                        {
                            var items = new[] { ThingDefOf.Silver, ThingDefOf.Steel, ThingDefOf.ComponentIndustrial };
                            foreach (var itemDef in items)
                            {
                                var item = ThingMaker.MakeThing(itemDef);
                                item.stackCount = Rand.Range(10, 50);
                                pods.Add(item);
                            }
                        }
                        
                        CreateDropPodLandingIndicator(target.Cell);
                        DropPodUtility.DropThingsNear(target.Cell, Find.CurrentMap, pods);
                        
                        var factionText = spawnAsFriendly ? "friendly" : "hostile";
                        Messages.Message($"Gift pods delivered {factionText} {pawn.LabelShort} (no drop zone found)", MessageTypeDefOf.PositiveEvent);
                    }
                });
            }
        }
        
        private void SpawnRandomAnimal()
        {
            var animals = CacheManager.GetPawnsByCategory("Animals");
            if (animals.Any())
            {
                var randomAnimal = animals.RandomElement();
                SpawnPawnWithOptions(randomAnimal);
            }
            else
            {
                Messages.Message("No animals found in cache", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void SpawnGiftPods()
        {
            // Check for drop zones first
            var dropZones = Find.CurrentMap.zoneManager.AllZones.OfType<Zone_Stockpile>()
                .Where(z => z.GetSlotGroup()?.Settings?.filter?.Allows(ThingDefOf.Silver) == true).ToList();
            
            if (dropZones.Any())
            {
                // Use drop zone targeting like orbital traders
                var targetingParams = TargetingParameters.ForDropPodsDestination();
                Find.Targeter.BeginTargeting(targetingParams, target => {
                    if (target.IsValid)
                    {
                        var pods = new List<Thing>();
                        
                        // Add random valuable items
                        var valuableItems = new[] 
                        { 
                            ThingDefOf.Silver, ThingDefOf.Gold, ThingDefOf.Steel, 
                            ThingDefOf.ComponentIndustrial, ThingDefOf.Plasteel 
                        };
                        
                        foreach (var itemDef in valuableItems.Take(3))
                        {
                            var item = ThingMaker.MakeThing(itemDef);
                            item.stackCount = Rand.Range(20, 100);
                            pods.Add(item);
                        }
                        
                        CreateDropPodLandingIndicator(target.Cell);
                        DropPodUtility.DropThingsNear(target.Cell, Find.CurrentMap, pods);
                        Messages.Message("Gift pods delivered to drop zone with valuable items", MessageTypeDefOf.PositiveEvent);
                    }
                }, null, delegate {
                    DrawDropZoneHighlights();
                });
            }
            else
            {
                // Fallback to cell targeting if no drop zones
                Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                    if (target.IsValid)
                    {
                        var pods = new List<Thing>();
                        
                        var valuableItems = new[] 
                        { 
                            ThingDefOf.Silver, ThingDefOf.Gold, ThingDefOf.Steel, 
                            ThingDefOf.ComponentIndustrial, ThingDefOf.Plasteel 
                        };
                        
                        foreach (var itemDef in valuableItems.Take(3))
                        {
                            var item = ThingMaker.MakeThing(itemDef);
                            item.stackCount = Rand.Range(20, 100);
                            pods.Add(item);
                        }
                        
                        CreateDropPodLandingIndicator(target.Cell);
                        DropPodUtility.DropThingsNear(target.Cell, Find.CurrentMap, pods);
                        Messages.Message("Gift pods delivered with valuable items (no drop zone found)", MessageTypeDefOf.PositiveEvent);
                    }
                });
            }
        }
        
        private void CreateDropPodLandingIndicator(IntVec3 cell)
        {
            // Create a visual indicator for where the drop pod will land
            try
            {
                // Try to create a flash effect
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), Find.CurrentMap, 2f);
            }
            catch
            {
                // Fallback to dust puff if lightning glow fails
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), Find.CurrentMap, 2f, Color.yellow);
            }
            
            // Also create a landing marker that lasts longer
            FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), Find.CurrentMap, 3f, Color.green);
        }
        
        private void DrawDropZoneHighlights()
        {
            var dropZones = Find.CurrentMap.zoneManager.AllZones.OfType<Zone_Stockpile>()
                .Where(z => z.GetSlotGroup()?.Settings?.filter?.Allows(ThingDefOf.Silver) == true);
                
            foreach (var zone in dropZones)
            {
                var cells = zone.Cells.ToList();
                if (cells.Any())
                {
                    var highlightColor = Color.green;
                    highlightColor.a = 0.3f;
                    GenDraw.DrawFieldEdges(cells, highlightColor);
                }
            }
        }
    }
}