using UnityEngine;
using Verse;
using RimWorld;

namespace GUIDevMode
{
    public class GUIDevModeDesignator : Designator
    {
        public GUIDevModeDesignator()
        {
            defaultLabel = "GUI Dev Mode";
            defaultDesc = "Open GUI Developer Mode window";
            icon = ContentFinder<Texture2D>.Get("UI/Commands/SelectNext", true);
            useMouseIcon = true;
            soundSucceeded = SoundDefOf.Tick_High;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            GUIDevModeButton.ToggleWindow();
        }

        protected override void FinalizeDesignationSucceeded()
        {
            base.FinalizeDesignationSucceeded();
            // Auto-deselect this tool after use
            Find.DesignatorManager.Deselect();
        }
    }
}