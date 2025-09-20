using UnityEngine;
using Verse;
using RimWorld;
using System.Collections.Generic;
using System.Linq;

namespace GUIDevMode
{
    public static class BackgroundManager
    {
        private static List<Texture2D> backgroundTextures = new List<Texture2D>();
        private static Texture2D currentBackground = null;
        private static bool texturesLoaded = false;
        private static int currentBackgroundIndex = 0;
        private static int currentFallbackIndex = 0;
        private static bool autoChangeOnOpen = true;
        
        // Default background colors if no textures are found
        private static readonly Color[] fallbackColors = new Color[]
        {
            new Color(0.1f, 0.1f, 0.2f, 0.8f), // Dark blue
            new Color(0.2f, 0.1f, 0.1f, 0.8f), // Dark red
            new Color(0.1f, 0.2f, 0.1f, 0.8f), // Dark green
            new Color(0.2f, 0.15f, 0.1f, 0.8f), // Dark brown
            new Color(0.15f, 0.1f, 0.2f, 0.8f), // Dark purple
            new Color(0.1f, 0.15f, 0.2f, 0.8f), // Steel blue
            new Color(0.2f, 0.1f, 0.15f, 0.8f), // Burgundy
            new Color(0.12f, 0.18f, 0.12f, 0.8f), // Forest green
        };
        private static Color currentFallbackColor = fallbackColors[0];
        
        public static void LoadBackgroundTextures()
        {
            if (texturesLoaded) return;
            
            try
            {
                // Try to load textures from common RimWorld texture paths and mod paths
                var possibleTextures = new string[]
                {
                    "UI/Backgrounds/MenuBackground",
                    "UI/HeroArt/HeroArt_Planet",
                    "UI/HeroArt/HeroArt_Colony1", 
                    "UI/HeroArt/HeroArt_Colony2",
                    "UI/HeroArt/HeroArt_Colony3",
                    "UI/Backgrounds/Background1",
                    "UI/Backgrounds/Background2", 
                    "UI/Backgrounds/Background3",
                    "UI/Misc/ScreenshotModeOn",
                    "Things/Building/Art/SculptureSmall/SculptureSmall_a",
                    "Things/Building/Art/SculptureLarge/SculptureLarge_a",
                    "World/Hills",
                    "World/Mountain",
                    "World/FeatureIcons/Coast",
                    "World/WorldObjects/Sites/GenericSite",
                    "UI/Icons/MainButtons/Research",
                    "UI/Icons/MainButtons/Architect",
                    "UI/Icons/MainButtons/Production",
                    "UI/Icons/MainButtons/Animals",
                    "Things/Ethereal/Sky/SkyDay",
                    "Things/Ethereal/Sky/SkyNight",
                    "Things/Ethereal/Sky/SkySunset",
                };
                
                foreach (var texturePath in possibleTextures)
                {
                    var texture = ContentFinder<Texture2D>.Get(texturePath, false);
                    if (texture != null)
                    {
                        backgroundTextures.Add(texture);
                    }
                }
                
                // Also try to find textures from mods
                try
                {
                    var allTextures = ContentFinder<Texture2D>.GetAllInFolder("UI/Backgrounds/");
                    if (allTextures != null)
                    {
                        foreach (var texture in allTextures)
                        {
                            if (texture != null && !backgroundTextures.Contains(texture))
                            {
                                backgroundTextures.Add(texture);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors when searching mod folders
                }
                
                if (backgroundTextures.Any())
                {
                    Log.Message($"[GUI Dev Mode] Loaded {backgroundTextures.Count} background textures");
                }
                else
                {
                    Log.Message("[GUI Dev Mode] No background textures found, using color backgrounds");
                }
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[GUI Dev Mode] Error loading background textures: {ex.Message}");
            }
            
            texturesLoaded = true;
            SetRandomBackground();
        }
        
        public static void SetRandomBackground()
        {
            if (backgroundTextures.Any())
            {
                currentBackgroundIndex = Rand.Range(0, backgroundTextures.Count);
                currentBackground = backgroundTextures[currentBackgroundIndex];
            }
            else
            {
                currentFallbackIndex = Rand.Range(0, fallbackColors.Length);
                currentFallbackColor = fallbackColors[currentFallbackIndex];
            }
        }
        
        public static void OnWindowOpened()
        {
            if (autoChangeOnOpen)
            {
                SetRandomBackground();
            }
        }
        
        public static void ToggleAutoChange()
        {
            autoChangeOnOpen = !autoChangeOnOpen;
            Messages.Message($"Auto-change backgrounds: {(autoChangeOnOpen ? "ON" : "OFF")}", MessageTypeDefOf.NeutralEvent);
        }
        
        public static bool IsAutoChangeEnabled => autoChangeOnOpen;
        
        public static void SetNextBackground()
        {
            if (backgroundTextures.Any())
            {
                currentBackgroundIndex = (currentBackgroundIndex + 1) % backgroundTextures.Count;
                currentBackground = backgroundTextures[currentBackgroundIndex];
                Messages.Message($"Background {currentBackgroundIndex + 1}/{backgroundTextures.Count}", MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                currentFallbackIndex = (currentFallbackIndex + 1) % fallbackColors.Length;
                currentFallbackColor = fallbackColors[currentFallbackIndex];
                Messages.Message($"Color {currentFallbackIndex + 1}/{fallbackColors.Length}", MessageTypeDefOf.NeutralEvent);
            }
        }
        
        public static void DrawBackground(Rect rect)
        {
            if (currentBackground != null)
            {
                // Draw textured background
                var originalColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.3f); // Semi-transparent
                
                // Scale and position the background texture
                var aspectRatio = (float)currentBackground.width / currentBackground.height;
                var rectAspectRatio = rect.width / rect.height;
                
                Rect bgRect;
                if (aspectRatio > rectAspectRatio)
                {
                    // Texture is wider, fit by height
                    var scaledWidth = rect.height * aspectRatio;
                    bgRect = new Rect(rect.x - (scaledWidth - rect.width) / 2, rect.y, scaledWidth, rect.height);
                }
                else
                {
                    // Texture is taller, fit by width
                    var scaledHeight = rect.width / aspectRatio;
                    bgRect = new Rect(rect.x, rect.y - (scaledHeight - rect.height) / 2, rect.width, scaledHeight);
                }
                
                GUI.DrawTexture(bgRect, currentBackground, ScaleMode.StretchToFill);
                GUI.color = originalColor;
            }
            else
            {
                // Draw color background
                Widgets.DrawBoxSolid(rect, currentFallbackColor);
            }
        }
        
        public static Color GetComplementaryTextColor()
        {
            // Return text color that contrasts well with the current background
            return Color.white; // White text works well with dark/transparent backgrounds
        }
        
        public static Color GetComplementaryUIColor()
        {
            // Return UI color that works well with the current background
            if (currentBackground != null)
            {
                return new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark semi-transparent
            }
            else
            {
                return new Color(0.2f, 0.2f, 0.2f, 0.9f); // Slightly lighter for color backgrounds
            }
        }
    }
}