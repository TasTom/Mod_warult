using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace Mod_warult
{
    [HarmonyPatch(typeof(PawnBioAndNameGenerator), "GiveAppropriateBioAndNameTo")]
    public static class BossNameAndBackstoryPatch
    {
        private static readonly Dictionary<string, (string name, string childhood, string adulthood)> BossData =
            new Dictionary<string, (string, string, string)>
            {
                ["Expedition33_Eveque"] = ("L'Évêque", "Expedition33_Eveque_Childhood_Debug", "Expedition33_Eveque_Adulthood_Debug"),
                ["Expedition33_Dualiste"] = ("Le Dualiste", "Expedition33_Dualiste_Childhood_Debug", "Expedition33_Dualiste_Adulthood_Debug"),
                ["Expedition33_SakapatateRobuste"] = ("Sakapatate le Robuste", "Expedition33_SakapatateRobuste_Childhood_Debug", "Expedition33_SakapatateRobuste_Adulthood_Debug"),
                ["Expedition33_SakapatateUltime"] = ("Sakapatate l'Ultime", "Expedition33_SakapatateUltime_Childhood_Debug", "Expedition33_SakapatateUltime_Adulthood_Debug"),
                ["Expedition33_Francois"] = ("François l'Immortel", "Expedition33_Francois_Childhood_Debug", "Expedition33_Francois_Adulthood_Debug"),
                ["Expedition33_MaitreDesLampes"] = ("Le Maître des Lampes", "Expedition33_MaitreLampes_Childhood_Debug", "Expedition33_MaitreLampes_Adulthood_Debug"),
                ["Expedition33_Renoir"] = ("Commandant Renoir", "Expedition33_CommandantRenoir_Childhood_Debug", "Expedition33_CommandantRenoir_Adulthood_Debug"),
                ["Expedition33_Sirene"] = ("La Sirène", "Expedition33_Sirene_Childhood_Debug", "Expedition33_Sirene_Adulthood_Debug"),
                ["Expedition33_Visages"] = ("Les Visages", "Expedition33_Visages_Childhood_Debug", "Expedition33_Visages_Adulthood_Debug"),
                ["Expedition33_Paintress"] = ("La Paintress", "Expedition33_Paintress_Childhood_Debug", "Expedition33_Paintress_Adulthood_Debug"),
                ["Expedition_Verso"] = ("Verso", "Expedition_Verso_Childhood_Debug", "Expedition_Verso_Adulthood_Debug"),
                ["Expedition33_Mime"] = ("Le Mime", "Expedition33_Mime_Childhood_Debug", "Expedition33_Mime_Adulthood_Debug"),
                ["Expedition33_Goblu"] = ("Le Goblu", "Expedition33_Goblu_Childhood_Debug", "Expedition33_Goblu_Adulthood_Debug")
            };

        public static void Postfix(Pawn pawn)
        {
            if (pawn?.kindDef?.defName == null || !BossData.ContainsKey(pawn.kindDef.defName))
                return;

            try
            {
                var bossInfo = BossData[pawn.kindDef.defName];

                pawn.Name = new NameTriple("", bossInfo.name, "");

                if (pawn.story != null)
                {
                    var childhood = DefDatabase<BackstoryDef>.GetNamedSilentFail(bossInfo.childhood);
                    var adulthood = DefDatabase<BackstoryDef>.GetNamedSilentFail(bossInfo.adulthood);

                    if (childhood != null)
                        pawn.story.Childhood = childhood;
                    if (adulthood != null)
                        pawn.story.Adulthood = adulthood;
                }

                Log.Message("Expedition33_BossGeneratedSuccess".Translate(bossInfo.name));
            }
            catch (System.Exception ex)
            {
                Log.Error("Expedition33_BossGenerationError".Translate(pawn.kindDef.defName, ex.Message));
            }
        }
    }
}
