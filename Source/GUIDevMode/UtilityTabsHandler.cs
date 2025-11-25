using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class UtilityTabsHandler
    {
        // Static variables for plant growth area targeting
        private static bool isTargetingPlantGrowth = false;
        private static IntVec3 firstGrowthCorner = IntVec3.Invalid;
        private static bool hasFirstGrowthCorner = false;
        
        // Public properties for external access
        public static bool IsTargetingPlantGrowth => isTargetingPlantGrowth;
        
        /// <summary>
        /// Clears plant growth targeting state
        /// </summary>
        public static void ClearPlantGrowthTargeting()
        {
            isTargetingPlantGrowth = false;
            hasFirstGrowthCorner = false;
            firstGrowthCorner = IntVec3.Invalid;
            
            // Stop the MapComponent preview
            MapComponent_RadiusPreview.StopPlantGrowthPreview();
        }
        
        // Research tab data
        private Vector2 researchScrollPos = Vector2.zero;
        private string researchSearchText = "";
        private bool showUnresearchedOnly = true; // Show unresearched by default
        
        // Trading tab data
        private Vector2 tradingScrollPos = Vector2.zero;
        private bool spawnOrbitalTrader = true;
        private string tradingSearchText = "";
        
        // Utilities tab data
        private Vector2 utilitiesScrollPos = Vector2.zero;
        private bool confirmCleanMap = false;
        
        // Factions tab data
        private Vector2 factionsScrollPos = Vector2.zero;
        
        // Incidents tab data
        private Vector2 incidentsScrollPos = Vector2.zero;
        private bool showRaidIncidentsOnly = false;
        private string incidentsSearchText = "";
        
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
            
            // Warning about caravan traders
            GUI.color = Color.yellow;
            listing.Label("⚠️ CARAVAN TRADERS DISABLED ⚠️");
            listing.Label("Caravan traders disabled due to mod conflicts.");
            listing.Label("Orbital traders are still available.");
            listing.Gap(10f);
            GUI.color = Color.white;
            
            // Orbital trader toggle (always true)
            spawnOrbitalTrader = true;
            listing.CheckboxLabeled("Spawn as Orbital Trader (Only Option)", ref spawnOrbitalTrader);
            listing.Gap(5f);
            
            // Quick spawn buttons
            listing.Label("Quick Actions:");
            if (listing.ButtonText("Call Random Orbital Trader"))
                CallRandomOrbitalTrader();
            if (listing.ButtonText("Add 50 Components"))
                AddComponents(50);
            listing.Gap(10f);
            
            listing.End();
            
            // Trader list with scroll
            var scrollRect = new Rect(rect.x, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            var allTraders = DefDatabase<TraderKindDef>.AllDefs.Where(t => t != null).OrderBy(t => t.label).ToList();
            var contentHeight = allTraders.Count * 60f;
            
            Widgets.BeginScrollView(scrollRect, ref tradingScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var trader in allTraders)
            {
                var traderRect = new Rect(0, y, scrollRect.width - 16f, 55f);
                DrawOrbitalTraderEntry(traderRect, trader);
                y += 60f;
            }
            
            Widgets.EndScrollView();
        }
        
        public void DrawUtilitiesTab(Rect rect)
        {
            // Calculate actual content height based on number of buttons and sections
            // Each button is ~30f, each gap is ~8f, each label is ~20f
            // Map Actions: 1 label + 3 buttons + 1 gap = 20 + 90 + 8 = 118
            // Dangerous Actions: 1 label + 2-3 buttons + 1 gap = 20 + 60-90 + 8 = 88-118
            // Colony Utilities: 1 label + 6 buttons + 1 gap = 20 + 180 + 8 = 208
            // Plant utilities: 1 label + 2 buttons + 1 gap = 20 + 60 + 8 = 88
            // Aggressive actions: 1 label + 4 buttons = 20 + 120 = 140
            // Total: approximately 642-672f, so let's use 800f to be safe
            float contentHeight = 800f;
            
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
            if (listing.ButtonText("Grow Plants in Zone (Select Area)"))
                StartAreaPlantGrowth();
                
            listing.Gap(8f);
            
            // Aggressive actions
            listing.Label("Aggressive Actions:");
            if (listing.ButtonText("Make Colonist Berserk (Select Target)"))
                StartBerserkTargeting();
            if (listing.ButtonText("Make Animal Attack Everything (Select Target)"))
                StartAnimalAttackTargeting();
            if (listing.ButtonText("Create Animal Manhunt (All Map Animals)"))
                StartAnimalManhunt();
            if (listing.ButtonText("Target Colonist Hunger (Select Target)"))
                StartHungerTargeting();
                
            listing.End();
            Widgets.EndScrollView();
        }
        
        public void DrawFactionsTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            // Add extra gap at top to prevent covering tab above
            listing.Gap(15f);
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
            listing.Gap(5f);
            
            // Search field for incidents
            listing.Label("Search incidents:");
            var newIncidentsSearchText = listing.TextEntry(incidentsSearchText);
            if (newIncidentsSearchText != incidentsSearchText)
            {
                incidentsSearchText = newIncidentsSearchText;
            }
            listing.Gap(10f); // More space to prevent overlap
            
            var currentY = listing.CurHeight;
            listing.End();
            
            // Ensure proper spacing by using absolute positioning
            var scrollRect = new Rect(rect.x, rect.y + currentY, rect.width, rect.height - currentY);
            var incidents = DefDatabase<IncidentDef>.AllDefs
                .Where(inc => {
                    try
                    {
                        // Skip incidents with null worker (incompatible mods)
                        if (inc == null || inc.Worker == null || inc.category == null)
                            return false;
                        
                        // Apply raid filter
                        if (showRaidIncidentsOnly && inc.category != IncidentCategoryDefOf.ThreatBig)
                            return false;
                        
                        // Apply search filter
                        if (!string.IsNullOrEmpty(incidentsSearchText))
                        {
                            var searchTerm = incidentsSearchText.ToLower();
                            return (inc.label?.ToLower().Contains(searchTerm) ?? false) ||
                                   (inc.defName?.ToLower().Contains(searchTerm) ?? false) ||
                                   (inc.category?.label?.ToLower().Contains(searchTerm) ?? false);
                        }
                        
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[GUI Dev Mode] Error filtering incident {inc?.defName ?? "unknown"}: {ex.Message}");
                        return false; // Skip problematic incidents
                    }
                })
                .OrderBy(inc => inc.category?.label ?? "Unknown")
                .ThenBy(inc => inc.label ?? inc.defName ?? "Unknown");
                
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
        
        private void DrawOrbitalTraderEntry(Rect rect, TraderKindDef trader)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(3);
            
            var nameRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.75f, 25f);
            var buttonRect = new Rect(innerRect.x + innerRect.width * 0.75f, innerRect.y, innerRect.width * 0.25f, 25f);
            
            Widgets.Label(nameRect, trader.label + " (Orbital)");
            if (Widgets.ButtonText(buttonRect, "Spawn"))
            {
                SpawnOrbitalTrader(trader);
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
            if (incident == null) return;
            
            try
            {
                Widgets.DrawMenuSection(rect);
                var innerRect = rect.ContractedBy(3);
                
                // Title and category row
                var titleRect = new Rect(innerRect.x, innerRect.y, innerRect.width * 0.7f, 20f);
                var buttonRect = new Rect(innerRect.x + innerRect.width * 0.75f, innerRect.y, innerRect.width * 0.25f, 20f);
                
                // Color code by incident category
                if (incident.category != null)
                {
                    GUI.color = GetIncidentCategoryColor(incident.category);
                }
                
                var displayLabel = incident.label ?? incident.defName ?? "Unknown";
                var categoryLabel = incident.category?.label ?? "Unknown";
                Widgets.Label(titleRect, $"{displayLabel} ({categoryLabel})");
                GUI.color = Color.white;
                
                // Trigger button - disable if worker is null
                var canTrigger = incident.Worker != null && incident.category != null;
                if (!canTrigger)
                {
                    GUI.color = Color.gray;
                }
                
                if (Widgets.ButtonText(buttonRect, canTrigger ? "Trigger" : "Invalid"))
                {
                    if (canTrigger)
                    {
                        TriggerIncident(incident);
                    }
                    else
                    {
                        Messages.Message($"{displayLabel} cannot be triggered - incompatible or missing data", MessageTypeDefOf.RejectInput);
                    }
                }
                
                GUI.color = Color.white;
                
                // Description row
                var descRect = new Rect(innerRect.x, innerRect.y + 22f, innerRect.width, 22f);
                GUI.color = Color.gray;
                var description = GetIncidentDescription(incident);
                Widgets.Label(descRect, description);
                GUI.color = Color.white;
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[GUI Dev Mode] Error drawing incident entry for {incident?.defName ?? "unknown"}: {ex.Message}");
            }
        }
        
        private Color GetIncidentCategoryColor(IncidentCategoryDef category)
        {
            if (category == null)
                return Color.white;
            
            try
            {
                if (category == IncidentCategoryDefOf.ThreatBig || category == IncidentCategoryDefOf.ThreatSmall)
                    return Color.red;
                if (category.defName == "FactionArrival")
                    return Color.yellow;
                if (category == IncidentCategoryDefOf.Misc)
                    return Color.cyan;
                if (category.defName != null && category.defName.Contains("Disease"))
                    return new Color(1f, 0.5f, 0f); // Orange
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[GUI Dev Mode] Error getting incident category color: {ex.Message}");
            }
            
            return Color.white;
        }
        
        private string GetIncidentDescription(IncidentDef incident)
        {
            if (incident == null)
                return "Unknown incident";
            
            try
            {
                // Try to get description from various sources
                if (!string.IsNullOrEmpty(incident.description))
                    return incident.description;
                
                // Generate description based on incident type and name
                var defName = (incident.defName ?? "").ToLowerInvariant();
                var label = (incident.label ?? "").ToLowerInvariant();
                
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
                if (incident.category?.defName?.Contains("Disease") == true)
                    return "Disease affects the colony";
                
                // Faction events
                if (incident.category?.defName == "FactionArrival")
                    return "New faction arrives in the area";
                
                // Default description
                return $"Event: {incident.label ?? incident.defName ?? "Unknown"}";
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[GUI Dev Mode] Error getting incident description: {ex.Message}");
                return "Unable to load incident description";
            }
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
        
        private void SpawnOrbitalTrader(TraderKindDef traderKind)
        {
            try
            {
                if (Find.CurrentMap == null)
                {
                    Messages.Message("No active map to spawn trader", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (traderKind == null)
                {
                    Messages.Message("Invalid trader kind", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                // Use safer orbital trader spawning only
                try
                {
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                    parms.target = Find.CurrentMap;
                    parms.traderKind = traderKind;
                    parms.forced = true; // Force spawn in dev mode
                    
                    var incident = IncidentDefOf.OrbitalTraderArrival;
                    if (incident?.Worker != null && incident.Worker.CanFireNow(parms))
                    {
                        incident.Worker.TryExecute(parms);
                        Messages.Message($"Orbital {traderKind.label} trader called", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Cannot spawn orbital trader right now - try again later", MessageTypeDefOf.RejectInput);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Warning($"[GUI Dev Mode] Orbital trader failed: {ex.Message}");
                    Messages.Message("Orbital trader spawning failed", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Critical error spawning orbital trader {traderKind?.defName}: {ex.Message}");
                Messages.Message("Critical trader spawning error - check logs", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void CallRandomOrbitalTrader()
        {
            var allTraders = DefDatabase<TraderKindDef>.AllDefs.ToList();
            if (allTraders.Any())
            {
                var randomTrader = allTraders.RandomElement();
                SpawnOrbitalTrader(randomTrader);
            }
            else
            {
                Messages.Message("No traders available", MessageTypeDefOf.RejectInput);
            }
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
                
                // Check for null or invalid trader kinds
                if (traderKind == null)
                {
                    Messages.Message("Invalid trader kind", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (spawnOrbitalTrader)
                {
                    // Use safer orbital trader spawning
                    try
                    {
                        var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                        parms.target = Find.CurrentMap;
                        parms.traderKind = traderKind;
                        
                        // Additional safety checks for orbital traders
                        var incident = IncidentDefOf.OrbitalTraderArrival;
                        if (incident?.Worker != null && incident.Worker.CanFireNow(parms))
                        {
                            incident.Worker.TryExecute(parms);
                            Messages.Message($"Orbital {traderKind.label} trader called", MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message("Cannot spawn orbital trader right now - try again later", MessageTypeDefOf.RejectInput);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[GUI Dev Mode] Orbital trader failed: {ex.Message}. Trying alternative method.");
                        Messages.Message("Orbital trader spawning failed - traders may be disabled by other mods", MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    // Use safer caravan trader spawning with better error handling
                    try
                    {
                        var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.CurrentMap);
                        parms.target = Find.CurrentMap;
                        parms.traderKind = traderKind;
                        
                        // For caravan traders, we need to ensure there's a valid faction
                        FactionDef factionToUse = null;
                        
                        // First try the trader's preferred faction
                        if (traderKind.faction != null)
                        {
                            factionToUse = traderKind.faction;
                        }
                        else
                        {
                            // Find any friendly or neutral faction that can trade
                            var availableFactions = Find.FactionManager.AllFactions
                                .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                                .ToList();
                            
                            if (availableFactions.Any())
                            {
                                parms.faction = availableFactions.RandomElement();
                            }
                            else
                            {
                                // Create a temporary trading faction if none exists
                                var neutralFactions = DefDatabase<FactionDef>.AllDefs
                                    .Where(f => f.humanlikeFaction && !f.isPlayer && !f.hidden)
                                    .ToList();
                                
                                if (neutralFactions.Any())
                                {
                                    factionToUse = neutralFactions.RandomElement();
                                }
                            }
                        }
                        
                        // Additional validation for caravan traders
                        var incident = IncidentDefOf.TraderCaravanArrival;
                        if (incident?.Worker != null)
                        {
                            // Force ignore cooldowns and restrictions for dev mode
                            parms.forced = true;
                            
                            if (incident.Worker.CanFireNow(parms) || parms.forced)
                            {
                                var success = incident.Worker.TryExecute(parms);
                                if (success)
                                {
                                    Messages.Message($"{traderKind.label} caravan called", MessageTypeDefOf.PositiveEvent);
                                }
                                else
                                {
                                    // Try alternative method - direct spawn
                                    Log.Message($"[GUI Dev Mode] Standard caravan spawn failed, trying direct method for {traderKind.label}");
                                    TryDirectCaravanSpawn(traderKind, parms.faction);
                                }
                            }
                            else
                            {
                                // Try direct spawn anyway in dev mode
                                Log.Message($"[GUI Dev Mode] CanFireNow failed for {traderKind.label}, trying direct spawn");
                                TryDirectCaravanSpawn(traderKind, parms.faction);
                            }
                        }
                        else
                        {
                            Messages.Message("Trader caravan incident worker not available", MessageTypeDefOf.RejectInput);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[GUI Dev Mode] Caravan trader {traderKind.defName} failed: {ex.Message}. Trying direct spawn.");
                        TryDirectCaravanSpawn(traderKind, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Critical error spawning trader {traderKind?.defName}: {ex.Message}");
                Messages.Message("Critical trader spawning error - check logs", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void TryDirectCaravanSpawn(TraderKindDef traderKind, Faction faction = null)
        {
            try
            {
                // Get or create a suitable faction
                if (faction == null)
                {
                    // Try to find existing non-hostile factions
                    var availableFactions = Find.FactionManager.AllFactions
                        .Where(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer) && f.def.humanlikeFaction)
                        .ToList();
                    
                    if (availableFactions.Any())
                    {
                        faction = availableFactions.RandomElement();
                    }
                    else
                    {
                        // Use any suitable faction def to create the trader
                        var suitableFactionDefs = DefDatabase<FactionDef>.AllDefs
                            .Where(f => f.humanlikeFaction && !f.isPlayer && !f.hidden)
                            .ToList();
                        
                        if (suitableFactionDefs.Any())
                        {
                            // Find existing faction or create temporary reference
                            var factionDef = suitableFactionDefs.RandomElement();
                            faction = Find.FactionManager.AllFactions
                                .FirstOrDefault(f => f.def == factionDef) ?? 
                                Find.FactionManager.AllFactions
                                .FirstOrDefault(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer));
                        }
                    }
                }
                
                if (faction != null)
                {
                    // Try to spawn caravan using incident system with forced parameters
                    var incident = IncidentDefOf.TraderCaravanArrival;
                    var parms = new IncidentParms();
                    parms.target = Find.CurrentMap;
                    parms.faction = faction;
                    parms.traderKind = traderKind;
                    parms.forced = true;
                    
                    // Bypass normal restrictions for dev mode
                    var success = incident.Worker.TryExecute(parms);
                    
                    if (success)
                    {
                        Messages.Message($"{traderKind.label} caravan spawned (direct method)", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message($"Failed to spawn {traderKind.label} caravan - may be incompatible with current game state", MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("No suitable faction found for caravan trader", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Direct caravan spawn failed for {traderKind.defName}: {ex.Message}");
                Messages.Message($"Direct caravan spawn failed for {traderKind.label}", MessageTypeDefOf.RejectInput);
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
            try
            {
                if (incident == null)
                {
                    Messages.Message("Invalid incident", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (Find.CurrentMap == null)
                {
                    Messages.Message("No active map to trigger incident", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                if (incident.Worker == null)
                {
                    Messages.Message($"Incident {incident.label ?? incident.defName} has no worker - possibly from incompatible mod", MessageTypeDefOf.RejectInput);
                    Log.Warning($"[GUI Dev Mode] Incident {incident.defName} has null Worker");
                    return;
                }
                
                if (incident.category == null)
                {
                    Messages.Message($"Incident {incident.label ?? incident.defName} has invalid category", MessageTypeDefOf.RejectInput);
                    return;
                }
                
                var parms = StorytellerUtility.DefaultParmsNow(incident.category, Find.CurrentMap);
                parms.forced = true; // Force in dev mode
                
                // Additional validation for specific parameters
                if (parms.target == null)
                {
                    parms.target = Find.CurrentMap;
                }
                
                if (incident.Worker.CanFireNow(parms))
                {
                    if (incident.Worker.TryExecute(parms))
                    {
                        Messages.Message($"Triggered incident: {incident.label ?? incident.defName}", MessageTypeDefOf.NeutralEvent);
                    }
                    else
                    {
                        Messages.Message($"Failed to trigger {incident.label ?? incident.defName} - execution failed", MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message($"Cannot trigger {incident.label ?? incident.defName} right now (conditions not met)", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Error triggering incident {incident?.defName ?? "unknown"}: {ex.Message}\n{ex.StackTrace}");
                Messages.Message($"Error triggering incident - check log. This may be from an incompatible mod.", MessageTypeDefOf.RejectInput);
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
            
            // Set static tracking variables
            isTargetingPlantGrowth = true;
            hasFirstGrowthCorner = false;
            firstGrowthCorner = IntVec3.Invalid;
            
            // Start the MapComponent preview
            MapComponent_RadiusPreview.StartPlantGrowthPreview();
            
            Messages.Message("Select area for plant growth: Click first corner, then second corner to define zone", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting area selection
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            // Simplified targeting without drawing delegates - MapComponent handles rendering
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), firstCorner => {
                if (firstCorner.IsValid)
                {
                    firstGrowthCorner = firstCorner.Cell;
                    hasFirstGrowthCorner = true;
                    
                    // Update MapComponent with first corner
                    MapComponent_RadiusPreview.SetPlantGrowthFirstCorner(firstCorner.Cell);
                    
                    Messages.Message("First corner selected. Now click second corner to complete the zone.", 
                        MessageTypeDefOf.NeutralEvent);
                    
                    Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), secondCorner => {
                        if (secondCorner.IsValid)
                        {
                            GrowPlantsInArea(firstCorner.Cell, secondCorner.Cell);
                        }
                        else
                        {
                            Messages.Message("Plant growth zone selection cancelled", MessageTypeDefOf.NeutralEvent);
                        }
                        // Reset static variables when done
                        ClearPlantGrowthTargeting();
                    }, null, null, null, () => {
                        // Cancel second corner selection
                        ClearPlantGrowthTargeting();
                        Messages.Message("Plant growth zone selection cancelled", MessageTypeDefOf.NeutralEvent);
                    });
                }
                else
                {
                    // Reset static variables on cancel
                    ClearPlantGrowthTargeting();
                    Messages.Message("Plant growth zone selection cancelled", MessageTypeDefOf.NeutralEvent);
                }
            }, null, null, null, () => {
                // Cancel first corner selection
                ClearPlantGrowthTargeting();
                Messages.Message("Plant growth zone selection cancelled", MessageTypeDefOf.NeutralEvent);
            });
        }
        
        private void DrawAreaGrowthPreview(IntVec3 firstCorner)
        {
            var currentMouseCell = UI.MouseCell();
            if (!currentMouseCell.InBounds(Find.CurrentMap))
                return;
            
            // Calculate area bounds
            var minX = Mathf.Min(firstCorner.x, currentMouseCell.x);
            var maxX = Mathf.Max(firstCorner.x, currentMouseCell.x);
            var minZ = Mathf.Min(firstCorner.z, currentMouseCell.z);
            var maxZ = Mathf.Max(firstCorner.z, currentMouseCell.z);
            
            var areaCells = new List<IntVec3>();
            var plantsToGrow = 0;
            
            // Build area cells list and count plants
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Find.CurrentMap))
                    {
                        areaCells.Add(cell);
                        var plants = cell.GetThingList(Find.CurrentMap).OfType<Plant>();
                        plantsToGrow += plants.Count(p => p.Growth < 1f);
                    }
                }
            }
            
            // Draw area preview
            if (areaCells.Any())
            {
                // Highlight the entire area
                GenDraw.DrawFieldEdges(areaCells, Color.green);
                
                // Draw corner markers with different colors
                GenDraw.DrawFieldEdges(new List<IntVec3> { firstCorner }, new Color(1f, 1f, 0f)); // Yellow
                GenDraw.DrawFieldEdges(new List<IntVec3> { currentMouseCell }, Color.cyan);
                
                // Show area info
                var mousePos = Event.current.mousePosition;
                var infoText = $"Area: {areaCells.Count} cells, {plantsToGrow} plants to grow";
                var labelRect = new Rect(mousePos.x + 10f, mousePos.y - 40f, 300f, 40f);
                
                GUI.color = Color.green;
                Widgets.Label(labelRect, infoText);
                GUI.color = Color.white;
            }
        }
        
        // Static method for continuous plant growth area preview drawing
        public static void DrawPlantGrowthPreviewStatic()
        {
            // Check if targeting is actually active and clear state if not
            if (isTargetingPlantGrowth && !Find.Targeter.IsTargeting)
            {
                ClearPlantGrowthTargeting();
                return;
            }
            
            if (!isTargetingPlantGrowth || Find.CurrentMap == null)
                return;
                
            var currentMouseCell = UI.MouseCell();
            if (!currentMouseCell.InBounds(Find.CurrentMap))
                return;
            
            if (!hasFirstGrowthCorner)
            {
                // First corner selection - highlight current cell like vanilla zone designation
                var cells = new List<IntVec3> { currentMouseCell };
                
                // Use more visible highlighting like vanilla zone designation
                GenDraw.DrawFieldEdges(cells, Color.green, null);
                
                // Add additional visual feedback with filled area
                Vector3 drawPos = currentMouseCell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, 
                    MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, 
                    new Color(0f, 1f, 0f, 0.3f)), 0);
                
                // Show info text
                var mousePos = Event.current.mousePosition;
                var labelRect = new Rect(mousePos.x + 10f, mousePos.y - 20f, 200f, 20f);
                GUI.color = Color.green;
                Widgets.Label(labelRect, "Click to select first corner");
                GUI.color = Color.white;
            }
            else
            {
                // Second corner selection - show area preview
                var minX = Mathf.Min(firstGrowthCorner.x, currentMouseCell.x);
                var maxX = Mathf.Max(firstGrowthCorner.x, currentMouseCell.x);
                var minZ = Mathf.Min(firstGrowthCorner.z, currentMouseCell.z);
                var maxZ = Mathf.Max(firstGrowthCorner.z, currentMouseCell.z);
                
                var areaCells = new List<IntVec3>();
                var plantsToGrow = 0;
                
                // Build area cells list and count plants
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var cell = new IntVec3(x, 0, z);
                        if (cell.InBounds(Find.CurrentMap))
                        {
                            areaCells.Add(cell);
                            var plants = cell.GetThingList(Find.CurrentMap).OfType<Plant>();
                            plantsToGrow += plants.Count(p => p.Growth < 1f);
                        }
                    }
                }
                
                // Draw area preview like vanilla zone designation
                if (areaCells.Any())
                {
                    // Use vanilla-style zone highlighting
                    GenDraw.DrawFieldEdges(areaCells, Color.green, null);
                    
                    // Add filled area visualization like vanilla growing zones
                    foreach (var cell in areaCells)
                    {
                        Vector3 drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                        Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, 
                            MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, 
                            new Color(0f, 1f, 0f, 0.2f)), 0);
                    }
                    
                    // Draw corner markers with different colors
                    Vector3 firstCornerPos = firstGrowthCorner.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                    Graphics.DrawMesh(MeshPool.plane10, firstCornerPos, Quaternion.identity, 
                        MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, 
                        new Color(1f, 1f, 0f, 0.8f)), 0); // Yellow for first corner
                        
                    Vector3 currentCornerPos = currentMouseCell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                    Graphics.DrawMesh(MeshPool.plane10, currentCornerPos, Quaternion.identity, 
                        MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, 
                        new Color(0f, 1f, 1f, 0.8f)), 0); // Cyan for current corner
                    
                    // Show area info
                    var mousePos = Event.current.mousePosition;
                    var infoText = $"Area: {areaCells.Count} cells, {plantsToGrow} plants to grow";
                    var labelRect = new Rect(mousePos.x + 10f, mousePos.y - 40f, 300f, 40f);
                    
                    GUI.color = Color.green;
                    Widgets.Label(labelRect, infoText);
                    GUI.color = Color.white;
                }
            }
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
            }, null, null, null, () => {
                Messages.Message("Berserk targeting cancelled", MessageTypeDefOf.NeutralEvent);
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
            }, null, null, null, () => {
                Messages.Message("Animal attack targeting cancelled", MessageTypeDefOf.NeutralEvent);
            });
        }
        
        private void MakeColonistBerserk(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;
            
            // Try multiple possible berserk hediff names for different RimWorld versions
            string[] possibleBerserkNames = { 
                "Berserk", 
                "BerserkRage", 
                "MentalState_Berserk", 
                "Beserk",  // typo in some mods
                "BerserkMentalState" 
            };
            
            HediffDef berserkDef = null;
            foreach (var name in possibleBerserkNames)
            {
                berserkDef = DefDatabase<HediffDef>.GetNamedSilentFail(name);
                if (berserkDef != null) break;
            }
            
            if (berserkDef != null)
            {
                var berserkHediff = HediffMaker.MakeHediff(berserkDef, pawn);
                pawn.health.AddHediff(berserkHediff);
                Messages.Message($"{pawn.Name.ToStringShort} has been made berserk!", MessageTypeDefOf.NegativeEvent);
            }
            else
            {
                // Try using mental state instead of hediff
                try
                {
                    var mentalStateDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("Berserk");
                    if (mentalStateDef == null)
                        mentalStateDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("MentalState_Berserk");
                    
                    if (mentalStateDef != null && pawn.mindState != null)
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(mentalStateDef, null, true);
                        Messages.Message($"{pawn.Name.ToStringShort} has been made berserk!", MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {
                        Messages.Message("Could not find berserk hediff or mental state. Check console for available options.", MessageTypeDefOf.RejectInput);
                        
                        // Debug: List available hediff and mental state defs
                        Log.Message("[GUI Dev Mode] Available HediffDefs containing 'berserk':");
                        foreach (var hediff in DefDatabase<HediffDef>.AllDefsListForReading)
                        {
                            if (hediff.defName.ToLower().Contains("berserk") || hediff.label.ToLower().Contains("berserk"))
                            {
                                Log.Message($"  - {hediff.defName} (label: {hediff.label})");
                            }
                        }
                        
                        Log.Message("[GUI Dev Mode] Available MentalStateDefs containing 'berserk':");
                        foreach (var mental in DefDatabase<MentalStateDef>.AllDefsListForReading)
                        {
                            if (mental.defName.ToLower().Contains("berserk") || mental.label.ToLower().Contains("berserk"))
                            {
                                Log.Message($"  - {mental.defName} (label: {mental.label})");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[GUI Dev Mode] Error trying to make pawn berserk: {ex.Message}");
                    Messages.Message("Error making pawn berserk - check console for details", MessageTypeDefOf.RejectInput);
                }
            }
        }
        
        private void MakeAnimalAttackEverything(Pawn animal)
        {
            if (animal == null)
            {
                Log.Warning("[GUI Dev Mode] MakeAnimalAttackEverything called with null animal");
                return;
            }
            
            if (animal.mindState?.mentalStateHandler == null)
            {
                Log.Warning($"[GUI Dev Mode] Animal {animal.def?.label ?? "Unknown"} has no mental state handler");
                return;
            }
            
            try
            {
                var success = animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                
                string animalName = "Unknown animal";
                try
                {
                    animalName = animal.Name?.ToStringShort ?? animal.def?.label ?? animal.LabelShort ?? "Unknown animal";
                }
                catch
                {
                    animalName = "Unknown animal";
                }
                
                if (success)
                {
                    Messages.Message($"{animalName} is now attacking everything!", MessageTypeDefOf.NegativeEvent);
                }
                else
                {
                    Messages.Message($"Failed to make {animalName} attack everything - already in mental state", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Failed to make animal attack everything: {ex.Message}\nStack trace: {ex.StackTrace}");
                Messages.Message("Failed to make animal attack - critical error", MessageTypeDefOf.RejectInput);
            }
        }
        
        private void StartAnimalManhunt()
        {
            if (Find.CurrentMap == null) return;
            
            var animals = Find.CurrentMap.mapPawns.AllPawns
                .Where(p => p.RaceProps.Animal && !p.Dead && p.Spawned)
                .ToList();
            
            if (animals.Count == 0)
            {
                Messages.Message("No animals found on map", MessageTypeDefOf.NeutralEvent);
                return;
            }
            
            int manhuntCount = 0;
            foreach (var animal in animals)
            {
                if (animal?.mindState?.mentalStateHandler != null && 
                    animal.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter))
                {
                    manhuntCount++;
                }
            }
            
            Messages.Message($"{manhuntCount} animals are now manhunting!", MessageTypeDefOf.ThreatBig);
            
            // Auto-close GUI after action
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
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
            
            Messages.Message("Left-click on any pawn/animal (player, NPC, wild, tame) to adjust their hunger level, right-click to cancel", 
                MessageTypeDefOf.NeutralEvent);
            
            // Auto-close GUI when starting targeting
            Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
            
            // Create targeting parameters that allow targeting any pawn
            var targetParams = new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetItems = false,
                validator = (TargetInfo target) => 
                {
                    return target.HasThing && target.Thing is Pawn pawn && 
                           pawn.needs?.food != null && !pawn.Dead && pawn.Spawned;
                }
            };
            
            Find.Targeter.BeginTargeting(targetParams, target => {
                if (target.IsValid && target.Thing is Pawn pawn)
                {
                    if (pawn.needs?.food != null)
                    {
                        ShowHungerMenu(pawn);
                    }
                    else
                    {
                        string pawnName = pawn.Name?.ToStringShort ?? pawn.def?.label ?? "Unknown";
                        Messages.Message($"{pawnName} does not have hunger needs", MessageTypeDefOf.RejectInput);
                    }
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
            if (pawn?.needs?.food == null) 
            {
                Messages.Message("Invalid pawn or pawn has no food need", MessageTypeDefOf.RejectInput);
                return;
            }
            
            // Ensure pawn has a valid name
            string pawnName = "Unknown";
            try
            {
                pawnName = pawn.Name?.ToStringShort ?? pawn.LabelShort ?? "Unknown Pawn";
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[GUI Dev Mode] Error getting pawn name: {ex.Message}");
                pawnName = "Unknown Pawn";
            }
            
            var options = new List<FloatMenuOption>();
            
            // Add hunger level options with proper null checking
            options.Add(new FloatMenuOption("Set to Famished (0%)", () => {
                try
                {
                    if (pawn?.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = 0f;
                        Messages.Message($"{pawnName} is now famished", MessageTypeDefOf.NegativeEvent);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[GUI Dev Mode] Error setting hunger to famished: {ex.Message}");
                }
            }));
            
            options.Add(new FloatMenuOption("Set to Hungry (25%)", () => {
                try
                {
                    if (pawn?.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = 0.25f;
                        Messages.Message($"{pawnName} is now hungry", MessageTypeDefOf.NeutralEvent);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[GUI Dev Mode] Error setting hunger to hungry: {ex.Message}");
                }
            }));
            
            options.Add(new FloatMenuOption("Set to Satisfied (75%)", () => {
                try
                {
                    if (pawn?.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = 0.75f;
                        Messages.Message($"{pawnName} is now satisfied", MessageTypeDefOf.PositiveEvent);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[GUI Dev Mode] Error setting hunger to satisfied: {ex.Message}");
                }
            }));
            
            options.Add(new FloatMenuOption("Set to Full (100%)", () => {
                try
                {
                    if (pawn?.needs?.food != null)
                    {
                        pawn.needs.food.CurLevel = 1f;
                        Messages.Message($"{pawnName} is now completely full", MessageTypeDefOf.PositiveEvent);
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[GUI Dev Mode] Error setting hunger to full: {ex.Message}");
                }
            }));
            
            // Add other needs manipulation with null checking
            options.Add(new FloatMenuOption("--- Other Needs ---", null));
            
            if (pawn?.needs?.mood != null)
            {
                options.Add(new FloatMenuOption("Set Mood to Happy", () => {
                    try
                    {
                        pawn.needs.mood.CurLevel = 1f;
                        Messages.Message($"{pawnName} is now very happy", MessageTypeDefOf.PositiveEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[GUI Dev Mode] Error setting mood to happy: {ex.Message}");
                    }
                }));
                
                options.Add(new FloatMenuOption("Set Mood to Sad", () => {
                    try
                    {
                        pawn.needs.mood.CurLevel = 0.1f;
                        Messages.Message($"{pawnName} is now very sad", MessageTypeDefOf.NegativeEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[GUI Dev Mode] Error setting mood to sad: {ex.Message}");
                    }
                }));
            }
            
            if (pawn?.needs?.rest != null)
            {
                options.Add(new FloatMenuOption("Set Rest to Rested", () => {
                    try
                    {
                        pawn.needs.rest.CurLevel = 1f;
                        Messages.Message($"{pawnName} is now fully rested", MessageTypeDefOf.PositiveEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[GUI Dev Mode] Error setting rest to rested: {ex.Message}");
                    }
                }));
                
                options.Add(new FloatMenuOption("Set Rest to Exhausted", () => {
                    try
                    {
                        pawn.needs.rest.CurLevel = 0f;
                        Messages.Message($"{pawnName} is now exhausted", MessageTypeDefOf.NegativeEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[GUI Dev Mode] Error setting rest to exhausted: {ex.Message}");
                    }
                }));
            }
            
            if (pawn?.needs?.joy != null)
            {
                options.Add(new FloatMenuOption("Set Recreation to Full", () => {
                    try
                    {
                        pawn.needs.joy.CurLevel = 1f;
                        Messages.Message($"{pawnName} is now fully entertained", MessageTypeDefOf.PositiveEvent);
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"[GUI Dev Mode] Error setting recreation to full: {ex.Message}");
                    }
                }));
            }
            
            options.Add(new FloatMenuOption("Cancel", null));
            
            try
            {
                Find.WindowStack.Add(new FloatMenu(options));
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Dev Mode] Error creating float menu: {ex.Message}");
                Messages.Message("Error creating hunger menu - check console for details", MessageTypeDefOf.RejectInput);
            }
        }
    }
}