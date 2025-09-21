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
        
        /// <summary>
        /// Clears explosion targeting state - use when canceling targeting
        /// </summary>
        public static void ClearExplosionTargeting()
        {
            explosionTargetingActive = false;
            isTargetingExplosion = false;
            currentExplosionType = null;
            currentExplosionRadius = 0f;
            
            // Stop the MapComponent preview
            MapComponent_RadiusPreview.StopExplosionPreview();
        }
        
        /// <summary>
        /// Validates that a damage type is suitable for explosion targeting
        /// </summary>
        public static bool IsValidExplosionType(DamageDef damageDef)
        {
            if (damageDef == null) return false;
            
            // Allow all types that can harm health or have special effects
            return damageDef.harmsHealth || 
                   damageDef.canInterruptJobs ||
                   damageDef.hasForcefulImpact ||
                   damageDef.defName.ToLowerInvariant().Contains("extinguish") ||
                   damageDef.defName.ToLowerInvariant().Contains("foam") ||
                   damageDef.defName.ToLowerInvariant().Contains("smoke");
        }
        
        /// <summary>
        /// Gets explosion statistics for debugging and information
        /// </summary>
        public static string GetExplosionStats()
        {
            if (allExplosionTypes == null || !allExplosionTypes.Any())
                return "No explosion types loaded";
                
            var vanillaCount = allExplosionTypes.Count(def => def.modContentPack?.IsCoreMod == true);
            var modCount = allExplosionTypes.Count - vanillaCount;
            var validCount = allExplosionTypes.Count(IsValidExplosionType);
            
            return $"Total: {allExplosionTypes.Count} ({vanillaCount} vanilla, {modCount} modded, {validCount} valid)";
        }
        
        public static void RefreshExplosionTypes()
        {
            allExplosionTypes = new List<DamageDef>();
            
            // Start with ALL vanilla damage types that can cause area effects
            var vanillaDamageTypes = DefDatabase<DamageDef>.AllDefs
                .Where(def => def.modContentPack?.IsCoreMod == true)
                .Where(def => 
                    // Core explosion types
                    def == DamageDefOf.Bomb ||
                    def == DamageDefOf.Flame ||
                    def == DamageDefOf.EMP ||
                    def == DamageDefOf.Frostbite ||
                    def == DamageDefOf.Burn ||
                    
                    // Additional vanilla types that can be used for explosions
                    def.defName == "Extinguish" ||
                    def.defName == "Smoke" ||
                    def.defName == "ToxGas" ||
                    def.defName == "Stun" ||
                    def.defName == "Arrow" ||
                    def.defName == "ArrowHighVelocity" ||
                    def.defName == "Bullet" ||
                    def.defName == "BulletHighVelocity" ||
                    
                    // Area effect capabilities
                    def.harmsHealth && (def.canInterruptJobs || def.hasForcefulImpact))
                .OrderBy(def => def.label ?? def.defName);
            
            allExplosionTypes.AddRange(vanillaDamageTypes);
            
            // Comprehensive mod explosion detection - much more extensive patterns
            var modExplosionTypes = DefDatabase<DamageDef>.AllDefs
                .Where(def => def.modContentPack?.IsCoreMod != true)
                .Where(def => 
                    // Explosion terminology (comprehensive)
                    def.defName.ToLowerInvariant().Contains("explosion") ||
                    def.defName.ToLowerInvariant().Contains("explode") ||
                    def.defName.ToLowerInvariant().Contains("bomb") ||
                    def.defName.ToLowerInvariant().Contains("blast") ||
                    def.defName.ToLowerInvariant().Contains("burst") ||
                    def.defName.ToLowerInvariant().Contains("detonate") ||
                    def.defName.ToLowerInvariant().Contains("grenade") ||
                    def.defName.ToLowerInvariant().Contains("missile") ||
                    def.defName.ToLowerInvariant().Contains("rocket") ||
                    def.defName.ToLowerInvariant().Contains("mortar") ||
                    def.defName.ToLowerInvariant().Contains("artillery") ||
                    def.defName.ToLowerInvariant().Contains("shell") ||
                    def.defName.ToLowerInvariant().Contains("torpedo") ||
                    def.defName.ToLowerInvariant().Contains("drone") ||
                    def.defName.ToLowerInvariant().Contains("mine") ||
                    def.defName.ToLowerInvariant().Contains("charge") ||
                    def.defName.ToLowerInvariant().Contains("warhead") ||
                    def.defName.ToLowerInvariant().Contains("payload") ||
                    
                    // Chemical and gas warfare
                    def.defName.ToLowerInvariant().Contains("gas") ||
                    def.defName.ToLowerInvariant().Contains("acid") ||
                    def.defName.ToLowerInvariant().Contains("toxic") ||
                    def.defName.ToLowerInvariant().Contains("poison") ||
                    def.defName.ToLowerInvariant().Contains("chemical") ||
                    def.defName.ToLowerInvariant().Contains("smoke") ||
                    def.defName.ToLowerInvariant().Contains("napalm") ||
                    def.defName.ToLowerInvariant().Contains("incendiary") ||
                    def.defName.ToLowerInvariant().Contains("tear") ||
                    def.defName.ToLowerInvariant().Contains("teargas") ||
                    def.defName.ToLowerInvariant().Contains("extinguish") ||
                    def.defName.ToLowerInvariant().Contains("foam") ||
                    def.defName.ToLowerInvariant().Contains("steam") ||
                    def.defName.ToLowerInvariant().Contains("vapor") ||
                    def.defName.ToLowerInvariant().Contains("cloud") ||
                    def.defName.ToLowerInvariant().Contains("mist") ||
                    def.defName.ToLowerInvariant().Contains("spore") ||
                    def.defName.ToLowerInvariant().Contains("bio") ||
                    def.defName.ToLowerInvariant().Contains("virus") ||
                    def.defName.ToLowerInvariant().Contains("bacteria") ||
                    
                    // Energy weapons and special effects
                    def.defName.ToLowerInvariant().Contains("plasma") ||
                    def.defName.ToLowerInvariant().Contains("laser") ||
                    def.defName.ToLowerInvariant().Contains("beam") ||
                    def.defName.ToLowerInvariant().Contains("ray") ||
                    def.defName.ToLowerInvariant().Contains("pulse") ||
                    def.defName.ToLowerInvariant().Contains("emp") ||
                    def.defName.ToLowerInvariant().Contains("electromagnetic") ||
                    def.defName.ToLowerInvariant().Contains("psychic") ||
                    def.defName.ToLowerInvariant().Contains("psi") ||
                    def.defName.ToLowerInvariant().Contains("stun") ||
                    def.defName.ToLowerInvariant().Contains("sonic") ||
                    def.defName.ToLowerInvariant().Contains("shock") ||
                    def.defName.ToLowerInvariant().Contains("electric") ||
                    def.defName.ToLowerInvariant().Contains("lightning") ||
                    def.defName.ToLowerInvariant().Contains("thunder") ||
                    def.defName.ToLowerInvariant().Contains("energy") ||
                    def.defName.ToLowerInvariant().Contains("void") ||
                    def.defName.ToLowerInvariant().Contains("antimatter") ||
                    def.defName.ToLowerInvariant().Contains("nuclear") ||
                    def.defName.ToLowerInvariant().Contains("atomic") ||
                    def.defName.ToLowerInvariant().Contains("quantum") ||
                    def.defName.ToLowerInvariant().Contains("fusion") ||
                    def.defName.ToLowerInvariant().Contains("fission") ||
                    
                    // Fire and heat effects
                    def.defName.ToLowerInvariant().Contains("fire") ||
                    def.defName.ToLowerInvariant().Contains("flame") ||
                    def.defName.ToLowerInvariant().Contains("burn") ||
                    def.defName.ToLowerInvariant().Contains("heat") ||
                    def.defName.ToLowerInvariant().Contains("thermal") ||
                    def.defName.ToLowerInvariant().Contains("molten") ||
                    def.defName.ToLowerInvariant().Contains("lava") ||
                    def.defName.ToLowerInvariant().Contains("magma") ||
                    def.defName.ToLowerInvariant().Contains("scorch") ||
                    def.defName.ToLowerInvariant().Contains("sear") ||
                    
                    // Cold and ice effects  
                    def.defName.ToLowerInvariant().Contains("freeze") ||
                    def.defName.ToLowerInvariant().Contains("frost") ||
                    def.defName.ToLowerInvariant().Contains("ice") ||
                    def.defName.ToLowerInvariant().Contains("cryo") ||
                    def.defName.ToLowerInvariant().Contains("cold") ||
                    def.defName.ToLowerInvariant().Contains("arctic") ||
                    def.defName.ToLowerInvariant().Contains("glacial") ||
                    def.defName.ToLowerInvariant().Contains("blizzard") ||
                    
                    // Label-based detection (more comprehensive)
                    (def.label != null && (
                        def.label.ToLowerInvariant().Contains("explosion") ||
                        def.label.ToLowerInvariant().Contains("bomb") ||
                        def.label.ToLowerInvariant().Contains("blast") ||
                        def.label.ToLowerInvariant().Contains("gas") ||
                        def.label.ToLowerInvariant().Contains("toxic") ||
                        def.label.ToLowerInvariant().Contains("acid") ||
                        def.label.ToLowerInvariant().Contains("drone") ||
                        def.label.ToLowerInvariant().Contains("mine") ||
                        def.label.ToLowerInvariant().Contains("grenade") ||
                        def.label.ToLowerInvariant().Contains("tear") ||
                        def.label.ToLowerInvariant().Contains("extinguish") ||
                        def.label.ToLowerInvariant().Contains("foam") ||
                        def.label.ToLowerInvariant().Contains("fire") ||
                        def.label.ToLowerInvariant().Contains("flame") ||
                        def.label.ToLowerInvariant().Contains("freeze") ||
                        def.label.ToLowerInvariant().Contains("frost") ||
                        def.label.ToLowerInvariant().Contains("plasma") ||
                        def.label.ToLowerInvariant().Contains("energy") ||
                        def.label.ToLowerInvariant().Contains("shock") ||
                        def.label.ToLowerInvariant().Contains("stun"))) ||
                    
                    // Check if it has explosion-like properties (comprehensive)
                    (def.harmsHealth && (
                        def.canInterruptJobs || 
                        def.hasForcefulImpact ||
                        def.makesBlood ||
                        (def.defaultDamage > 10 && def.armorCategory != null))))
                .Where(def => !allExplosionTypes.Contains(def))
                .OrderBy(def => def.label ?? def.defName);
                
            allExplosionTypes.AddRange(modExplosionTypes);
            
            // Remove duplicates and sort by category and name
            allExplosionTypes = allExplosionTypes
                .Distinct()
                .OrderBy(def => def.modContentPack?.IsCoreMod == true ? 0 : 1)
                .ThenBy(def => def.label ?? def.defName)
                .ToList();
            
            if (selectedExplosionType == null && allExplosionTypes.Any())
                selectedExplosionType = DamageDefOf.Bomb;
                
            // Log explosion count only when debugging
            if (Prefs.DevMode)
            {
                var vanillaCount = allExplosionTypes.Count(def => def.modContentPack?.IsCoreMod == true);
                var modCount = allExplosionTypes.Count - vanillaCount;
                Log.Message($"[GUI Dev Mode] Found {allExplosionTypes.Count} explosion types ({vanillaCount} vanilla, {modCount} modded)");
            }
        }
        
        public static float GetExplosionRadius(DamageDef damageDef)
        {
            // Vanilla damage type defaults
            if (damageDef == DamageDefOf.Bomb) return 3.0f;
            if (damageDef == DamageDefOf.Flame) return 2.5f;
            if (damageDef == DamageDefOf.EMP) return 4.0f;
            if (damageDef == DamageDefOf.Frostbite) return 2.0f;
            if (damageDef == DamageDefOf.Burn) return 2.0f;
            
            // Smart radius detection based on damage type name and properties
            var defName = (damageDef.defName ?? "").ToLowerInvariant();
            var label = (damageDef.label ?? "").ToLowerInvariant();
            
            // Large explosions
            if (defName.Contains("nuclear") || defName.Contains("atomic") || 
                defName.Contains("antimatter") || defName.Contains("nuke"))
                return 12.0f;
                
            // Artillery and heavy weapons
            if (defName.Contains("artillery") || defName.Contains("mortar") || 
                defName.Contains("missile") || defName.Contains("rocket") ||
                defName.Contains("torpedo") || defName.Contains("warhead"))
                return 6.0f;
                
            // Medium explosions
            if (defName.Contains("bomb") || defName.Contains("explosion") || 
                defName.Contains("blast") || defName.Contains("detonate"))
                return 4.0f;
                
            // Small explosions and grenades
            if (defName.Contains("grenade") || defName.Contains("mine") || 
                defName.Contains("charge") || defName.Contains("burst"))
                return 3.0f;
                
            // Gas and chemical weapons (larger area, less damage)
            if (defName.Contains("gas") || defName.Contains("toxic") || 
                defName.Contains("poison") || defName.Contains("chemical") ||
                defName.Contains("smoke") || defName.Contains("tear") ||
                defName.Contains("extinguish") || defName.Contains("foam"))
                return 5.0f;
                
            // Energy weapons
            if (defName.Contains("plasma") || defName.Contains("laser") || 
                defName.Contains("beam") || defName.Contains("emp") ||
                defName.Contains("electric") || defName.Contains("shock"))
                return 3.5f;
                
            // Fire effects
            if (defName.Contains("fire") || defName.Contains("flame") || 
                defName.Contains("burn") || defName.Contains("napalm") ||
                defName.Contains("incendiary") || defName.Contains("molten"))
                return 3.0f;
                
            // Ice and cold effects
            if (defName.Contains("freeze") || defName.Contains("frost") || 
                defName.Contains("ice") || defName.Contains("cryo") ||
                defName.Contains("cold") || defName.Contains("arctic"))
                return 2.5f;
                
            // Psychic and special effects
            if (defName.Contains("psychic") || defName.Contains("psi") || 
                defName.Contains("stun") || defName.Contains("sonic") ||
                defName.Contains("void") || defName.Contains("quantum"))
                return 4.5f;
            
            // Base radius on damage amount if available
            if (damageDef.defaultDamage > 50) return 4.0f;
            if (damageDef.defaultDamage > 25) return 3.0f;
            if (damageDef.defaultDamage > 10) return 2.5f;
            
            return 2.5f; // Default radius for unknown types
        }
        
        public static Color GetExplosionColor()
        {
            if (currentExplosionType == null) return Color.red;
            
            // Vanilla damage type colors
            if (currentExplosionType == DamageDefOf.Bomb) return Color.red;
            if (currentExplosionType == DamageDefOf.Flame) return Color.yellow;
            if (currentExplosionType == DamageDefOf.EMP) return Color.blue;
            if (currentExplosionType == DamageDefOf.Frostbite) return Color.cyan;
            if (currentExplosionType == DamageDefOf.Burn) return new Color(1f, 0.5f, 0f); // Orange
            
            // Smart color detection based on damage type
            var defName = (currentExplosionType.defName ?? "").ToLowerInvariant();
            var label = (currentExplosionType.label ?? "").ToLowerInvariant();
            
            // Fire and heat effects - Red/Orange/Yellow spectrum
            if (defName.Contains("fire") || defName.Contains("flame") || 
                defName.Contains("burn") || defName.Contains("napalm") ||
                defName.Contains("incendiary") || defName.Contains("molten") ||
                defName.Contains("lava") || defName.Contains("magma") ||
                defName.Contains("heat") || defName.Contains("thermal"))
                return new Color(1f, 0.5f, 0f); // Orange
                
            // Ice and cold effects - Cyan/Blue spectrum
            if (defName.Contains("freeze") || defName.Contains("frost") || 
                defName.Contains("ice") || defName.Contains("cryo") ||
                defName.Contains("cold") || defName.Contains("arctic") ||
                defName.Contains("glacial") || defName.Contains("blizzard"))
                return Color.cyan;
                
            // Electric and EMP effects - Blue spectrum
            if (defName.Contains("emp") || defName.Contains("electric") || 
                defName.Contains("shock") || defName.Contains("lightning") ||
                defName.Contains("thunder") || defName.Contains("electromagnetic"))
                return Color.blue;
                
            // Energy weapons - Purple/Magenta spectrum
            if (defName.Contains("plasma") || defName.Contains("laser") || 
                defName.Contains("beam") || defName.Contains("ray") ||
                defName.Contains("energy") || defName.Contains("antimatter") ||
                defName.Contains("void") || defName.Contains("quantum"))
                return Color.magenta;
                
            // Nuclear and atomic - Bright Green
            if (defName.Contains("nuclear") || defName.Contains("atomic") || 
                defName.Contains("nuke") || defName.Contains("fusion") ||
                defName.Contains("fission") || defName.Contains("radioactive"))
                return Color.green;
                
            // Gas and chemical weapons - Green spectrum
            if (defName.Contains("gas") || defName.Contains("toxic") || 
                defName.Contains("poison") || defName.Contains("chemical") ||
                defName.Contains("acid") || defName.Contains("bio") ||
                defName.Contains("virus") || defName.Contains("bacteria"))
                return new Color(0.5f, 1f, 0f); // Lime green
                
            // Smoke and extinguish - Gray
            if (defName.Contains("smoke") || defName.Contains("extinguish") || 
                defName.Contains("foam") || defName.Contains("steam") ||
                defName.Contains("vapor") || defName.Contains("mist"))
                return Color.gray;
                
            // Psychic effects - Purple
            if (defName.Contains("psychic") || defName.Contains("psi") || 
                defName.Contains("stun") || defName.Contains("sonic"))
                return new Color(0.5f, 0f, 1f); // Purple
                
            // Explosive weapons - Red spectrum
            if (defName.Contains("bomb") || defName.Contains("explosion") || 
                defName.Contains("blast") || defName.Contains("grenade") ||
                defName.Contains("mine") || defName.Contains("missile") ||
                defName.Contains("rocket") || defName.Contains("artillery") ||
                defName.Contains("mortar") || defName.Contains("warhead"))
                return Color.red;
            
            return Color.red; // Default color for unknown types
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
            
            // Start continuous preview via MapComponent
            MapComponent_RadiusPreview.StartExplosionPreview(damageDef, radius, explosionPreviewColor);
            
            Messages.Message($"Targeting {damageDef.label} explosion (radius: {radius:F1}) - Right-click to cancel", MessageTypeDefOf.NeutralEvent);
            
            // Use simplified BeginTargeting with proper cancel handling
            Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target => {
                if (target.IsValid)
                {
                    // Check if this is a gas-based explosion that needs special handling
                    if (IsGasExplosion(damageDef))
                    {
                        CreateGasExplosion(target.Cell, damageDef, radius, isCustom, damageAmount);
                    }
                    else if (isCustom)
                    {
                        GenExplosion.DoExplosion(target.Cell, Find.CurrentMap, radius, damageDef, null, 
                            Mathf.RoundToInt(damageAmount));
                    }
                    else
                    {
                        GenExplosion.DoExplosion(target.Cell, Find.CurrentMap, radius, damageDef, null);
                    }
                    
                    // Continue targeting for more explosions - restart targeting immediately
                    StartExplosionTargeting(damageDef, radius, isCustom, damageAmount);
                }
                else
                {
                    // Stop targeting if invalid target (right-click cancel)
                    ClearExplosionTargeting();
                    Messages.Message("Explosion targeting stopped", MessageTypeDefOf.NeutralEvent);
                }
            });
        }
        
        // Note: Visual rendering is now handled by MapComponent_RadiusPreview for continuous display
        
        public static void StartQuickExplosion(DamageDef damageDef, float radius, float damageAmount)
        {
            StartExplosionTargeting(damageDef, radius, true, damageAmount);
        }
        
        /// <summary>
        /// Determines if a damage type represents a gas-based explosion that needs persistent effects
        /// </summary>
        private static bool IsGasExplosion(DamageDef damageDef)
        {
            if (damageDef == null) return false;
            
            var defName = damageDef.defName.ToLowerInvariant();
            return defName.Contains("gas") || 
                   defName.Contains("toxic") || 
                   defName.Contains("smoke") || 
                   defName.Contains("poison") || 
                   defName.Contains("chemical") ||
                   defName.Contains("acid") ||
                   defName.Contains("vapor") ||
                   defName.Contains("mist") ||
                   defName == "toxgas";
        }
        
        /// <summary>
        /// Creates a gas explosion with persistent gas clouds
        /// </summary>
        private static void CreateGasExplosion(IntVec3 center, DamageDef damageDef, float radius, bool isCustom, float damageAmount)
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null) return;
                
                var defName = damageDef.defName.ToLowerInvariant();
                
                // First do a small initial explosion for immediate effects
                var initialRadius = radius * 0.3f;
                if (isCustom)
                {
                    GenExplosion.DoExplosion(center, map, initialRadius, damageDef, null, Mathf.RoundToInt(damageAmount * 0.5f));
                }
                else
                {
                    GenExplosion.DoExplosion(center, map, initialRadius, damageDef, null);
                }
                
                // Create persistent gas clouds in the area
                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map)) continue;
                    
                    var distance = cell.DistanceTo(center);
                    if (distance > radius) continue;
                    
                    // Create gas density based on distance from center
                    var gasIntensity = 1f - (distance / radius);
                    if (gasIntensity <= 0) continue;
                    
                    // Spawn gas effects with varying intensity
                    for (int i = 0; i < Mathf.RoundToInt(gasIntensity * 5); i++)
                    {
                        // Create visual gas effects that last longer
                        FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 2f + gasIntensity * 3f);
                        FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 3f + gasIntensity * 2f, GetGasColor(damageDef));
                        
                        // Add additional lingering effects
                        if (Rand.Chance(gasIntensity))
                        {
                            FleckMaker.ThrowAirPuffUp(cell.ToVector3Shifted(), map);
                        }
                    }
                    
                    // Apply immediate damage to pawns in gas area
                    var pawnsInCell = cell.GetThingList(map).OfType<Pawn>().ToList();
                    foreach (var pawn in pawnsInCell)
                    {
                        if (pawn.Dead) continue;
                        
                        var damageAmount_actual = isCustom ? damageAmount * gasIntensity * 0.3f : 8f * gasIntensity;
                        var damageInfo = new DamageInfo(damageDef, damageAmount_actual);
                        pawn.TakeDamage(damageInfo);
                        
                        // Add gas-specific effects
                        if (defName.Contains("toxic") && pawn.health?.hediffSet != null)
                        {
                            // Try to add food poisoning as a toxic effect
                            try
                            {
                                var hediffDef = HediffDefOf.FoodPoisoning;
                                if (hediffDef != null && !pawn.health.hediffSet.HasHediff(hediffDef))
                                {
                                    pawn.health.AddHediff(hediffDef);
                                }
                            }
                            catch { /* Ignore if hediff fails */ }
                        }
                    }
                }
                
                // Create additional delayed gas effects for persistence
                CreateDelayedGasEffects(center, damageDef, radius);
                
                Messages.Message($"{damageDef.label} gas cloud created (radius {radius:F1}) - enhanced persistent effects", 
                    MessageTypeDefOf.NeutralEvent);
                    
            }
            catch (System.Exception ex)
            {
                Log.Error($"GUIDevMode: Failed to create gas explosion: {ex}");
                // Fallback to regular explosion
                if (isCustom)
                {
                    GenExplosion.DoExplosion(center, Find.CurrentMap, radius, damageDef, null, Mathf.RoundToInt(damageAmount));
                }
                else
                {
                    GenExplosion.DoExplosion(center, Find.CurrentMap, radius, damageDef, null);
                }
            }
        }
        
        /// <summary>
        /// Creates delayed gas effects for better persistence without complex scheduling
        /// </summary>
        private static void CreateDelayedGasEffects(IntVec3 center, DamageDef damageDef, float radius)
        {
            try
            {
                var map = Find.CurrentMap;
                if (map == null) return;
                
                // Create multiple waves of gas effects with delays
                for (int wave = 1; wave <= 3; wave++)
                {
                    var delay = wave * 120; // 2 seconds per wave
                    var waveRadius = radius * (1f - wave * 0.2f); // Gradually shrinking
                    
                    // Use a simple timer approach
                    var effectCells = GenRadial.RadialCellsAround(center, waveRadius, true).ToList();
                    
                    foreach (var cell in effectCells.Take(20)) // Limit to prevent performance issues
                    {
                        if (!cell.InBounds(map)) continue;
                        
                        var distance = cell.DistanceTo(center);
                        if (distance > waveRadius) continue;
                        
                        // Create lingering visual effects
                        for (int i = 0; i < 2; i++)
                        {
                            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 2f + wave);
                            FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 2f + wave, GetGasColor(damageDef));
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"GUIDevMode: Failed to create delayed gas effects: {ex}");
            }
        }
        
        /// <summary>
        /// Gets appropriate color for gas type
        /// </summary>
        private static Color GetGasColor(DamageDef damageDef)
        {
            var defName = damageDef.defName.ToLowerInvariant();
            
            if (defName.Contains("toxic") || defName.Contains("poison")) return Color.green;
            if (defName.Contains("acid")) return Color.yellow;
            if (defName.Contains("smoke")) return Color.gray;
            if (defName.Contains("chemical")) return new Color(0.5f, 1f, 0.5f); // Light green
            
            return Color.gray; // Default gas color
        }
    }
}