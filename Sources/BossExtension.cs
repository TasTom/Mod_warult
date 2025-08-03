using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Mod_warult
{
    public class BossExtension : DefModExtension
    {
        public string bossType;
        public string questReward;
        public string customThinkTree;
        public string summonMinions;
        public float commandRadius = 15f;
        public string weaknessElement;
        public string resistanceElement;
        public string creatureName;
        public bool isLegendaryEntity;
        public List<string> specialMechanics = new List<string>();

        public void ApplyCustomAI(Pawn pawn)
        {
            Log.Message("Expedition33_BossInitialized".Translate(pawn.LabelShort, bossType));
        }
    }
}
