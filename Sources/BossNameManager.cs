using HarmonyLib;
using Verse;

namespace Mod_warult
{
    public static class BossNameManager
    {
        public static void InitializeBossName(Pawn pawn)
        {
            if (pawn?.Name is NameTriple ||
                (pawn?.Name is NameSingle single && single.Name == "Verso"))
                return;

            if (pawn?.kindDef?.defName == "Expedition_Verso")
            {
                pawn.Name = new NameSingle("Expedition_VersoName".Translate());
                return;
            }

            string bossTitle = GetBossTitle(pawn);
            pawn.Name = new NameTriple("", bossTitle, "");
        }

        private static string GetBossTitle(Pawn pawn)
        {
            return pawn?.kindDef?.defName switch
            {
                "Expedition33_Eveque" => "Expedition33_BishopTitle".Translate(),
                "Expedition33_Dualiste" => "Expedition33_DualistTitle".Translate(),
                "Expedition33_SakapatateRobuste" => "Expedition33_RobustSakapatateTitle".Translate(),
                "Expedition33_SakapatateUltime" => "Expedition33_UltimateSakapatateTitle".Translate(),
                "Expedition33_Francois" => "Expedition33_FrancoisTitle".Translate(),
                "Expedition33_MaitreDesLampes" => "Expedition33_LampmasterTitle".Translate(),
                "Expedition33_Renoir" => "Expedition33_RenoirTitle".Translate(),
                "Expedition33_Sirene" => "Expedition33_SirenTitle".Translate(),
                "Expedition33_Visages" => "Expedition33_FacesTitle".Translate(),
                "Expedition33_Paintress" => "Expedition33_PaintressTitle".Translate(),
                "Expedition_Verso" => "Expedition_VersoTitle".Translate(),
                "Expedition33_Mime" => "Expedition33_MimeTitle".Translate(),
                "Expedition33_Goblu" => "Expedition33_GobluTitle".Translate(),
                _ => "Expedition33_MysteriousBoss".Translate()
            };
        }
    }
}
