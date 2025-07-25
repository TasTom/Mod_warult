using RimWorld;
using Verse;

namespace Mod_warult
{
    public class CompDualityMaster : ThingComp
    {
        public bool IsLightMode = true;
        private int lastShiftTick = 0;
        
        public override void CompTick()
        {
            base.CompTick();
            
            // Change automatiquement de mode toutes les 30 secondes si pas utilisé manuellement
            if (GenTicks.TicksGame - lastShiftTick > 1800)
            {
                ShiftPolarity();
            }
        }
        
        public void ShiftPolarity()
        {
            IsLightMode = !IsLightMode;
            lastShiftTick = GenTicks.TicksGame;
            
            if (parent is Pawn pawn)
            {
                // Supprime les anciens hediffs de mode
                var oldMode = pawn.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamed(IsLightMode ? "Expedition33_DarkMode" : "Expedition33_LightMode"));
                if (oldMode != null)
                {
                    pawn.health.RemoveHediff(oldMode);
                }
                
                // Applique le nouveau mode
                string newModeHediff = IsLightMode ? "Expedition33_LightMode" : "Expedition33_DarkMode";
                var modeHediff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamed(newModeHediff), pawn);
                pawn.health.AddHediff(modeHediff);
            }
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref IsLightMode, "isLightMode", true);
            Scribe_Values.Look(ref lastShiftTick, "lastShiftTick", 0);
        }
    }
}
