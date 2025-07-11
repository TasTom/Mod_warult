using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Mod_warult
{
    public class BossExtension : DefModExtension
    {
        public string bossType;
        public string questReward;
        public string customThinkTree; // Gardé pour référence
        public string summonMinions;
        public float commandRadius = 15f;
        public string weaknessElement;
        public string resistanceElement;

        public string creatureName;
        public bool isLegendaryEntity;
        public List<string> specialMechanics = new List<string>();

        // Méthode simplifiée sans modification directe du ThinkTree
        public void ApplyCustomAI(Pawn pawn)
        {
            Log.Message($"[Expedition33] Boss {pawn.LabelShort} initialisé avec type : {bossType}");

            // Pas de modification directe du ThinkTree
            // L'IA personnalisée se fait via les JobGivers dans les ThinkTreeDefs
        }

       
    }
    
    
}
