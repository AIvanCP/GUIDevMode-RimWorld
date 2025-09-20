using System;
using UnityEngine;
using Verse;
using System.Reflection;
using HarmonyLib;

namespace GUIDevMode
{
    public class GUIDevModeMod : Mod
    {
        public static GUIDevModeSettings Settings { get; private set; }
        public static Harmony harmony;

        public GUIDevModeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<GUIDevModeSettings>();
            
            // Initialize Harmony
            harmony = new Harmony("AIvanCP.guidevmode");
            
            Log.Message("[GUI Developer Mode] Mod loading...");
            
            try
            {
                // Apply Harmony patches
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Message("[GUI Developer Mode] Harmony patches applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[GUI Developer Mode] Failed to apply Harmony patches: {ex}");
            }
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
            listing.CheckboxLabeled("Hide button (make invisible)", ref Settings.useBottomBarPlacement);
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
                Settings.itemDisplayLimit = (int)listing.Slider(Settings.itemDisplayLimit, 100f, 10000f);
                if (Settings.itemDisplayLimit > 5000)
                {
                    GUI.color = Color.yellow;
                    listing.Label("Warning: High limits may cause lag with large mod lists");
                    GUI.color = Color.white;
                }
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
                
                // Register the key binding action
                LongEventHandler.QueueLongEvent(() =>
                {
                    Log.Message("[GUI Developer Mode] Setting up key bindings...");
                }, "GUIDevMode_Setup", false, null);
            }
            catch (Exception ex)
            {
                Log.Error($"[GUI Developer Mode] Failed to initialize mod: {ex}");
            }
        }
    }
    
    // Harmony patch to ensure our GameComponent gets added when game starts
    [HarmonyPatch(typeof(Game), "InitNewGame")]
    public static class Game_InitNewGame_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Game __instance)
        {
            if (__instance.components.Find(x => x is GUIDevModeButton) == null)
            {
                __instance.components.Add(new GUIDevModeButton(__instance));
                Log.Message("[GUI Developer Mode] GameComponent added successfully");
            }
        }
    }
    
    // Patch for loading existing games
    [HarmonyPatch(typeof(Game), "LoadGame")]
    public static class Game_LoadGame_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            var game = Current.Game;
            if (game != null && game.components.Find(x => x is GUIDevModeButton) == null)
            {
                game.components.Add(new GUIDevModeButton(game));
                Log.Message("[GUI Developer Mode] GameComponent added to loaded game");
            }
        }
    }
}