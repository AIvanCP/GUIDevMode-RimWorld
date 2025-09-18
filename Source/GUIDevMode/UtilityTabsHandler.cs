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
            listing.End();
            
            // Research list
            var scrollRect = new Rect(rect.x, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            var allResearch = DefDatabase<ResearchProjectDef>.AllDefs
                .Where(research => string.IsNullOrEmpty(researchSearchText) || 
                    research.label.ToLower().Contains(researchSearchText.ToLower()))
                .OrderBy(research => research.label);
            
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
            
            if (listing.ButtonText("Spawn Bulk Goods Trader"))
                SpawnTrader(TraderKindDefOf.Orbital_BulkGoods);
            if (listing.ButtonText("Spawn Combat Supplier"))
                SpawnTrader(TraderKindDefOf.Orbital_CombatSupplier);
            if (listing.ButtonText("Spawn Exotic Goods Trader"))
                SpawnTrader(TraderKindDefOf.Orbital_ExoticGoods);
                
            listing.Gap(10f);
            listing.Label("Economy Actions:");
            
            if (listing.ButtonText("Add 10000 Silver"))
                AddSilver(10000);
            if (listing.ButtonText("Add 1000 Components"))
                AddComponents(1000);
            if (listing.ButtonText("Add Random Trade Goods"))
                AddRandomTradeGoods();
                
            listing.End();
        }
        
        public void DrawUtilitiesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
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
                
            listing.End();
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
            var contentHeight = factions.Count * 100f;
            
            Widgets.BeginScrollView(scrollRect, ref factionsScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var faction in factions)
            {
                var factionRect = new Rect(0, y, scrollRect.width - 16f, 95f);
                DrawFactionEntry(factionRect, faction);
                y += 100f;
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
            listing.End();
            
            var scrollRect = new Rect(rect.x, listing.CurHeight, rect.width, rect.height - listing.CurHeight);
            var incidents = DefDatabase<IncidentDef>.AllDefs
                .Where(inc => !showRaidIncidentsOnly || inc.category == IncidentCategoryDefOf.ThreatBig)
                .OrderBy(inc => inc.label);
                
            var contentHeight = incidents.Count() * 35f;
            
            Widgets.BeginScrollView(scrollRect, ref incidentsScrollPos, new Rect(0, 0, scrollRect.width - 16f, contentHeight));
            
            float y = 0f;
            foreach (var incident in incidents)
            {
                var incidentRect = new Rect(0, y, scrollRect.width - 16f, 30f);
                DrawIncidentEntry(incidentRect, incident);
                y += 35f;
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
                    research.ResetProgress();
                else
                    FinishResearch(research);
            }
            
            GUI.color = Color.white;
            
            var statusText = isCompleted ? "Completed" : "Incomplete";
            Widgets.Label(rect.RightPart(0.3f), statusText);
        }
        
        private void DrawFactionEntry(Rect rect, Faction faction)
        {
            Widgets.DrawMenuSection(rect);
            var innerRect = rect.ContractedBy(5);
            
            var listing = new Listing_Standard();
            listing.Begin(innerRect);
            
            listing.Label($"{faction.Name} ({faction.def.label})");
            listing.Label($"Goodwill: {faction.PlayerGoodwill}");
            
            var buttonRect = listing.GetRect(25f);
            if (Widgets.ButtonText(buttonRect.LeftHalf(), "Make Allied"))
                faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Ally);
            if (Widgets.ButtonText(buttonRect.RightHalf(), "Make Hostile"))
                faction.TrySetRelationKind(Faction.OfPlayer, FactionRelationKind.Hostile);
                
            listing.End();
        }
        
        private void DrawIncidentEntry(Rect rect, IncidentDef incident)
        {
            if (Widgets.ButtonText(rect, incident.label))
            {
                TriggerIncident(incident);
            }
        }
        
        // Action methods
        private void CompleteCurrentResearch()
        {
            if (Find.ResearchManager.currentProj != null)
            {
                FinishResearch(Find.ResearchManager.currentProj);
                Messages.Message($"Completed research: {Find.ResearchManager.currentProj.label}", MessageTypeDefOf.PositiveEvent);
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
            foreach (var research in DefDatabase<ResearchProjectDef>.AllDefs)
            {
                research.ResetProgress();
            }
            Messages.Message("Reset all research progress", MessageTypeDefOf.NeutralEvent);
        }
        
        private void FinishResearch(ResearchProjectDef research)
        {
            Find.ResearchManager.FinishProject(research);
        }
        
        private void SpawnTrader(TraderKindDef traderKind)
        {
            if (spawnOrbitalTrader)
            {
                var incident = new FiringIncident(IncidentDefOf.OrbitalTraderArrival, null, null);
                incident.parms.traderKind = traderKind;
                incident.def.Worker.TryExecute(incident.parms);
                Messages.Message($"Orbital {traderKind.label} trader arrived", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                var incident = new FiringIncident(IncidentDefOf.TraderCaravanArrival, null, null);
                incident.parms.traderKind = traderKind;
                incident.def.Worker.TryExecute(incident.parms);
                Messages.Message($"{traderKind.label} caravan arrived", MessageTypeDefOf.PositiveEvent);
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
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
            {
                pawn.health.Reset();
            }
            Messages.Message("Healed all colonists", MessageTypeDefOf.PositiveEvent);
        }
        
        private void FillAllNeeds()
        {
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
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
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
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
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
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
    }
}