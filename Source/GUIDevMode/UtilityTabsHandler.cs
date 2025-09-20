using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class UtilityTabsHandler
    {
        // Research tab data
        private Vector2 researchScrollPos = Vector2.zero;
        private string researchSearchText = "";
        private bool showUnresearchedOnly = true; // Show unresearched by default
        
        // Trading tab data
        private Vector2 tradingScrollPos = Vector2.zero;
        private bool spawnOrbitalTrader = true;
        
        // Utilities tab data
        private Vector2 utilitiesScrollPos = Vector2.zero;
        private bool confirmCleanMap = false;
        
        // Factions tab data
        private Vector2 factionsScrollPos = Vector2.zero;
        
        // Incidents tab data
        private Vector2 incidentsScrollPos = Vector2.zero;
        private bool showRaidIncidentsOnly = false;
        
        public void DrawResearchTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Research Manager:");
            listing.Gap(5f);
            
            // Filter toggle - allow showing all research or just unresearched
            var filterRect = listing.GetRect(24f);
            var newShowUnresearched = showUnresearchedOnly;
            Widgets.CheckboxLabeled(filterRect, "Show only unresearched projects", ref newShowUnresearched);
            showUnresearchedOnly = newShowUnresearched;
            listing.Gap(5f);
            
            // Search
            listing.Label("Search research:");
            var newSearchText = listing.TextEntry(researchSearchText);
            if (newSearchText != researchSearchText)
            {
                researchSearchText = newSearchText;
            }
            
            listing.Gap(5f);
            
            // Quick actions
            if (listing.ButtonText("Complete Current Research"))
            {
                CompleteCurrentResearch();
            }
            if (listing.ButtonText("Complete All Research"))
            {
                CompleteAllResearch();
            }
            if (listing.ButtonText("Reset All Research"))
            {
                ResetAllResearch();
            }
            
            listing.Gap(10f);
            
            var currentY = listing.CurHeight;
            listing.End();
            
            // Research list with filtering - use absolute positioning to prevent overlap
            var scrollRect = new Rect(rect.x, rect.y + currentY, rect.width, rect.height - currentY);
            var allResearch = DefDatabase<ResearchProjectDef>.AllDefs
                .Where(research => {
                    // Apply unresearched filter if enabled
                    if (showUnresearchedOnly && research.IsFinished)
                        return false;
                    
                    // Apply search filter
                    if (!string.IsNullOrEmpty(researchSearchText) && 
                        !research.label.ToLower().Contains(researchSearchText.ToLower()))
                        return false;
                    
                    return true;
                })
                .OrderBy(research => research.IsFinished ? 1 : 0) // Put unfinished first
                .ThenBy(research => research.label);
            
            var contentHeight = allResearch.Count() * 35f;
            
            Widgets.BeginScrollView(scrollRect, ref researchScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var research in allResearch)
            {
                var researchRect = new Rect(0, y, scrollRect.width - 16f, 30f);
                DrawResearchEntry(researchRect, research);
                y += 35f;
            }
            
            Widgets.EndScrollView();
        }
        
        public void DrawTradingTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Trading & Economy:");
            listing.Gap(5f);
            
            listing.CheckboxLabeled("Spawn orbital traders (vs caravan)", ref spawnOrbitalTrader);
            listing.Gap(5f);
            
            listing.Label("Economy Actions:");
            if (listing.ButtonText("Add 1000 Silver"))
                AddSilver(1000);
            if (listing.ButtonText("Add 10000 Silver"))
                AddSilver(10000);
            if (listing.ButtonText("Add 1000 Components"))
                AddComponents(1000);
            if (listing.ButtonText("Add Random Trade Goods"))
                AddRandomTradeGoods();
            if (listing.ButtonText("Call Random Trader"))
                CallRandomTrader();
                
            listing.Gap(10f);
            listing.Label("Available Traders (Click to Spawn):");
            
            var currentY = listing.CurHeight;
            listing.End();
            
            // Scrollable trader list - use absolute positioning
            var scrollRect = new Rect(rect.x, rect.y + currentY, rect.width, rect.height - currentY);
            var allTraders = DefDatabase<TraderKindDef>.AllDefs
                .OrderBy(t => t.label)
                .ToList();
                
            var contentHeight = allTraders.Count * 70f; // More height for details
            
            Widgets.BeginScrollView(scrollRect, ref tradingScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var trader in allTraders)
            {
                var traderRect = new Rect(0, y, scrollRect.width - 16f, 65f);
                DrawTraderEntry(traderRect, trader);
                y += 70f;
            }
            
            Widgets.EndScrollView();
        }
        
        public void DrawUtilitiesTab(Rect rect)
        {
            // Calculate content height for scrolling
            float contentHeight = 600f; // Estimated height for all utility content
            
            var scrollRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            Widgets.BeginScrollView(scrollRect, ref utilitiesScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            var listing = new Listing_Standard();
            listing.Begin(new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            listing.Label("Map & Colony Utilities:");
            listing.Gap(10f);
            
            // Map actions
            listing.Label("Map Actions:");
            if (listing.ButtonText("Reveal Entire Map"))
                RevealEntireMap();
            if (listing.ButtonText("Clear All Fog"))
                ClearAllFog();
            if (listing.ButtonText("Generate New Map Area"))
                GenerateNewMapArea();
                
            listing.Gap(8f);
            
            // Dangerous actions
            listing.Label("Dangerous Actions:");
            if (!confirmCleanMap)
            {
                if (listing.ButtonText("Clean Map (Click to Confirm)"))
                    confirmCleanMap = true;
            }
            else
            {
                GUI.color = Color.red;
                if (listing.ButtonText("CONFIRM: Clean All Things from Map"))
                {
                    CleanMap();
                    confirmCleanMap = false;
                }
                GUI.color = Color.white;
                if (listing.ButtonText("Cancel"))
                    confirmCleanMap = false;
            }
            
            listing.Gap(8f);
            
            // Colony utilities
            listing.Label("Colony Utilities:");
            if (listing.ButtonText("Heal All Colonists"))
                HealAllColonists();
            if (listing.ButtonText("Fill All Needs"))
                FillAllNeeds();
            if (listing.ButtonText("Boost All Skills"))
                BoostAllSkills();
            if (listing.ButtonText("Remove All Negative Thoughts"))
                RemoveNegativeThoughts();
            if (listing.ButtonText("Clear All Conditions"))
                ClearAllConditions();
            if (listing.ButtonText("Clean Filth from Map"))
                CleanFilthFromMap();
                
            listing.Gap(8f);
            
            // Plant utilities
            listing.Label("Plant & Agriculture:");
            if (listing.ButtonText("Grow All Plants (Full Map)"))
                GrowAllPlantsOnMap();
            if (listing.ButtonText("Grow Plants in Area (Select Zone)"))
                StartAreaPlantGrowth();
                
            listing.Gap(8f);
            
            // Aggressive actions
            listing.Label("Aggressive Actions:");
            if (listing.ButtonText("Make Colonist Berserk (Select Target)"))
                StartBerserkTargeting();
            if (listing.ButtonText("Make Animal Attack Everything (Select Target)"))
                StartAnimalAttackTargeting();
            if (listing.ButtonText("Target Colonist Hunger (Select Target)"))
                StartHungerTargeting();
                
            listing.End();
            Widgets.EndScrollView();
        }
        
        public void DrawFactionsTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Faction Relations Manager:");
            listing.Gap(5f);
            listing.End();
            
            var scrollRect = new Rect(rect.x, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            var factions = Find.FactionManager.AllFactions.Where(f => f != null && !f.IsPlayer).ToList();
            var contentHeight = factions.Count * 120f; // Increased height for extra buttons
            
            Widgets.BeginScrollView(scrollRect, ref factionsScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var faction in factions)
            {
                var factionRect = new Rect(0, y, scrollRect.width - 16f, 115f); // Increased height
                DrawFactionEntry(factionRect, faction);
                y += 120f; // Increased spacing
            }
            
            Widgets.EndScrollView();
        }
        
        public void DrawIncidentsTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            listing.Label("Incident Manager:");
            listing.Gap(5f);
            
            listing.CheckboxLabeled("Show raid incidents only", ref showRaidIncidentsOnly);
            listing.Gap(10f); // More space to prevent overlap
            
            var currentY = listing.CurHeight;
            listing.End();
            
            // Ensure proper spacing by using absolute positioning
            var scrollRect = new Rect(rect.x, rect.y + currentY, rect.width, rect.height - currentY);
            var incidents = DefDatabase<IncidentDef>.AllDefs
                .Where(inc => !showRaidIncidentsOnly || inc.category == IncidentCategoryDefOf.ThreatBig)
                .OrderBy(inc => inc.category.label)
                .ThenBy(inc => inc.label);
                
            // Calculate height for expanded incident entries (more space for descriptions)
            var contentHeight = incidents.Count() * 55f; // Increased height per entry
            
            Widgets.BeginScrollView(scrollRect, ref incidentsScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var incident in incidents)
            {
                var incidentRect = new Rect(0, y, scrollRect.width - 16f, 50f); // Taller entries
                DrawIncidentEntry(incidentRect, incident);
                y += 55f; // Increased spacing
            }
            
            Widgets.EndScrollView();
        }
        
        // Helper methods
        private void DrawResearchEntry(Rect rect, ResearchProjectDef research)
        {
            var isCompleted = research.IsFinished;
            
            if (isCompleted)
                GUI.color = Color.green;
            else
                GUI.color = Color.white;
                
            if (Widgets.ButtonText(rect.LeftPart(0.7f), research.label))
            {
                if (isCompleted)
                {
                    // Debug approach: reset by finishing incomplete research
                    Log.Warning($"Resetting research progress for {research.label} not supported - would need to reset save data");
                }
                else
                    FinishResearch(research);
            }
            
            GUI.color = Color.white;
            
            var statusText = isCompleted ? "Completed" : "Incomplete";
            Widgets.Label(rect.RightPart(0.3f), statusText);
        }
        
        private void DrawTraderEntry(Rect rect, TraderKindDef trader)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5);
            
            // Trader name and spawn button
            var nameRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 25f);
            var buttonRect = new Rect(innerRect.x + innerRect.width * 0.75f, innerRect.y, innerRect.width * 0.25f, 25f);
            
            Widgets.Label(nameRect, trader.label);
            if (Widgets.ButtonText(buttonRect, "Spawn"))
            {
                SpawnTrader(trader);
            }
            
            // Details about what they sell/buy
            var detailsRect = new Rect(innerRect.x, innerRect.y + 28f, innerRect.width, 30f);
            var details = GetTraderDetails(trader);
            GUI.color = Color.gray;
            Text.Font = GameFont.Tiny;
            Widgets.Label(detailsRect, details);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
        }
        
        private void DrawFactionEntry(Rect rect, Faction faction)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5);
            
            var listing = new Listing_Standard();
            listing.Begin(innerRect);
            
            listing.Label($"{faction.Name} ({faction.def.label})");
            listing.Label($"Goodwill: {faction.PlayerGoodwill}");
            
            // Row 1: +/-10 buttons
            var buttonRect1 = listing.GetRect(25f);
            if (Widgets.ButtonText(buttonRect1.LeftHalf(), "+10 Goodwill"))
                ChangeFactionGoodwill(faction, 10);
            if (Widgets.ButtonText(buttonRect1.RightHalf(), "-10 Goodwill"))
                ChangeFactionGoodwill(faction, -10);
                
            // Row 2: Extreme options
            var buttonRect2 = listing.GetRect(25f);
            if (Widgets.ButtonText(buttonRect2.LeftHalf(), "Make Allied"))
                ChangeFactionGoodwill(faction, 200); // Set high goodwill for allied status
            if (Widgets.ButtonText(buttonRect2.RightHalf(), "Make Hostile"))
                ChangeFactionGoodwill(faction, -200); // Set low goodwill for hostile status
                
            listing.End();
        }
        
        private void DrawIncidentEntry(Rect rect, IncidentDef incident)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(3);
            
            // Title and category row
            var titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 20f);
            var buttonRect = new Rect(innerRect.x + innerRect.width * 0.75f, innerRect.y, innerRect.width * 0.25f, 20f);
            
            // Color code by incident category
            GUI.color = GetIncidentCategoryColor(incident.category);
            Widgets.Label(titleRect, $"{incident.label} ({incident.category.label})");
            GUI.color = Color.white;
            
            // Trigger button
            if (Widgets.ButtonText(buttonRect, "Trigger"))
            {
                TriggerIncident(incident);
            }
            
            // Description row
            var descRect = new Rect(innerRect.x, innerRect.y + 22f, innerRect.width, 22f);
            GUI.color = Color.gray;
            var description = GetIncidentDescription(incident);
            Widgets.Label(descRect, description);
            GUI.color = Color.white;
        }
        
        private Color GetIncidentCategoryColor(IncidentCategoryDef category)
        {
            if (category == IncidentCategoryDefOf.ThreatBig || category == IncidentCategoryDefOf.ThreatSmall)
                return Color.red;
            if (category.defName == "FactionArrival")
                return Color.yellow;
            if (category == IncidentCategoryDefOf.Misc)
                return Color.cyan;
            if (category.defName.Contains("Disease"))
                return new Color(1f, 0.5f, 0f); // Orange
            
            return Color.white;
        }
        
        private string GetIncidentDescription(IncidentDef incident)
        {
            // Try to get description from various sources
            if (!string.IsNullOrEmpty(incident.description))
                return incident.description;
            
            // Generate description based on incident type and name
            var defName = incident.defName.ToLowerInvariant();
            var label = incident.label.ToLowerInvariant();
            
            // Threat descriptions
            if (incident.category == IncidentCategoryDefOf.ThreatBig)
            {
                if (defName.Contains("raid")) return "Large hostile force attacks your colony";
                if (defName.Contains("siege")) return "Enemies set up siege equipment and attack";
                if (defName.Contains("drop")) return "Hostile forces arrive via drop pods";
                if (defName.Contains("mech")) return "Mechanoid cluster threatens the area";
                if (defName.Contains("infestation")) return "Insect infestation spawns underground";
                return "Major threat to colony";
            }
            
            if (incident.category == IncidentCategoryDefOf.ThreatSmall)
            {
                if (defName.Contains("manhunter")) return "Animals turn hostile and hunt colonists";
                if (defName.Contains("raid")) return "Small hostile force attacks";
                if (defName.Contains("predator")) return "Dangerous predators appear on the map";
                return "Minor threat to colony";
            }
            
            // Positive events
            if (defName.Contains("refugee")) return "Refugee arrives asking for help";
            if (defName.Contains("trader")) return "Trading opportunity arrives";
            if (defName.Contains("gift")) return "Friendly faction sends gifts";
            if (defName.Contains("join")) return "Wanderer wants to join colony";
            
            // Environmental events
            if (defName.Contains("fire")) return "Fire breaks out on the map";
            if (defName.Contains("weather")) return "Weather pattern changes";
            if (defName.Contains("eclipse")) return "Solar eclipse blocks solar power";
            if (defName.Contains("fallout")) return "Toxic fallout affects the region";
            if (defName.Contains("cold")) return "Cold snap lowers temperatures";
            if (defName.Contains("heat")) return "Heat wave raises temperatures";
            
            // Diseases
            if (incident.category.defName.Contains("Disease"))
                return "Disease affects the colony";
            
            // Faction events
            if (incident.category.defName == "FactionArrival")
                return "New faction arrives in the area";
            
            // Default description
            return $"Event: {incident.label}";
        }
        
        // Action methods
        private void CompleteCurrentResearch()
        {
            if (Find.ResearchManager.GetProject() != null)
            {
                var currentProject = Find.ResearchManager.GetProject();
                FinishResearch(currentProject);
                Messages.Message($"Completed research: {currentProject.label}", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("No research in progress", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void CompleteAllResearch()
        {
            foreach (var research in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                FinishResearch(research);
            }
            Messages.Message("Completed all research", MessageTypeDefOf.PositiveEvent);
        }
        
        private void ResetAllResearch()
        {
            Log.Warning("Reset all research not supported - would require game restart or mod support");
            Messages.Message("Research reset not supported in current API", MessageTypeDefOf.RejectInput);
        }
        
        private void FinishResearch(ResearchProjectDef research)
        {
            Find.ResearchManager.FinishProject(research);
        }
        
        private void SpawnTrader(TraderKindDef traderKind)
        {
            try
            {
                if (Find.CurrentMap == null)
                {
                    Messages.Message("No active map to spawn trader", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (spawnOrbitalTrader)
                {
                    // Create proper incident parameters for orbital trader
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                    parms.target = Find.CurrentMap;
                    parms.traderKind = traderKind;
                    
                    var incident = IncidentDefOf.OrbitalTraderArrival;
                    if (incident.Worker.CanFireNow(parms))
                    {
                        incident.Worker.TryExecute(parms);
                        Messages.Message($"Orbital {traderKind.label} trader called", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Cannot spawn orbital trader right now", MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    // Create proper incident parameters for caravan trader
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                    parms.target = Find.CurrentMap;
                    parms.traderKind = traderKind;
                    
                    var incident = IncidentDefOf.TraderCaravanArrival;
                    if (incident.Worker.CanFireNow(parms))
                    {
                        incident.Worker.TryExecute(parms);
                        Messages.Message($"{traderKind.label} caravan called", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Cannot spawn trader caravan right now", MessageTypeDefOf.RejectInput);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Error spawning trader {traderKind?.defName}: {ex.Message}");
                Messages.Message("Failed to spawn trader - check logs for details", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void CallRandomTrader()
        {
            var allTraders = DefDatabase<TraderKindDef>.AllDefs.ToList();
            if (allTraders.Any())
            {
                var randomTrader = allTraders.RandomElement();
                SpawnTrader(randomTrader);
            }
            else
            {
                Messages.Message("No traders available", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void AddSilver(int amount)
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = amount;
            GenPlace.TryPlaceThing(silver, Find.AnyPlayerHomeMap.AllCells.RandomElement(), Find.AnyPlayerHomeMap, ThingPlaceMode.Near);
            Messages.Message($"Added {amount} silver", MessageTypeDefOf.PositiveEvent);
        }
        
        private void AddComponents(int amount)
        {
            var components = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            components.stackCount = amount;
            GenPlace.TryPlaceThing(components, Find.AnyPlayerHomeMap.AllCells.RandomElement(), Find.AnyPlayerHomeMap, ThingPlaceMode.Near);
            Messages.Message($"Added {amount} components", MessageTypeDefOf.PositiveEvent);
        }
        
        private void AddRandomTradeGoods()
        {
            var items = new[] { ThingDefOf.Gold, ThingDefOf.Jade, ThingDefOf.Plasteel, ThingDefOf.Uranium };
            foreach (var itemDef in items.Take(2))
            {
                var item = ThingMaker.MakeThing(itemDef);
                item.stackCount = Rand.Range(10, 50);
                GenPlace.TryPlaceThing(item, Find.AnyPlayerHomeMap.AllCells.RandomElement(), Find.AnyPlayerHomeMap, ThingPlaceMode.Near);
            }
            Messages.Message("Added random trade goods", MessageTypeDefOf.PositiveEvent);
        }
        
        private void RevealEntireMap()
        {
            foreach (var cell in Find.CurrentMap.AllCells)
            {
                Find.CurrentMap.fogGrid.Unfog(cell);
            }
            Messages.Message("Revealed entire map", MessageTypeDefOf.NeutralEvent);
        }
        
        private void ClearAllFog()
        {
            Find.CurrentMap.fogGrid.ClearAllFog();
            Messages.Message("Cleared all fog", MessageTypeDefOf.NeutralEvent);
        }
        
        private void GenerateNewMapArea()
        {
            Messages.Message("Map area generation not implemented", MessageTypeDefOf.RejectInput);
        }
        
        private void CleanMap()
        {
            var things = Find.CurrentMap.listerThings.AllThings.ToList();
            foreach (var thing in things)
            {
                if (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Building)
                {
                    thing.Destroy();
                }
            }
            Messages.Message("Cleaned all things from map", MessageTypeDefOf.NeutralEvent);
        }
        
        private void HealAllColonists()
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                pawn.health.Reset();
            }
            Messages.Message("Healed all colonists", MessageTypeDefOf.PositiveEvent);
        }
        
        private void FillAllNeeds()
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn.needs != null)
                {
                    foreach (var need in pawn.needs.AllNeeds)
                    {
                        need.CurLevel = need.MaxLevel;
                    }
                }
            }
            Messages.Message("Filled all colonist needs", MessageTypeDefOf.PositiveEvent);
        }
        
        private void BoostAllSkills()
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn.skills != null)
                {
                    foreach (var skill in pawn.skills.skills)
                    {
                        skill.Level = 20;
                        skill.xpSinceLastLevel = 0;
                    }
                }
            }
            Messages.Message("Boosted all colonist skills to level 20", MessageTypeDefOf.PositiveEvent);
        }
        
        private void RemoveNegativeThoughts()
        {
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
            {
                if (pawn.needs?.mood?.thoughts?.memories != null)
                {
                    var negativeThoughts = pawn.needs.mood.thoughts.memories.Memories
                        .Where(m => m.MoodOffset() < 0).ToList();
                    foreach (var thought in negativeThoughts)
                    {
                        pawn.needs.mood.thoughts.memories.RemoveMemory(thought);
                    }
                }
            }
            Messages.Message("Removed all negative thoughts", MessageTypeDefOf.PositiveEvent);
        }
        
        private string GetTraderDetails(TraderKindDef traderKind)
        {
            var details = $"Trader: {traderKind.label}\n\n";
            
            // Get stock generators to show what they typically buy/sell
            if (traderKind.stockGenerators != null && traderKind.stockGenerators.Any())
            {
                details += "Stock Categories:\n";
                foreach (var stockGen in traderKind.stockGenerators)
                {
                    try
                    {
                        if (stockGen.GetType().Name == "StockGenerator_Category")
                        {
                            // Use reflection safely to get category info
                            var categoryField = stockGen.GetType().GetField("categoryDef");
                            if (categoryField != null)
                            {
                                var categoryDef = categoryField.GetValue(stockGen);
                                if (categoryDef != null)
                                {
                                    var labelProp = categoryDef.GetType().GetProperty("label");
                                    if (labelProp != null)
                                    {
                                        details += $"• {labelProp.GetValue(categoryDef) ?? "Unknown Category"}\n";
                                        continue;
                                    }
                                }
                            }
                            details += "• Category Items\n";
                        }
                        else if (stockGen.GetType().Name == "StockGenerator_SingleDef")
                        {
                            // Use reflection safely to get thing info
                            var thingField = stockGen.GetType().GetField("thingDef");
                            if (thingField != null)
                            {
                                var thingDef = thingField.GetValue(stockGen);
                                if (thingDef != null)
                                {
                                    var labelProp = thingDef.GetType().GetProperty("label");
                                    if (labelProp != null)
                                    {
                                        details += $"• {labelProp.GetValue(thingDef) ?? "Unknown Item"}\n";
                                        continue;
                                    }
                                }
                            }
                            details += "• Specific Item\n";
                        }
                        else if (stockGen.GetType().Name == "StockGenerator_Animals")
                        {
                            details += "• Animals\n";
                        }
                        else
                        {
                            details += $"• {stockGen.GetType().Name.Replace("StockGenerator_", "")}\n";
                        }
                    }
                    catch
                    {
                        details += $"• {stockGen.GetType().Name.Replace("StockGenerator_", "")}\n";
                    }
                }
            }
            
            // Add general description
            details += "\nThis trader buys and sells various items based on their category.";
            
            return details;
        }
        
        private void TriggerIncident(IncidentDef incident)
        {
            var parms = StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap);
            var firingIncident = new FiringIncident(incident, null, parms);
            
            if (incident.Worker.CanFireNow(parms))
            {
                incident.Worker.TryExecute(parms);
                Messages.Message($"Triggered incident: {incident.label}", MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message($"Cannot trigger {incident.label} right now", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void ClearAllConditions()
        {
            if (Find.CurrentMap?.mapPawns?.AllPawns == null) return;
            
            int clearedCount = 0;
            // Only affect player colonists, not enemies or neutral pawns
            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists.Concat(PawnsFinder.AllMaps_PrisonersOfColony))
            {
                // Clear hediffs (health conditions)
                if (pawn.health?.hediffSet?.hediffs != null)
                {
                    var hediffsToRemove = pawn.health.hediffSet.hediffs
                        .Where(h => h.def.isBad || h.def.chronic || 
                               h.def.defName == "Berserk" || h.def.defName == "PsychicBerserk")
                        .ToList();
                        
                    foreach (var hediff in hediffsToRemove)
                    {
                        pawn.health.RemoveHediff(hediff);
                        clearedCount++;
                    }
                }
                
                // Clear mental states (like berserk, daze, etc.)
                if (pawn.mindState?.mentalStateHandler != null)
                {
                    if (pawn.mindState.mentalStateHandler.CurStateDef != null)
                    {
                        pawn.mindState.mentalStateHandler.Reset();
                        clearedCount++;
                    }
                }
                
                // Clear any mental breaks - try to break out of current state
                if (pawn.mindState?.mentalBreaker != null)
                {
                    // Force break the current mental break state
                    if (pawn.mindState.mentalStateHandler.CurStateDef != null)
                    {
                        pawn.mindState.mentalStateHandler.Reset();
                    }
                }
            }
            
            Messages.Message($"Cleared {clearedCount} conditions and mental states from colonists", MessageTypeDefOf.PositiveEvent);
        }
        
        private void CleanFilthFromMap()
        {
            if (Find.CurrentMap == null) return;
            
            var filthThings = Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Filth).ToList();
            int cleanedCount = filthThings.Count;
            
            foreach (var filth in filthThings)
            {
                filth.Destroy();
            }
            
            Messages.Message($"Cleaned {cleanedCount} filth from map", MessageTypeDefOf.PositiveEvent);
        }
        
        private void MakeAllColonistsBerserk()
        {
            if (Find.CurrentMap?.mapPawns?.FreeColonists == null) return;
            
            int berserkCount = 0;
            foreach (var colonist in Find.CurrentMap.mapPawns.FreeColonists)
            {
                if (colonist.mindState != null)
                {
                    colonist.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                    berserkCount++;
                }
            }
            
            Messages.Message($"Made {berserkCount} colonists berserk", MessageTypeDefOf.ThreatBig);
        }
        
        private void MakeAllAnimalsAttackEverything()
        {
            if (Find.CurrentMap?.mapPawns?.AllPawns == null) return;
            
            int animalCount = 0;
            foreach (var animal in Find.CurrentMap.mapPawns.AllPawns.Where(p => p.RaceProps.Animal))
            {
                if (animal.mindState != null)
                {
                    animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                    animalCount++;
                }
            }
            
            Messages.Message($"Made {animalCount} animals attack everything", MessageTypeDefOf.ThreatBig);
        }
        
        private void ChangeFactionGoodwill(Faction faction, int change)
        {
            try
            {
                if (faction == null || faction.IsPlayer)
                {
                    Messages.Message("Cannot modify player faction or null faction", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                var currentGoodwill = faction.PlayerGoodwill;
                var newGoodwill = Mathf.Clamp(currentGoodwill + change, -100, 100);
                
                // Use RimWorld's built-in method for changing goodwill
                var goodwillChange = newGoodwill - currentGoodwill;
                faction.TryAffectGoodwillWith(Faction.OfPlayer, goodwillChange, canSendMessage: false, canSendHostilityLetter: false);
                
                // Alternative approach if the above doesn't work
                if (faction.PlayerGoodwill == currentGoodwill)
                {
                    // Use reflection as fallback
                    var goodwillField = typeof(Faction).GetField("goodwillInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (goodwillField != null)
                    {
                        goodwillField.SetValue(faction, newGoodwill);
                    }
                }
                
                // Relations will automatically update based on goodwill thresholds
                var finalGoodwill = faction.PlayerGoodwill;
                
                Messages.Message($"{faction.Name} goodwill changed to {finalGoodwill} ({goodwillChange:+0;-#})", 
                    goodwillChange > 0 ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent);
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Error changing faction goodwill for {faction?.Name}: {ex.Message}");
                Messages.Message("Failed to change faction goodwill - check logs for details", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void GrowAllPlantsOnMap()
        {
            if (Find.CurrentMap == null) return;
            
            int plantsGrown = 0;
            foreach (var plant in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Plant))
            {
                if (plant is Plant p && p.Growth < 1f)
                {
                    p.Growth = 1f;
                    plantsGrown++;
                }
            }
            
            Messages.Message($"Grew {plantsGrown} plants to full maturity", MessageTypeDefOf.PositiveEvent);
        }
        
        private void StartAreaPlantGrowth()
        {
            if (Find.CurrentMap == null) return;
            
            Messages.Message("Select two corners to define the growth area (first click = start, second click = end)", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting area selection
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), firstCorner => {
                if (firstCorner.IsValid)
                {
                    // Show visual feedback for first corner
                    ShowAreaSelectionVisual(firstCorner.Cell, IntVec3.Invalid);
                    
                    Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), secondCorner => {
                        if (secondCorner.IsValid)
                        {
                            // Show final area selection visual
                            ShowAreaSelectionVisual(firstCorner.Cell, secondCorner.Cell);
                            GrowPlantsInArea(firstCorner.Cell, secondCorner.Cell);
                        }
                        else
                        {
                            Messages.Message("Area plant growth cancelled", MessageTypeDefOf.NeutralEvent);
                        }
                    }, null, delegate { 
                        // Drawing delegate for area selection preview
                        DrawAreaSelectionPreview(firstCorner.Cell);
                    });
                }
                else
                {
                    Messages.Message("Area plant growth cancelled", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        private void GrowPlantsInArea(IntVec3 corner1, IntVec3 corner2)
        {
            if (Find.CurrentMap == null) return;
            
            var minX = Mathf.Min(corner1.x, corner2.x);
            var maxX = Mathf.Max(corner1.x, corner2.x);
            var minZ = Mathf.Min(corner1.z, corner2.z);
            var maxZ = Mathf.Max(corner1.z, corner2.z);
            
            int plantsGrown = 0;
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Find.CurrentMap))
                    {
                        var plants = cell.GetThingList(Find.CurrentMap).OfType<Plant>();
                        foreach (var plant in plants)
                        {
                            if (plant.Growth < 1f)
                            {
                                plant.Growth = 1f;
                                plantsGrown++;
                            }
                        }
                    }
                }
            }
            
            Messages.Message($"Grew {plantsGrown} plants in selected area ({(maxX - minX + 1)}x{(maxZ - minZ + 1)} cells)", 
                MessageTypeDefOf.PositiveEvent);
        }
        
        private void StartBerserkTargeting()
        {
            if (Find.CurrentMap == null) return;
            
            Messages.Message("Left-click on a colonist to make them berserk, right-click to cancel", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForThing(), target => {
                if (target.IsValid && target.Thing is Pawn pawn)
                {
                    MakeColonistBerserk(pawn);
                }
                else
                {
                    Messages.Message("Berserk targeting cancelled", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        private void StartAnimalAttackTargeting()
        {
            if (Find.CurrentMap == null) return;
            
            Messages.Message("Left-click on an animal to make it attack everything, right-click to cancel", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForThing(), target => {
                if (target.IsValid && target.Thing is Pawn pawn && pawn.RaceProps.Animal)
                {
                    MakeAnimalAttackEverything(pawn);
                }
                else
                {
                    Messages.Message("Animal attack targeting cancelled or invalid target", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        private void MakeColonistBerserk(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;
            
            // Find berserk hediff def by string
            var berserkDef = DefDatabase<HediffDef>.GetNamedSilentFail("Berserk");
            if (berserkDef != null)
            {
                var berserkHediff = HediffMaker.MakeHediff(berserkDef, pawn);
                pawn.health.AddHediff(berserkHediff);
                Messages.Message($"{pawn.Name.ToStringShort} has been made berserk!", MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                Messages.Message("Berserk hediff not found!", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void MakeAnimalAttackEverything(Pawn animal)
        {
            if (animal?.mindState == null) return;
            
            animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
            
            Messages.Message($"{animal.Name.ToStringShort} is now attacking everything!", MessageTypeDefOf.NegativeEvent);
        }
        
        /// <summary>
        /// Shows visual feedback for area selection
        /// </summary>
        private void ShowAreaSelectionVisual(IntVec3 corner1, IntVec3 corner2)
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null) return;
                
                if (!corner2.IsValid)
                {
                    // First corner only - show single cell highlight
                    FleckMaker.ThrowLightningGlow(corner1.ToVector3Shifted(), map, 2f);
                    FleckMaker.ThrowDustPuffThick(corner1.ToVector3Shifted(), map, 1.5f, Color.green);
                }
                else
                {
                    // Both corners - show area highlight
                    var cells = GetAreaCells(corner1, corner2);
                    
                    foreach (var cell in cells.Take(100)) // Limit for performance
                    {
                        FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 1f, Color.green);
                        if (cell == corner1 || cell == corner2)
                        {
                            FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 1.5f);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"GUIDevMode: Failed to show area selection visual: {ex}");
            }
        }
        
        /// <summary>
        /// Draws area selection preview during targeting
        /// </summary>
        private void DrawAreaSelectionPreview(IntVec3 firstCorner)
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null) return;
                
                var mousePos = UI.MouseMapPosition();
                var mouseCell = mousePos.ToIntVec3();
                
                if (mouseCell.InBounds(map))
                {
                    var cells = GetAreaCells(firstCorner, mouseCell);
                    
                    // Draw preview area
                    var previewColor = Color.green;
                    previewColor.a = 0.3f;
                    GenDraw.DrawFieldEdges(cells, previewColor);
                    
                    // Highlight corners
                    var cornerColor = Color.yellow;
                    cornerColor.a = 0.8f;
                    GenDraw.DrawFieldEdges(new List<IntVec3> { firstCorner, mouseCell }, cornerColor);
                    
                    // Show info
                    var areaSize = $"{Mathf.Abs(mouseCell.x - firstCorner.x) + 1} x {Mathf.Abs(mouseCell.z - firstCorner.z) + 1}";
                    var plantCount = cells.Count(cell => 
                        cell.GetThingList(map).Any(thing => thing is Plant plant && plant.Growth < 1f));
                    
                    var infoText = $"Area: {areaSize}\nPlants to grow: {plantCount}";
                    var infoRect = new Rect(UI.screenWidth - 200f, 50f, 190f, 50f);
                    Widgets.DrawBoxSolid(infoRect, new Color(0f, 0f, 0f, 0.7f));
                    GUI.color = Color.green;
                    Text.Font = GameFont.Small;
                    Widgets.Label(infoRect, infoText);
                    GUI.color = Color.white;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"GUIDevMode: Failed to draw area selection preview: {ex}");
            }
        }
        
        /// <summary>
        /// Gets all cells in rectangular area between two corners
        /// </summary>
        private List<IntVec3> GetAreaCells(IntVec3 corner1, IntVec3 corner2)
        {
            var cells = new List<IntVec3>();
            
            var minX = Mathf.Min(corner1.x, corner2.x);
            var maxX = Mathf.Max(corner1.x, corner2.x);
            var minZ = Mathf.Min(corner1.z, corner2.z);
            var maxZ = Mathf.Max(corner1.z, corner2.z);
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Find.CurrentMap))
                    {
                        cells.Add(cell);
                    }
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// Starts targeting for hunger manipulation
        /// </summary>
        private void StartHungerTargeting()
        {
            if (Find.CurrentMap == null) return;
            
            Messages.Message("Left-click on a colonist to adjust their hunger level, right-click to cancel", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForThing(), target => {
                if (target.IsValid && target.Thing is Pawn pawn && pawn.RaceProps.Humanlike)
                {
                    ShowHungerMenu(pawn);
                }
                else
                {
                    Messages.Message("Hunger targeting cancelled or invalid target", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        /// <summary>
        /// Shows hunger level adjustment menu for selected colonist
        /// </summary>
        private void ShowHungerMenu(Pawn pawn)
        {
            if (pawn?.needs?.food == null) return;
            
            var options = new List<FloatMenuOption>();
            
            // Add hunger level options
            options.Add(new FloatMenuOption("Set to Famished (0%)", () => {
                pawn.needs.food.CurLevel = 0f;
                Messages.Message($"{pawn.Name.ToStringShort} is now famished", MessageTypeDefOf.NegativeEvent);
            }));
            
            options.Add(new FloatMenuOption("Set to Hungry (25%)", () => {
                pawn.needs.food.CurLevel = 0.25f;
                Messages.Message($"{pawn.Name.ToStringShort} is now hungry", MessageTypeDefOf.NeutralEvent);
            }));
            
            options.Add(new FloatMenuOption("Set to Satisfied (75%)", () => {
                pawn.needs.food.CurLevel = 0.75f;
                Messages.Message($"{pawn.Name.ToStringShort} is now satisfied", MessageTypeDefOf.PositiveEvent);
            }));
            
            options.Add(new FloatMenuOption("Set to Full (100%)", () => {
                pawn.needs.food.CurLevel = 1f;
                Messages.Message($"{pawn.Name.ToStringShort} is now completely full", MessageTypeDefOf.PositiveEvent);
            }));
            
            // Add other needs manipulation
            options.Add(new FloatMenuOption("--- Other Needs ---", null));
            
            if (pawn.needs.mood != null)
            {
                options.Add(new FloatMenuOption("Set Mood to Happy", () => {
                    pawn.needs.mood.CurLevel = 1f;
                    Messages.Message($"{pawn.Name.ToStringShort} is now very happy", MessageTypeDefOf.PositiveEvent);
                }));
                
                options.Add(new FloatMenuOption("Set Mood to Sad", () => {
                    pawn.needs.mood.CurLevel = 0.1f;
                    Messages.Message($"{pawn.Name.ToStringShort} is now very sad", MessageTypeDefOf.NegativeEvent);
                }));
            }
            
            if (pawn.needs.rest != null)
            {
                options.Add(new FloatMenuOption("Set Rest to Rested", () => {
                    pawn.needs.rest.CurLevel = 1f;
                    Messages.Message($"{pawn.Name.ToStringShort} is now fully rested", MessageTypeDefOf.PositiveEvent);
                }));
                
                options.Add(new FloatMenuOption("Set Rest to Exhausted", () => {
                    pawn.needs.rest.CurLevel = 0f;
                    Messages.Message($"{pawn.Name.ToStringShort} is now exhausted", MessageTypeDefOf.NegativeEvent);
                }));
            }
            
            if (pawn.needs.joy != null)
            {
                options.Add(new FloatMenuOption("Set Recreation to Full", () => {
                    pawn.needs.joy.CurLevel = 1f;
                    Messages.Message($"{pawn.Name.ToStringShort} is now fully entertained", MessageTypeDefOf.PositiveEvent);
                }));
            }
            
            options.Add(new FloatMenuOption("Cancel", null));
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}