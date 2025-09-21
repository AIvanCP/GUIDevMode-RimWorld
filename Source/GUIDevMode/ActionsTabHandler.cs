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
        
        // Time skip progress tracking
        private bool timeSkipInProgress = false;
        private int timeSkipTotalTicks = 0;
        private int timeSkipProcessedTicks = 0;
        private float timeSkipProgress = 0f;
        private System.DateTime timeSkipStartTime;
        
        public void DrawActionsTab(Rect rect)
        {
            // Use scroll view for the entire tab content
            var viewRect = new Rect(0, 0, rect.width - 16f, 1000f); // Give enough height for scrolling
            
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            var listing = new Listing_Standard();
            listing.Begin(viewRect);
            
            // Time Controls Section
            DrawTimeControlsSection(listing);
            
            listing.GapLine(12f);
            
            // Explosion Section
            DrawExplosionSection(listing);
            
            listing.End();
            
            Widgets.EndScrollView();
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
            
            // Disable buttons if time skip is in progress
            GUI.enabled = !timeSkipInProgress;
            
            if (Widgets.ButtonText(leftThird, "Skip 1 Hour"))
            {
                StartTimeSkip(2500); // 1 hour = 2500 ticks
            }
            if (Widgets.ButtonText(middleThird, "Skip 1 Day"))
            {
                StartTimeSkip(60000); // 1 day = 60000 ticks
            }
            if (Widgets.ButtonText(rightThird, "Skip 1 Season"))
            {
                StartTimeSkip(900000); // 1 season = 900000 ticks
            }
            
            GUI.enabled = true;
            
            // Show progress bar and cancel button if time skip is active
            if (timeSkipInProgress)
            {
                listing.Gap(5f);
                
                // Progress bar
                var progressRect = listing.GetRect(25f);
                Widgets.FillableBar(progressRect, timeSkipProgress);
                
                var progressText = $"Time skip: {timeSkipProcessedTicks:N0} / {timeSkipTotalTicks:N0} ticks ({timeSkipProgress:P0})";
                Widgets.Label(progressRect, progressText);
                
                // Cancel button
                var cancelRect = listing.GetRect(30f);
                GUI.color = Color.red;
                if (Widgets.ButtonText(cancelRect, "Cancel Time Skip"))
                {
                    CancelTimeSkip();
                }
                GUI.color = Color.white;
                
                // Update progress
                UpdateTimeSkipProgress();
            }
        }
        
        private void DrawExplosionSection(Listing_Standard listing)
        {
            listing.Label("Explosions & Targeting:");
            
            // Search filter
            var searchRect = listing.GetRect(30f);
            Widgets.Label(searchRect.LeftPart(0.2f), "Filter:");
            explosionSearchFilter = Widgets.TextField(searchRect.RightPart(0.8f), explosionSearchFilter);
            
            // Calculate dynamic height for explosion list based on remaining space
            var remainingHeight = listing.CurHeight;
            var minHeight = 150f;
            var maxHeight = 400f;
            var dynamicHeight = Mathf.Clamp(remainingHeight - 200f, minHeight, maxHeight); // Leave 200f for controls below
            
            // Explosion type selection with scrolling
            var explosionListRect = listing.GetRect(dynamicHeight);
            var filteredExplosions = string.IsNullOrEmpty(explosionSearchFilter) 
                ? ExplosionSystem.AllExplosionTypes 
                : ExplosionSystem.AllExplosionTypes.Where(e => 
                    (e.label?.ToLower().Contains(explosionSearchFilter.ToLower()) ?? false) ||
                    e.defName.ToLower().Contains(explosionSearchFilter.ToLower())).ToList();
            
            var viewRect = new Rect(0, 0, explosionListRect.width - 16f, filteredExplosions.Count * 25f);
            
            Widgets.BeginScrollView(explosionListRect, ref explosionScrollPosition, viewRect);
            
            var y = 0f;
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
                {
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 2f, 75f);
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                }
            }
            if (Widgets.ButtonText(quickMiddleThird, "Medium (R:3)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                {
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 3f, 100f);
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                }
            }
            if (Widgets.ButtonText(quickRightThird, "Large (R:5)"))
            {
                if (ExplosionSystem.selectedExplosionType != null)
                {
                    ExplosionSystem.StartQuickExplosion(ExplosionSystem.selectedExplosionType, 5f, 150f);
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                }
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
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
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
                    Find.WindowStack.TryRemove(typeof(GUIDevModeWindow), false);
                }
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
        
        private void StartTimeSkip(int ticks)
        {
            timeSkipInProgress = true;
            timeSkipTotalTicks = ticks;
            timeSkipProcessedTicks = 0;
            timeSkipProgress = 0f;
            timeSkipStartTime = System.DateTime.Now;
            
            Messages.Message($"Starting time skip: {ticks:N0} ticks", MessageTypeDefOf.NeutralEvent);
        }
        
        private void UpdateTimeSkipProgress()
        {
            if (!timeSkipInProgress) return;
            
            var settings = GUIDevModeMod.Settings;
            // Reduce batch size to prevent freezing - process fewer ticks per frame
            var ticksToProcess = Mathf.Min(50, timeSkipTotalTicks - timeSkipProcessedTicks); // Reduced from 500 to 50
            
            if (ticksToProcess <= 0)
            {
                // Time skip completed
                CompleteTimeSkip();
                return;
            }
            
            // Process the ticks
            if (settings.useQuickSkipMode)
            {
                // Quick mode: jump forward in smaller increments
                var jumpTicks = Mathf.Min(100, ticksToProcess); // Smaller jumps to prevent issues
                Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + jumpTicks);
                timeSkipProcessedTicks += jumpTicks;
            }
            else
            {
                // Proper simulation mode with performance optimization
                var ticksPerFrame = Mathf.Min(10, ticksToProcess); // Process even fewer ticks per frame
                
                for (int i = 0; i < ticksPerFrame; i++)
                {
                    try
                    {
                        Find.TickManager.DoSingleTick();
                        timeSkipProcessedTicks++;
                        
                        // Check if we've completed
                        if (timeSkipProcessedTicks >= timeSkipTotalTicks)
                        {
                            break;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error($"GUIDevMode: Error during time skip tick: {ex}");
                        // Continue processing despite errors
                    }
                }
            }
            
            timeSkipProgress = (float)timeSkipProcessedTicks / timeSkipTotalTicks;
            
            // Add performance monitoring
            var elapsed = System.DateTime.Now - timeSkipStartTime;
            if (elapsed.TotalSeconds > 30f) // Auto-cancel if taking too long
            {
                Messages.Message("Time skip taking too long, auto-cancelling to prevent freeze", MessageTypeDefOf.RejectInput);
                CancelTimeSkip();
            }
        }
        
        private void CompleteTimeSkip()
        {
            timeSkipInProgress = false;
            var elapsed = System.DateTime.Now - timeSkipStartTime;
            Messages.Message($"Time skip completed! Processed {timeSkipTotalTicks:N0} ticks in {elapsed.TotalSeconds:F1} seconds", 
                MessageTypeDefOf.PositiveEvent);
        }
        
        private void CancelTimeSkip()
        {
            timeSkipInProgress = false;
            Messages.Message($"Time skip cancelled. Processed {timeSkipProcessedTicks:N0} of {timeSkipTotalTicks:N0} ticks", 
                MessageTypeDefOf.NeutralEvent);
        }
        
        private void AdvanceTime(int ticks)
        {
            StartTimeSkip(ticks);
        }
    }
}