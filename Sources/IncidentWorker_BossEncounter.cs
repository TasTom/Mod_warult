using System;
using System.Linq;                    // ✅ manquant
using RimWorld;
using Verse;
using UnityEngine;

namespace Mod_warult
{
    public class IncidentWorker_BossEncounter : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            /* 1. Quel boss doit apparaître ? */
            string bossToSpawn = GetBossForCurrentQuest();
            if (string.IsNullOrEmpty(bossToSpawn))
                return false;

            /* 2. Récupération du PawnKindDef sans risque d’exception */
            PawnKindDef bossKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(bossToSpawn);
            if (bossKind == null)
            {
                Log.Error($"[Expedition33] PawnKindDef introuvable : {bossToSpawn}");
                return false;
            }

            /* 3. Génération du boss */
            Pawn boss = PawnGenerator.GeneratePawn(bossKind);

            /* 4. Choix de la case de spawn (fallback : centre de carte) */
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(map.Center, map, out IntVec3 spawnSpot, 50))
                spawnSpot = map.Center;

            GenSpawn.Spawn(boss, spawnSpot, map);

            /* 5. Message d’alerte */
            Messages.Message(
                $"⚔️ {boss.LabelShort.CapitalizeFirst()} apparaît près de votre base !",
                new TargetInfo(spawnSpot, map),
                MessageTypeDefOf.ThreatBig
            );
            return true;
        }

        /* ------------------------------------------------------------------ */
        /* Méthode d’aiguillage selon la quête                               */
        /* ------------------------------------------------------------------ */
        private string GetBossForCurrentQuest()
        {
            Pawn colonistWithTracker = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                .FirstOrDefault(p =>
                    p.health.hediffSet.GetFirstHediffOfDef(
                        DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker")) != null);

            if (colonistWithTracker == null) return null;

            var tracker = colonistWithTracker.health.hediffSet
                .GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker"))
                as Hediff_QuestTracker;

            return tracker?.currentQuestId switch
            {
                "ActeI_VallonsFleuris"      => "Expedition33_Eveque",
                "ActeI_OceanSuspendu"       => "Expedition33_Goblu",
                "ActeI_SanctuaireAncien"    => "Expedition33_SakapatateUltime",
                "ActeI_NidEsquie"           => "Expedition33_Francois",
                "ActeI_Final"               => "Expedition33_MaitreDesLampes",
                "ActeII_TerresOubliees"     => "Expedition33_Dualiste",
                "ActeII_Manoir"             => "Expedition33_Renoir",
                "ActeII_LesAxons"           => "Expedition33_Sirene",
                _                           => null
            };
        }
    }
}
