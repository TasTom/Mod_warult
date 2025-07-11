using RimWorld;
using Verse;

namespace Mod_warult
{

    public class CompBossPhases : ThingComp
    {
        private bool phase2Triggered = false;

        public override void CompTick()
        {
            if (parent is Pawn boss && !phase2Triggered)
            {
                float healthPercent = boss.health.summaryHealth.SummaryHealthPercent;

                if (healthPercent <= 0.5f)
                {
                    TriggerPhase2(boss);
                    phase2Triggered = true;
                }
            }
        }

        private void TriggerPhase2(Pawn boss)
        {
            var bossExt = boss.kindDef.GetModExtension<BossExtension>();

            if (bossExt?.bossType == "Sacred")
            {
                // Evêque : Aura de régénération
                var regenHediff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamed("Expedition33_SacredAura"), boss);
                boss.health.AddHediff(regenHediff);

                Messages.Message("✨ L'Evêque entre en phase sacrée !", MessageTypeDefOf.ThreatBig);
            }
        }
    }

}