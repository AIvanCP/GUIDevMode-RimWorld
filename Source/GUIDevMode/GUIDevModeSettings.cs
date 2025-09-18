using UnityEngine;
using Verse;

namespace GUIDevMode
{
    public class GUIDevModeSettings : ModSettings
    {
        public bool enableItemTools = true;
        public bool enableTerrainTools = true;
        public bool enableKillTools = true;
        public bool enableExplosionTools = true;
        public bool useBottomBarPlacement = false;
        public string buttonText = "Developer Tools";
        public float buttonSize = 150f;
        public bool showToolTips = true;
        public bool confirmDestructiveActions = true;
        public bool fastConstruction = false;
        public bool explosionRadiusPreviewPersistent = true;
        public bool useQuickSkipMode = false;
        public bool limitItemDisplay = true;
        public int itemDisplayLimit = 2000;
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enableItemTools, "enableItemTools", true);
            Scribe_Values.Look(ref enableTerrainTools, "enableTerrainTools", true);
            Scribe_Values.Look(ref enableKillTools, "enableKillTools", true);
            Scribe_Values.Look(ref enableExplosionTools, "enableExplosionTools", true);
            Scribe_Values.Look(ref useBottomBarPlacement, "useBottomBarPlacement", false);
            Scribe_Values.Look(ref buttonText, "buttonText", "Developer Tools");
            Scribe_Values.Look(ref buttonSize, "buttonSize", 150f);
            Scribe_Values.Look(ref showToolTips, "showToolTips", true);
            Scribe_Values.Look(ref confirmDestructiveActions, "confirmDestructiveActions", true);
            Scribe_Values.Look(ref fastConstruction, "fastConstruction", false);
            Scribe_Values.Look(ref explosionRadiusPreviewPersistent, "explosionRadiusPreviewPersistent", true);
            Scribe_Values.Look(ref useQuickSkipMode, "useQuickSkipMode", false);
            Scribe_Values.Look(ref limitItemDisplay, "limitItemDisplay", true);
            Scribe_Values.Look(ref itemDisplayLimit, "itemDisplayLimit", 2000);
            base.ExposeData();
        }
    }
}