using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Mod_warult
{
    // ═══════════════════════════════════════════════════════════
    // PATCH PRINCIPAL : MORT DES BOSS
    // ═══════════════════════════════════════════════════════════

    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Patch_BossDefeated
    {
        [HarmonyPostfix]
        static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            try
            {
                // Gestion des boss d'expédition
                if (__instance.kindDef.defName.StartsWith("Expedition33_"))
                {
                    string bossId = __instance.kindDef.defName;
                    string trigger = "BOSS_DEFEATED_" + bossId;

                    Log.Message($"[Expedition33] Boss tué: {bossId}");
                    TriggerQuestEventForAllTrackers(trigger);

                    // Supprime le site du monde si il existe
                    // RemoveBossSiteFromWorld(bossId);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Expedition33] Erreur patch mort boss: {ex.Message}");
            }
        }

        private static void TriggerQuestEventForAllTrackers(string eventType)
        {
            var tracker = QuestEventUtility.FindQuestTracker();
            if (tracker != null)
            {
                Log.Message($"[Expedition33] Déclenchement: {eventType}");
                tracker.TriggerQuestEvent(eventType);
            }
            else
            {
                Log.Warning("[Expedition33] Aucun Quest Tracker trouvé");
            }
        }

        private static void RemoveBossSiteFromWorld(string bossDefName)
        {
            try
            {
                var sites = Find.WorldObjects.AllWorldObjects.OfType<Site>().ToList();
                foreach (var site in sites)
                {
                    // Vérifie si le site correspond au boss tué
                    if (site.Label.Contains(GetSiteLabelFromBoss(bossDefName)))
                    {
                        site.Destroy();
                        Log.Message($"[Expedition33] Site supprimé: {site.Label}");
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Expedition33] Erreur suppression site: {ex.Message}");
            }
        }

        private static string GetSiteLabelFromBoss(string bossDefName)
        {
            return bossDefName switch
            {
                "Expedition33_Eveque" => "Vallons Fleuris",
                "Expedition33_Goblu" => "Océan Suspendu",
                "Expedition33_SakapatateUltime" => "Sanctuaire Ancien",
                "Expedition33_Francois" => "Nid d'Esquie",
                "Expedition33_MaitreDesLampes" => "Sanctuaire de Lumière",
                "Expedition33_Dualiste" => "Terres Oubliées",
                "Expedition33_Renoir" => "Manoir de Renoir",
                "Expedition33_Sirene" => "Domaine de la Sirène",
                "Expedition33_Visages" => "Palais des Visages",
                "Expedition33_Peintresse" => "Monolithe",
                _ => ""
            };
        }
    }

    // ═══════════════════════════════════════════════════════════
    // PATCH SIMPLE : FORMATION DE CARAVANE
    // ═══════════════════════════════════════════════════════════

  


    // ═══════════════════════════════════════════════════════════
    // UTILITAIRES PARTAGÉS
    // ═══════════════════════════════════════════════════════════

    public static class QuestEventUtility
{
    public static Hediff_QuestTracker FindQuestTracker()
    {
        return PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
            .Select(p => p.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker")) as Hediff_QuestTracker)
            .FirstOrDefault(h => h != null);
    }

    public static void TriggerGlobalEvent(string eventType)
    {
        var tracker = FindQuestTracker();
        tracker?.TriggerQuestEvent(eventType);
    }

    public static void RemoveBossSite(string bossDefName)
    {
        string labelFragment = bossDefName switch
        {
            "Expedition33_Eveque"               => "Vallons Fleuris",
            "Expedition33_Goblu"                => "Océan Suspendu",
            "Expedition33_SakapatateUltime"    => "Sanctuaire Ancien",
            "Expedition33_Francois"            => "Nid d'Esquie",
            "Expedition33_MaitreDesLampes"     => "Sanctuaire de Lumière",
            "Expedition33_Dualiste"            => "Terres Oubliées",
            "Expedition33_Renoir"              => "Manoir de Renoir",
            "Expedition33_Sirene"              => "Domaine de la Sirène",
            "Expedition33_Visages"             => "Palais des Visages",
            "Expedition33_Peintresse"          => "Monolithe",
            _                                   => string.Empty
        };

        if (labelFragment.NullOrEmpty()) return;

        foreach (var site in Find.WorldObjects.AllWorldObjects.OfType<Site>())
            if (site.Label.Contains(labelFragment))
            {
                site.Destroy();
                Log.Message($"[Expedition33] Site retiré : {site.Label}");
                break;
            }
    }
}

}
    
