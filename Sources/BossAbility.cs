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

        public enum BossPhase
        {
            Phase1_Normal,      // 100% - 70% santé
            Phase2_Aggressive,  // 70% - 40% santé  
            Phase3_Desperate    // 40% - 0% santé
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
