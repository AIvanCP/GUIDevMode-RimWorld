using UnityEngine;
using Verse;
using RimWorld;

namespace GUIDevMode
{
    public class GUIDevModeButton : GameComponent
    {
        private static bool isDragging = false;
        private static Vector2 dragOffset = Vector2.zero;
        private static Rect buttonRect;
        private static bool windowOpen = false;

        public GUIDevModeButton(Game game)
        {
            buttonRect = new Rect(Screen.width - 160f, 10f, 150f, 35f);
        }

        public override void GameComponentOnGUI()
        {
            if (Current.ProgramState != ProgramState.Playing)
                return;

            // Handle F9 key press
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F9)
            {
                ToggleWindow();
                Event.current.Use();
            }

            var settings = GUIDevModeMod.Settings;
            if (settings.useBottomBarPlacement)
            {
                DrawBottomBarButton();
                return;
            }

            DrawTopRightButton();
        }

        private static void DrawTopRightButton()
        {
            var settings = GUIDevModeMod.Settings;
            
            // Ensure button stays within screen bounds
            buttonRect.x = Mathf.Clamp(buttonRect.x, 0f, Screen.width - buttonRect.width);
            buttonRect.y = Mathf.Clamp(buttonRect.y, 0f, Screen.height - buttonRect.height);

            // Handle dragging
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && buttonRect.Contains(Event.current.mousePosition))
            {
                isDragging = true;
                dragOffset = Event.current.mousePosition - new Vector2(buttonRect.x, buttonRect.y);
                current.Use();
            }

            if (isDragging)
            {
                if (current.type == EventType.MouseDrag)
                {
                    Vector2 newPosition = Event.current.mousePosition - dragOffset;
                    buttonRect.x = newPosition.x;
                    buttonRect.y = newPosition.y;
                    current.Use();
                }

                if (current.type == EventType.MouseUp && current.button == 0)
                {
                    isDragging = false;
                    current.Use();
                }
            }

            // Draw button
            if (Widgets.ButtonText(buttonRect, settings.buttonText))
            {
                if (!isDragging) // Only open if not dragging
                {
                    ToggleWindow();
                }
            }

            // Add tooltip
            if (Mouse.IsOver(buttonRect) && settings.showToolTips)
            {
                TooltipHandler.TipRegion(buttonRect, "Click to open GUI Developer Mode\nDrag to reposition");
            }
        }

        private static void DrawBottomBarButton()
        {
            var settings = GUIDevModeMod.Settings;
            
            // Position button at bottom of screen
            var bottomButtonRect = new Rect(
                (Screen.width - settings.buttonSize) / 2f, // Center horizontally
                Screen.height - settings.buttonSize - 10f, // Bottom with some margin
                settings.buttonSize,
                30f // Standard bottom bar height
            );

            if (Widgets.ButtonText(bottomButtonRect, settings.buttonText))
            {
                ToggleWindow();
            }

            // Add tooltip
            if (Mouse.IsOver(bottomButtonRect) && settings.showToolTips)
            {
                TooltipHandler.TipRegion(bottomButtonRect, "Click to open GUI Developer Mode");
            }
        }

        public static void ToggleWindow()
        {
            if (Find.WindowStack.IsOpen<GUIDevModeWindow>())
            {
                Find.WindowStack.TryRemove(typeof(GUIDevModeWindow));
                windowOpen = false;
            }
            else
            {
                var window = new GUIDevModeWindow();
                Find.WindowStack.Add(window);
                windowOpen = true;
            }
        }

        public static void SetWindowOpen(bool open)
        {
            windowOpen = open;
        }

        public static bool IsWindowOpen()
        {
            return windowOpen;
        }
    }
}