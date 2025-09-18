using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public class ActionsTabHandler
    {
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 explosionScrollPosition = Vector2.zero;
        
        // Explosion settings
        private float customExplosionRadius = 3.0f;
        private float customExplosionDamage = 100f;
        private string explosionSearchFilter = "";
        
        // Time control settings
        private int timeSpeedMultiplier = 1;
        private bool timeControlsExpanded = false;
        
        public void DrawActionsTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            
            // Time Controls Section
            DrawTimeControlsSection(listing);
            
            listing.GapLine(12f);
            
            // Explosion Section
            DrawExplosionSection(listing);
            
            listing.End();
        }
        
        private void DrawTimeControlsSection(Listing_Standard listing)
        {
            var headerRect = listing.GetRect(24f);
            if (Widgets.ButtonText(headerRect, timeControlsExpanded ? "▼ Time Controls" : "▶ Time Controls"))
            {
                timeControlsExpanded = !timeControlsExpanded;
            }
            
            if (!timeControlsExpanded) return;
            
            listing.Gap(4f);
            
            // Time speed controls
            var speedRect = listing.GetRect(30f);
            Widgets.Label(speedRect.LeftHalf(), $"Time Speed: {timeSpeedMultiplier}x");
            
            var buttonWidth = 60f;
            var buttonRect = new Rect(speedRect.x + speedRect.width - buttonWidth * 4 - 12f, speedRect.y, buttonWidth, speedRect.height);
            
            if (Widgets.ButtonText(buttonRect, "0.5x"))
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Ultrafast;
                Find.TickManager.slower.SignalForceNormalSpeed();
                timeSpeedMultiplier = 1; // Normal is baseline
            }
            
            buttonRect.x += buttonWidth + 4f;
            if (Widgets.ButtonText(buttonRect, "1x"))
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                timeSpeedMultiplier = 1;
            }
            
            buttonRect.x += buttonWidth + 4f;
            if (Widgets.ButtonText(buttonRect, "2x"))
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Fast;
                timeSpeedMultiplier = 2;
            }
            
            buttonRect.x += buttonWidth + 4f;
            if (Widgets.ButtonText(buttonRect, "3x"))
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
                timeSpeedMultiplier = 3;
            }
            
            // Pause/Unpause
            var pauseRect = listing.GetRect(30f);
            var pauseText = Find.TickManager.Paused ? "Unpause" : "Pause";
            if (Widgets.ButtonText(pauseRect.RightHalf(), pauseText))
            {
                if (Find.TickManager.Paused)
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
                else
                    Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            }
            
            // Advanced time controls
            var advancedRect = listing.GetRect(30f);
            var thirdWidth = advancedRect.width / 3f;
            var leftThird = new Rect(advancedRect.x, advancedRect.y, thirdWidth - 2f, advancedRect.height);
            var middleThird = new Rect(advancedRect.x + thirdWidth + 2f, advancedRect.y, thirdWidth - 2f, advancedRect.height);
            var rightThird = new Rect(advancedRect.x + thirdWidth * 2f + 2f, advancedRect.y, thirdWidth - 2f, advancedRect.height);
            
            if (Widgets.ButtonText(leftThird, "Skip 1 Hour"))
            {
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 2500);
            }
            if (Widgets.ButtonText(middleThird, "Skip 1 Day"))
            {
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 60000);
            }
            if (Widgets.ButtonText(rightThird, "Skip 1 Season"))
            {
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 900000);
            }
        }
        
        private void DrawExplosionSection(Listing_Standard listing)
        {
            listing.Label("Explosions & Targeting:");
            
            // Search filter
            var searchRect = listing.GetRect(30f);
            Widgets.Label(searchRect.LeftPart(0.2f), "Filter:");
            explosionSearchFilter = Widgets.TextField(searchRect.RightPart(0.8f), explosionSearchFilter);
            
            // Explosion type selection with scrolling
            var explosionListRect = listing.GetRect(200f);
            var viewRect = new Rect(0, 0, explosionListRect.width - 16f, ExplosionSystem.AllExplosionTypes.Count * 25f);
            
            Widgets.BeginScrollView(explosionListRect, ref explosionScrollPosition, viewRect);
            
            var y = 0f;
            var filteredExplosions = string.IsNullOrEmpty(explosionSearchFilter) 
                ? ExplosionSystem.AllExplosionTypes 
                : ExplosionSystem.AllExplosionTypes.Where(e => 
                    (e.label?.ToLower().Contains(explosionSearchFilter.ToLower()) ?? false) ||
                    e.defName.ToLower().Contains(explosionSearchFilter.ToLower())).ToList();
            
            foreach (var explosionType in filteredExplosions)
            {
                var itemRect = new Rect(0, y, viewRect.width, 24f);
                var isSelected = ExplosionSystem.selectedExplosionType == explosionType;
                
                if (isSelected)
                {
                    Widgets.DrawHighlight(itemRect);
                }
                
                var labelText = explosionType.label ?? explosionType.defName;
                if (Widgets.ButtonText(itemRect, labelText, false))
                {
                    ExplosionSystem.selectedExplosionType = explosionType;
                }
                
                y += 25f;
            }
            
            Widgets.EndScrollView();
            
            listing.Gap(8f);
            
            // Quick explosion buttons
            var quickRect = listing.GetRect(30f);
            var quickThirdWidth = quickRect.width / 3f;
            var quickLeftThird = new Rect(quickRect.x, quickRect.y, quickThirdWidth - 2f, quickRect.height);
            var quickMiddleThird = new Rect(quickRect.x + quickThirdWidth + 2f, quickRect.y, quickThirdWidth - 2f, quickRect.height);
            var quickRightThird = new Rect(quickRect.x + quickThirdWidth * 2f + 2f, quickRect.y, quickThirdWidth - 2f, quickRect.height);
            
            if (Widgets.ButtonText(quickLeftThird, "Small (R:2)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 2f, 75f);
            }
            if (Widgets.ButtonText(quickMiddleThird, "Medium (R:3)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 3f, 100f);
            }
            if (Widgets.ButtonText(quickRightThird, "Large (R:5)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 5f, 150f);
            }
            
            // Custom explosion settings
            listing.Gap(4f);
            listing.Label("Custom Explosion:");
            
            var radiusRect = listing.GetRect(30f);
            Widgets.Label(radiusRect.LeftHalf(), $"Radius: {customExplosionRadius:F1}");
            customExplosionRadius = Widgets.HorizontalSlider(radiusRect.RightHalf(), customExplosionRadius, 0.5f, 10f);
            
            var damageRect = listing.GetRect(30f);
            Widgets.Label(damageRect.LeftHalf(), $"Damage: {customExplosionDamage:F0}");
            customExplosionDamage = Widgets.HorizontalSlider(damageRect.RightHalf(), customExplosionDamage, 10f, 500f);
            
            var customButtonRect = listing.GetRect(30f);
            if (Widgets.ButtonText(customButtonRect, "Custom Explosion (Click to Target)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                {
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, customExplosionRadius, customExplosionDamage);
                }
            }
            
            // Standard explosion button
            var standardButtonRect = listing.GetRect(30f);
            if (Widgets.ButtonText(standardButtonRect, "Standard Explosion (Click to Target)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                {
                    var radius = ExplosionSystem.GetExplosionRadius(ExplosionSystem.selectedExplosionType);
                    ExplosionSystem.StartExplosionTargeting(ExplosionSystem.selectedExplosionType, radius);
                }
            }
            
            // Status display
            if (ExplosionSystem.ExplosionTargetingActive)
            {
                listing.Gap(4f);
                var statusRect = listing.GetRect(20f);
                GUI.color = Color.yellow;
                Widgets.Label(statusRect, "Explosion targeting active - Right-click to cancel");
                GUI.color = Color.white;
            }
            
            // Refresh explosions button
            listing.Gap(8f);
            var refreshRect = listing.GetRect(30f);
            if (Widgets.ButtonText(refreshRect, "Refresh Explosion Types"))
            {
                ExplosionSystem.RefreshExplosionTypes();
                Messages.Message($"Found {ExplosionSystem.AllExplosionTypes.Count} explosion types", MessageTypeDefOf.NeutralEvent);
            }
        }
    }
}