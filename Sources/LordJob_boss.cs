using System;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Mod_warult
{
    public class LordJob_DefendBossSite : LordJob
    {
        private IntVec3 center;
        private float radius;

        public LordJob_DefendBossSite() { }

        public LordJob_DefendBossSite(IntVec3 center, float radius = 20f)
        {
            this.center = center;
            this.radius = radius;
        }

        public override StateGraph CreateGraph()
        {
            var graph = new StateGraph();
            var defend = new LordToil_DefendPoint(center, radius);
            graph.AddToil(defend);
            graph.StartingToil = defend;
            return graph;
        }

        // ✅ SOLUTION : Vérification et correction des duties
        public override void LordJobTick()
        {
            base.LordJobTick();
            
            // Vérifier toutes les 60 ticks (1 seconde)
            if (GenTicks.TicksGame % 60 == 0)
            {
                EnsureValidDuties();
            }
        }

        private void EnsureValidDuties()
        {
            if (lord?.ownedPawns == null) return;
            
            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn?.mindState == null) continue;
                
                // Vérifier si la duty est valide
                if (IsInvalidDuty(pawn.mindState.duty))
                {
                    // Créer une duty valide
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, center, radius);
                    
                    if (Prefs.DevMode)
                    {
                        Log.Message("Expedition33_DutyCorrected".Translate(pawn.LabelShort));
                    }
                }
            }
        }

        private bool IsInvalidDuty(PawnDuty duty)
        {
            return duty == null ||
                   duty.focus == null ||
                   !duty.focus.IsValid ||
                   duty.radius <= 0;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref center, "center");
            Scribe_Values.Look(ref radius, "radius", 20f);
        }
    }
}
