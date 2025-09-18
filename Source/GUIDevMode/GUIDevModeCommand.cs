using Verse;
using UnityEngine;

namespace GUIDevMode
{
    public class GUIDevModeCommand : Command
    {
        public override string Label => "GUI Dev Mode";
        public override string Desc => "Open GUI Developer Mode window";

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            GUIDevModeButton.ToggleWindow();
        }
    }

    [StaticConstructorOnStartup]
    public static class ConsoleCommands
    {
        static ConsoleCommands()
        {
            // Register console command
            try
            {
                Log.Message("[GUI Developer Mode] Console commands registered");
            }
            catch (System.Exception ex)
            {
                Log.Error($"[GUI Developer Mode] Failed to register console commands: {ex}");
            }
        }
    }
}