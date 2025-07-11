using Verse;

namespace Mod_warult
{
    public static class BossNameManager
    {
        public static void InitializeBossName(Pawn pawn)
        {
            if (pawn?.Name is NameTriple) return; // Déjà initialisé
            
            string bossTitle = GetBossTitle(pawn);
            pawn.Name = new NameTriple("", bossTitle, "");
            
            // ✅ SUPPRIMÉ : RemoveHumanLore qui causait l'erreur
            // Ne pas modifier les backstories ici
        }
        
        private static string GetBossTitle(Pawn pawn)
        {
            return pawn?.kindDef?.defName switch
            {
                "Expedition33_Eveque" => "L'Évêque",
                "Expedition33_Dualiste" => "Le Dualiste", 
                "Expedition33_SakapatateRobuste" => "Sakapatate le Robuste",
                "Expedition33_SakapatateUltime" => "Sakapatate l'Ultime",
                "Expedition33_Francois" => "François l'Immortel",
                "Expedition33_MaitreDesLampes" => "Le Maître des Lampes",
                "Expedition33_Renoir" => "Commandant Renoir",
                "Expedition33_Sirene" => "La Sirène",
                "Expedition33_Visages" => "Les Visages",
                "Expedition33_Peintresse" => "La Peintresse",
                "Expedition33_Verso" => "Verso",
                "Expedition33_Mime" => "Le Mime",
                "Expedition33_Goblu" => "Le Goblu",
                _ => "Boss Mystérieux"
            };
        }
    }
}
