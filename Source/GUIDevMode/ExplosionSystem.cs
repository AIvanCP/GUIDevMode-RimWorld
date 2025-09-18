using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public static class ExplosionSystem
    {
        // Explosion preview system
        private static List<DamageDef> allExplosionTypes = new List<DamageDef>();
        public static DamageDef selectedExplosionType = null;
        private static bool isTargetingExplosion = false;
        private static DamageDef currentExplosionType = null;
        private static float currentExplosionRadius = 0f;
        private static Color explosionPreviewColor = Color.red;
        private static bool explosionTargetingActive = false;
        
        public static List<DamageDef> AllExplosionTypes => allExplosionTypes;
        public static bool IsTargetingExplosion => isTargetingExplosion;
        public static bool ExplosionTargetingActive => explosionTargetingActive;
        
        public static void RefreshExplosionTypes()
        {
            allExplosionTypes = new List<DamageDef>
            {
                // Core explosion types
                DamageDefOf.Bomb,
                DamageDefOf.Flame,
                DamageDefOf.EMP,
                DamageDefOf.Frostbite,
                DamageDefOf.Burn
            };
            
            // Comprehensive mod explosion detection
            var modExplosionTypes = DefDatabase<DamageDef>.AllDefs
                .Where(def => 
                    // Common explosion patterns
                    def.defName.ToLower().Contains("explosion") ||
                    def.defName.ToLower().Contains("bomb") ||
                    def.defName.ToLower().Contains("blast") ||
                    def.defName.ToLower().Contains("grenade") ||
                    def.defName.ToLower().Contains("missile") ||
                    def.defName.ToLower().Contains("rocket") ||
                    def.defName.ToLower().Contains("mortar") ||
                    def.defName.ToLower().Contains("artillery") ||
                    def.defName.ToLower().Contains("shell") ||
                    def.defName.ToLower().Contains("drone") ||
                    def.defName.ToLower().Contains("mine") ||
                    def.defName.ToLower().Contains("charge") ||
                    
                    // Gas and chemical types
                    def.defName.ToLower().Contains("gas") ||
                    def.defName.ToLower().Contains("acid") ||
                    def.defName.ToLower().Contains("toxic") ||
                    def.defName.ToLower().Contains("poison") ||
                    def.defName.ToLower().Contains("chemical") ||
                    def.defName.ToLower().Contains("smoke") ||
                    def.defName.ToLower().Contains("napalm") ||
                    def.defName.ToLower().Contains("incendiary") ||
                    def.defName.ToLower().Contains("tear") ||
                    def.defName.ToLower().Contains("teargas") ||
                    def.defName.ToLower().Contains("extinguish") ||
                    def.defName.ToLower().Contains("foam") ||
                    def.defName.ToLower().Contains("steam") ||
                    def.defName.ToLower().Contains("vapor") ||
                    
                    // Energy and special types
                    def.defName.ToLower().Contains("plasma") ||
                    def.defName.ToLower().Contains("laser") ||
                    def.defName.ToLower().Contains("beam") ||
                    def.defName.ToLower().Contains("psychic") ||
                    def.defName.ToLower().Contains("psi") ||
                    def.defName.ToLower().Contains("stun") ||
                    def.defName.ToLower().Contains("sonic") ||
                    
                    // Label-based detection
                    (def.label != null && (
                        def.label.ToLower().Contains("explosion") ||
                        def.label.ToLower().Contains("bomb") ||
                        def.label.ToLower().Contains("blast") ||
                        def.label.ToLower().Contains("gas") ||
                        def.label.ToLower().Contains("toxic") ||
                        def.label.ToLower().Contains("acid") ||
                        def.label.ToLower().Contains("drone") ||
                        def.label.ToLower().Contains("mine") ||
                        def.label.ToLower().Contains("grenade") ||
                        def.label.ToLower().Contains("tear") ||
                        def.label.ToLower().Contains("extinguish") ||
                        def.label.ToLower().Contains("foam"))) ||
                    
                    // Check if it has explosion-like properties
                    (def.harmsHealth && def.canInterruptJobs))
                .Where(def => !allExplosionTypes.Contains(def))
                .OrderBy(def => def.label ?? def.defName);
                
            allExplosionTypes.AddRange(modExplosionTypes);
            
            if (selectedExplosionType == null && allExplosionTypes.Any())
                selectedExplosionType = DamageDefOf.Bomb;
                
            // Log explosion count only when debugging
            if (Prefs.DevMode)
                Log.Message($"[GUI Dev Mode] Found {allExplosionTypes.Count} explosion types including mod content");
        }
        
        public static float GetExplosionRadius(DamageDef damageDef)
        {
            if (damageDef == DamageDefOf.Bomb) return 3.0f;
            if (damageDef == DamageDefOf.Flame) return 2.5f;
            if (damageDef == DamageDefOf.EMP) return 4.0f;
            if (damageDef == DamageDefOf.Frostbite) return 2.0f;
            return 2.5f; // Default radius
        }
        
        public static Color GetExplosionColor()
        {
            if (currentExplosionType == DamageDefOf.Bomb) return Color.red;
            if (currentExplosionType == DamageDefOf.Flame) return Color.yellow;
            if (currentExplosionType == DamageDefOf.EMP) return Color.blue;
            if (currentExplosionType == DamageDefOf.Frostbite) return Color.cyan;
            return Color.red; // Default color
        }
        
        public static void StartExplosionTargeting(DamageDef damageDef, float radius, bool isCustom = false, float damageAmount = 100f)
        {
            // Set up explosion preview state
            currentExplosionType = damageDef;
            currentExplosionRadius = radius;
            explosionPreviewColor = GetExplosionColor();
            explosionPreviewColor.a = 0.35f; // Semi-transparent
            isTargetingExplosion = true;
            explosionTargetingActive = true;
            
            ContinuousExplosionTargeting(damageDef, radius, isCustom, damageAmount);
        }
        
        private static void ContinuousExplosionTargeting(DamageDef damageDef, float radius, bool isCustom = false, float damageAmount = 100f)
        {
            // Set up explosion preview state for continuous targeting
            currentExplosionType = damageDef;
            currentExplosionRadius = radius;
            explosionPreviewColor = GetExplosionColor();
            explosionPreviewColor.a = 0.35f; // Semi-transparent
            isTargetingExplosion = true;
            explosionTargetingActive = true;
            
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    if (isCustom)
                    {
                        GenExplosion.DoExplosion(target.Cell, Find.CurrentMap, radius, damageDef, null, 
                            Mathf.RoundToInt(damageAmount));
                        Messages.Message($"{damageDef.label} explosion (radius {radius:F1}, damage {damageAmount:F0})", 
                            MessageTypeDefOf.NeutralEvent);
                    }
                    else
                    {
                        GenExplosion.DoExplosion(target.Cell, Find.CurrentMap, radius, damageDef, null);
                        Messages.Message($"{damageDef.label} explosion (radius {radius:F1})", 
                            MessageTypeDefOf.NeutralEvent);
                    }
                    
                    // Continue targeting for more explosions
                    ContinuousExplosionTargeting(damageDef, radius, isCustom, damageAmount);
                }
                else
                {
                    // Stop targeting if invalid target (right-click cancel)
                    explosionTargetingActive = false;
                    isTargetingExplosion = false;
                    Messages.Message("Explosion targeting stopped", MessageTypeDefOf.NeutralEvent);
                }
            }, null, delegate { 
                // Drawing delegate - called every frame during targeting
                DrawExplosionRadiusPreviewStatic();
            });
        }
        
        public static void DrawExplosionRadiusPreviewStatic()
        {
            if (!explosionTargetingActive || !isTargetingExplosion || currentExplosionType == null)
            {
                return;
            }
                
            var cell = UI.MouseCell();
            if (!cell.InBounds(Find.CurrentMap))
                return;

            // Use the stored explosion preview color
            var color = explosionPreviewColor;
            
            // Draw explosion radius ring
            GenDraw.DrawRadiusRing(cell, currentExplosionRadius, color);
            
            // Also draw inner ring at half radius for better visualization
            var innerColor = color;
            innerColor.a = 0.15f;
            GenDraw.DrawRadiusRing(cell, currentExplosionRadius * 0.5f, innerColor);
            
            // Draw target cell highlight
            var targetColor = color;
            targetColor.a = 0.6f;
            GenDraw.DrawFieldEdges(new List<IntVec3> { cell }, targetColor);
        }
        
        public static void StartQuickExplosion(DamageDef damageDef, float radius, float damageAmount)
        {
            StartExplosionTargeting(damageDef, radius, true, damageAmount);
        }
    }
}