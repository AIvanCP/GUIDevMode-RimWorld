using System;
using UnityEngine;
using Verse;

namespace GUIDevMode
{
    public class GUIDevModeMod : Mod
    {
        public static GUIDevModeSettings Settings { get; private set; }

        public GUIDevModeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GUIDevModeSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            
            listing.Label("Tool Categories:");
            listing.CheckboxLabeled("Enable Item Tools", ref Settings.enableItemTools);
            listing.CheckboxLabeled("Enable Terrain Tools", ref Settings.enableTerrainTools);
            listing.CheckboxLabeled("Enable Kill Tools", ref Settings.enableKillTools);
            listing.CheckboxLabeled("Enable Explosion Tools", ref Settings.enableExplosionTools);
            
            listing.Gap(12f);
            
            listing.Label("Interface:");
            listing.CheckboxLabeled("Use Bottom Bar Placement", ref Settings.useBottomBarPlacement);
            listing.CheckboxLabeled("Show Tool Tips", ref Settings.showToolTips);
            listing.CheckboxLabeled("Confirm Destructive Actions", ref Settings.confirmDestructiveActions);
            listing.CheckboxLabeled("Explosion Radius Preview Persistent", ref Settings.explosionRadiusPreviewPersistent, 
                "Show explosion radius preview continuously during targeting");
            listing.CheckboxLabeled("Use Quick Skip Mode", ref Settings.useQuickSkipMode,
                "Use instant time skip instead of tick simulation (may cause inconsistencies)");
            
            listing.Gap(12f);
            
            listing.Label("Performance:");
            listing.CheckboxLabeled("Limit Item Display", ref Settings.limitItemDisplay,
                "Limit the number of items shown to prevent lag with large mod lists");
            
            if (Settings.limitItemDisplay)
            {
                listing.Label($"Item Display Limit: {Settings.itemDisplayLimit}");
                Settings.itemDisplayLimit = (int)listing.Slider(Settings.itemDisplayLimit, 500f, 5000f);
            }
            
            listing.Gap(12f);
            
            listing.Label($"Button Text: {Settings.buttonText}");
            Settings.buttonText = listing.TextEntry(Settings.buttonText);
            
            listing.Label($"Button Size: {Settings.buttonSize:F0}");
            Settings.buttonSize = listing.Slider(Settings.buttonSize, 100f, 300f);
            
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "GUI Developer Mode";
        }
    }

    [StaticConstructorOnStartup]
    public static class GUIDevModeInitializer
    {
        static GUIDevModeInitializer()
        {
            try
            {
                Log.Message("[GUI Developer Mode] Mod initialized successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[GUI Developer Mode] Failed to initialize mod: {ex}");
            }
        }
    }
}