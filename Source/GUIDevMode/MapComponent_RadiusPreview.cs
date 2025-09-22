using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    /// <summary>
    /// MapComponent that handles continuous rendering of radius previews for explosions and growth zones.
    /// This ensures previews remain visible without requiring camera movement.
    /// </summary>
    public class MapComponent_RadiusPreview : MapComponent
    {
        // Explosion preview state
        public static bool explosionPreviewActive = false;
        public static DamageDef explosionDamageType = null;
        public static float explosionRadius = 0f;
        public static Color explosionColor = Color.red;
        
        // Plant growth preview state
        public static bool plantGrowthPreviewActive = false;
        public static IntVec3 plantGrowthFirstCorner = IntVec3.Invalid;
        public static bool hasPlantGrowthFirstCorner = false;
        
        // Reference to current instance for direct access
        private static MapComponent_RadiusPreview currentInstance = null;
        
        // UI drawing backup system
        private static bool uiDrawingRegistered = false;
        
        public MapComponent_RadiusPreview(Map map) : base(map)
        {
            currentInstance = this;
            Log.Message("[GUI Dev Mode] MapComponent_RadiusPreview instantiated for map");
        }
        
        /// <summary>
        /// Called every frame - handles continuous rendering
        /// </summary>
        public override void MapComponentOnGUI()
        {
            if (map != Find.CurrentMap) return;
            
            // Draw explosion preview if active
            if (explosionPreviewActive && explosionDamageType != null)
            {
                DrawExplosionRadiusPreview();
            }
            
            // Draw plant growth preview if active
            if (plantGrowthPreviewActive)
            {
                DrawPlantGrowthPreview();
            }
        }
        
        /// <summary>
        /// Draws explosion radius preview with real-time mouse following
        /// </summary>
        private void DrawExplosionRadiusPreview()
        {
            var mouseCell = UI.MouseCell();
            if (!mouseCell.InBounds(map)) return;

            // Force redraw by ensuring visibility
            GenDraw.DrawRadiusRing(mouseCell, explosionRadius, explosionColor);
            
            // Additional visual enhancements for better visibility
            var brightColor = Color.yellow;
            brightColor.a = 0.8f;
            GenDraw.DrawRadiusRing(mouseCell, explosionRadius, brightColor);
            
            // Inner damage ring at 75% radius - red for high damage area
            var innerRadius = explosionRadius * 0.75f;
            if (innerRadius > 0.5f)
            {
                var innerColor = Color.red;
                innerColor.a = 0.7f;
                GenDraw.DrawRadiusRing(mouseCell, innerRadius, innerColor);
            }
            
            // Outer effect ring at 125% radius - for area effects like debris
            var outerRadius = explosionRadius * 1.25f;
            var outerColor = explosionColor;
            outerColor.a = 0.4f;
            GenDraw.DrawRadiusRing(mouseCell, outerRadius, outerColor);
            
            // Highlight center cell for targeting precision
            GenDraw.DrawFieldEdges(new List<IntVec3> { mouseCell }, Color.white);
        }
        
        /// <summary>
        /// Draws plant growth zone preview with real-time mouse following
        /// </summary>
        private void DrawPlantGrowthPreview()
        {
            var mouseCell = UI.MouseCell();
            if (!mouseCell.InBounds(map)) return;
            
            var color = Color.green;
            color.a = 0.4f;
            
            if (hasPlantGrowthFirstCorner && plantGrowthFirstCorner.IsValid)
            {
                // Draw area from first corner to current mouse position
                var cells = GetAreaCells(plantGrowthFirstCorner, mouseCell);
                if (cells.Any())
                {
                    GenDraw.DrawFieldEdges(cells, color);
                }
                
                // Highlight corners with different colors and labels
                GenDraw.DrawFieldEdges(new List<IntVec3> { plantGrowthFirstCorner }, Color.yellow);
                GenDraw.DrawFieldEdges(new List<IntVec3> { mouseCell }, Color.cyan);
            }
            else
            {
                // Simple single cell highlight - no 3x3 preview area
                GenDraw.DrawFieldEdges(new List<IntVec3> { mouseCell }, color);
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
                    if (cell.InBounds(map))
                    {
                        cells.Add(cell);
                    }
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// Starts explosion radius preview
        /// </summary>
        public static void StartExplosionPreview(DamageDef damageType, float radius, Color color)
        {
            explosionPreviewActive = true;
            explosionDamageType = damageType;
            explosionRadius = radius;
            explosionColor = color;
            
            // Ensure the MapComponent exists and is accessible
            var currentMap = Find.CurrentMap;
            if (currentMap != null)
            {
                var component = currentMap.GetComponent<MapComponent_RadiusPreview>();
                if (component == null)
                {
                    Log.Warning("[GUI Dev Mode] MapComponent_RadiusPreview not found on current map!");
                }
            }
        }
        
        /// <summary>
        /// Stops explosion radius preview
        /// </summary>
        public static void StopExplosionPreview()
        {
            explosionPreviewActive = false;
            explosionDamageType = null;
            explosionRadius = 0f;
            explosionColor = Color.white;
        }
        
        /// <summary>
        /// Starts plant growth zone preview
        /// </summary>
        public static void StartPlantGrowthPreview()
        {
            plantGrowthPreviewActive = true;
            hasPlantGrowthFirstCorner = false;
            plantGrowthFirstCorner = IntVec3.Invalid;
        }
        
        /// <summary>
        /// Sets first corner for plant growth area selection
        /// </summary>
        public static void SetPlantGrowthFirstCorner(IntVec3 corner)
        {
            plantGrowthFirstCorner = corner;
            hasPlantGrowthFirstCorner = true;
        }
        
        /// <summary>
        /// Stops plant growth zone preview
        /// </summary>
        public static void StopPlantGrowthPreview()
        {
            plantGrowthPreviewActive = false;
            hasPlantGrowthFirstCorner = false;
            plantGrowthFirstCorner = IntVec3.Invalid;
        }
        
        /// <summary>
        /// Called every tick to ensure continuous rendering
        /// </summary>
        public override void MapComponentTick()
        {
            // Light touch approach - only force redraw if previews are active
            // Removed Find.MapUI.Notify_SwitchedMap() to prevent interference during gameplay
            if (explosionPreviewActive || plantGrowthPreviewActive)
            {
                base.MapComponentTick();
            }
        }
        
        /// <summary>
        /// Alternative rendering method to ensure previews show consistently
        /// </summary>
        public override void MapComponentDraw()
        {
            // Draw previews using the map drawing system as a backup
            if (explosionPreviewActive && explosionDamageType != null)
            {
                DrawExplosionRadiusPreview();
            }
            
            if (plantGrowthPreviewActive)
            {
                DrawPlantGrowthPreview();
            }
            
            base.MapComponentDraw();
        }
        
        /// <summary>
        /// Called when the map component updates
        /// </summary>
        public override void MapComponentUpdate()
        {
            // Only clear if the targeting systems explicitly say they're not active
            // Don't auto-clear based on Find.Targeter.IsTargeting to prevent interference during gameplay
            if (explosionPreviewActive)
            {
                // Only clear if explosion system explicitly says it's not targeting
                if (!ExplosionSystem.ExplosionTargetingActive)
                {
                    StopExplosionPreview();
                }
            }
            
            if (plantGrowthPreviewActive)
            {
                // Only clear if utility system explicitly says it's not targeting
                if (!UtilityTabsHandler.IsTargetingPlantGrowth)
                {
                    StopPlantGrowthPreview();
                }
            }
        }
    }
}