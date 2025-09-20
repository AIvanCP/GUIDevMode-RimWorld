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
        
        // Faction spawning options
        public enum SpawnFaction
        {
            JoinPlayer,
            Friendly,
            Hostile
        }
        private SpawnFaction selectedSpawnFaction = SpawnFaction.Friendly;
        
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
            listing.Label("Spawn as:");
            
            var factionRect = listing.GetRect(25f);
            var joinPlayerRect = new Rect(factionRect.x, factionRect.y, factionRect.width / 3f - 5f, factionRect.height);
            var friendlyRect = new Rect(joinPlayerRect.xMax + 5f, factionRect.y, factionRect.width / 3f - 5f, factionRect.height);
            var hostileRect = new Rect(friendlyRect.xMax + 5f, factionRect.y, factionRect.width / 3f - 5f, factionRect.height);
            
            if (Widgets.RadioButtonLabeled(joinPlayerRect, "Join Colony", selectedSpawnFaction == SpawnFaction.JoinPlayer))
                selectedSpawnFaction = SpawnFaction.JoinPlayer;
            if (Widgets.RadioButtonLabeled(friendlyRect, "Friendly", selectedSpawnFaction == SpawnFaction.Friendly))
                selectedSpawnFaction = SpawnFaction.Friendly;
            if (Widgets.RadioButtonLabeled(hostileRect, "Hostile", selectedSpawnFaction == SpawnFaction.Hostile))
                selectedSpawnFaction = SpawnFaction.Hostile;
            
            listing.Gap(5f);
            
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
                ShowSpawnConfirmation(pawn);
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
                ShowSpawnConfirmation(PawnKindDefOf.Colonist);
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Random Animal"))
            {
                ShowRandomAnimalConfirmation();
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Selected Race"))
            {
                var raceToSpawn = selectedRace ?? PawnKindDefOf.Colonist;
                ShowSpawnConfirmation(raceToSpawn);
            }
            currentX += buttonWidth + 4f;
            
            if (Widgets.ButtonText(new Rect(currentX, rect.y, buttonWidth, rect.height), "Gift Pods"))
            {
                ShowGiftPodsConfirmation();
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
                    Faction faction;
                    switch (selectedSpawnFaction)
                    {
                        case SpawnFaction.JoinPlayer:
                            faction = Faction.OfPlayer;
                            break;
                        case SpawnFaction.Friendly:
                            faction = Find.FactionManager.AllFactions
                                .Where(f => f != Faction.OfPlayer && !f.HostileTo(Faction.OfPlayer) && !f.IsPlayer)
                                .RandomElementWithFallback() ?? Faction.OfPlayer;
                            break;
                        case SpawnFaction.Hostile:
                            faction = Find.FactionManager.RandomEnemyFaction() ?? 
                                Find.FactionManager.AllFactions.Where(f => f.HostileTo(Faction.OfPlayer)).RandomElementWithFallback();
                            break;
                        default:
                            faction = Faction.OfPlayer;
                            break;
                    }
                    
                    var pawn = PawnGenerator.GeneratePawn(pawnKind, faction);
                    GenSpawn.Spawn(pawn, target.Cell, Find.CurrentMap);
                    
                    string factionText = selectedSpawnFaction == SpawnFaction.JoinPlayer ? "colonist" :
                                       selectedSpawnFaction == SpawnFaction.Friendly ? "friendly" : "hostile";
                    Messages.Message($"Spawned {factionText} {pawn.LabelShort}", MessageTypeDefOf.NeutralEvent);
                    
                    // Restart for continuous placement
                    SpawnPawnDirect(pawnKind);
                }
                else
                {
                    Messages.Message("Pawn spawning stopped", MessageTypeDefOf.NeutralEvent);
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
                        Faction faction;
                        switch (selectedSpawnFaction)
                        {
                            case SpawnFaction.JoinPlayer:
                                faction = Faction.OfPlayer;
                                break;
                            case SpawnFaction.Friendly:
                                faction = Find.FactionManager.AllFactions
                                    .Where(f => f != Faction.OfPlayer && !f.HostileTo(Faction.OfPlayer) && !f.IsPlayer)
                                    .RandomElementWithFallback() ?? Faction.OfPlayer;
                                break;
                            case SpawnFaction.Hostile:
                                faction = Find.FactionManager.RandomEnemyFaction() ?? 
                                    Find.FactionManager.AllFactions.Where(f => f.HostileTo(Faction.OfPlayer)).RandomElementWithFallback();
                                break;
                            default:
                                faction = Faction.OfPlayer;
                                break;
                        }
                        
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
                        
                        string factionText = selectedSpawnFaction == SpawnFaction.JoinPlayer ? "colonist" :
                                           selectedSpawnFaction == SpawnFaction.Friendly ? "friendly" : "hostile";
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
                        Faction faction;
                        switch (selectedSpawnFaction)
                        {
                            case SpawnFaction.JoinPlayer:
                                faction = Faction.OfPlayer;
                                break;
                            case SpawnFaction.Friendly:
                                faction = Find.FactionManager.AllFactions
                                    .Where(f => f != Faction.OfPlayer && !f.HostileTo(Faction.OfPlayer) && !f.IsPlayer)
                                    .RandomElementWithFallback() ?? Faction.OfPlayer;
                                break;
                            case SpawnFaction.Hostile:
                                faction = Find.FactionManager.RandomEnemyFaction() ?? 
                                    Find.FactionManager.AllFactions.Where(f => f.HostileTo(Faction.OfPlayer)).RandomElementWithFallback();
                                break;
                            default:
                                faction = Faction.OfPlayer;
                                break;
                        }
                        
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
                        
                        string factionText = selectedSpawnFaction == SpawnFaction.JoinPlayer ? "colonist" :
                                           selectedSpawnFaction == SpawnFaction.Friendly ? "friendly" : "hostile";
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
        
        /// <summary>
        /// Shows confirmation dialog for spawning with description and image
        /// </summary>
        private void ShowSpawnConfirmation(PawnKindDef pawnKind)
        {
            var description = GetPawnDescription(pawnKind);
            var title = $"Spawn {pawnKind.label?.CapitalizeFirst() ?? pawnKind.defName}";
            
            var confirmDialog = new Dialog_MessageBox(
                text: $"{title}\n\n{description}\n\nSpawn method: {(useGiftPods ? "Gift Pods" : "Direct placement")}\nFaction: {GetFactionDescription()}",
                title: title,
                buttonAText: "Spawn",
                buttonAAction: () => {
                    SpawnPawnWithOptions(pawnKind);
                    // Auto-close window after confirmation
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                },
                buttonBText: "Cancel",
                buttonBAction: null
            );
            
            Find.WindowStack.Add(confirmDialog);
        }
        
        /// <summary>
        /// Shows confirmation for random animal spawning
        /// </summary>
        private void ShowRandomAnimalConfirmation()
        {
            var description = "Spawns a random animal from available animal races. The animal will be wild unless tamed through faction settings.";
            
            var confirmDialog = new Dialog_MessageBox(
                text: $"Spawn Random Animal\n\n{description}\n\nSpawn method: {(useGiftPods ? "Gift Pods" : "Direct placement")}\nFaction: {GetFactionDescription()}",
                title: "Spawn Random Animal", 
                buttonAText: "Spawn",
                buttonAAction: () => {
                    SpawnRandomAnimal();
                    // Auto-close window after confirmation
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                },
                buttonBText: "Cancel",
                buttonBAction: null
            );
            
            Find.WindowStack.Add(confirmDialog);
        }
        
        /// <summary>
        /// Shows confirmation for gift pods
        /// </summary>
        private void ShowGiftPodsConfirmation()
        {
            var description = "Spawns gift pods containing valuable items including silver, gold, steel, components, and plasteel. Items will be delivered via drop pods to the target location.";
            
            var confirmDialog = new Dialog_MessageBox(
                text: $"Spawn Gift Pods\n\n{description}\n\nContents: Valuable trade goods and materials",
                title: "Spawn Gift Pods",
                buttonAText: "Drop Pods",
                buttonAAction: () => {
                    SpawnGiftPods();
                    // Auto-close window after confirmation
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                },
                buttonBText: "Cancel",
                buttonBAction: null
            );
            
            Find.WindowStack.Add(confirmDialog);
        }
        
        /// <summary>
        /// Gets description for a pawn type
        /// </summary>
        private string GetPawnDescription(PawnKindDef pawnKind)
        {
            if (pawnKind.RaceProps.Humanlike)
            {
                return $"Human colonist with random traits, skills, and background. Can join your colony and perform all tasks.";
            }
            else if (pawnKind.RaceProps.Animal)
            {
                var tameable = "Tameable status unknown"; // Simplified for compatibility
                var bodySize = pawnKind.RaceProps.baseBodySize < 0.5f ? "Small" : 
                              pawnKind.RaceProps.baseBodySize < 1.5f ? "Medium" : "Large";
                return $"Animal - {bodySize} sized. {tameable}.";
            }
            else if (pawnKind.RaceProps.IsMechanoid)
            {
                return $"Mechanoid unit. Hostile by default unless faction is set to friendly.";
            }
            else
            {
                return $"Special pawn type: {pawnKind.defName}";
            }
        }
        
        /// <summary>
        /// Gets faction description for spawning
        /// </summary>
        private string GetFactionDescription()
        {
            switch (selectedSpawnFaction)
            {
                case SpawnFaction.JoinPlayer:
                    return "Player faction (will join your colony)";
                case SpawnFaction.Friendly:
                    return "Friendly (will not attack your colonists)";
                case SpawnFaction.Hostile:
                    return "Hostile (will attack your colonists)";
                default:
                    return "Unknown faction";
            }
        }
    }
}