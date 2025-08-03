using Verse;

namespace Mod_warult
{
    public class BossAbility
    {
        public string name;
        public float range;
        public bool selfCast;

        public BossAbility(string name, float range, bool selfCast)
        {
            this.name = name;
            this.range = range;
            this.selfCast = selfCast;
        }

        public override string ToString()
        {
            return "Expedition33_AbilityInfo".TranslateSimple().Formatted(name, range, selfCast.ToString());
        }

        public enum BossPhase
        {
            Phase1_Normal,
            Phase2_Aggressive, 
            Phase3_Desperate
        }

        private BossPhase GetCurrentPhase(Pawn pawn)
        {
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
            return healthPercent switch
            {
                >= 0.7f => BossPhase.Phase1_Normal,
                >= 0.4f => BossPhase.Phase2_Aggressive,
                _ => BossPhase.Phase3_Desperate
            };
        }
    }
}
