using Verse;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using System.Linq;
using UnityEngine;

namespace Mod_warult
{
    public class Hediff_QuestTracker : Hediff
    {
        public string currentQuestId = "Prologue_Start";
        public HashSet<string> completedQuests = new HashSet<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentQuestId, "currentQuestId", "Prologue_Start");
            Scribe_Collections.Look(ref completedQuests, "completedQuests", LookMode.Value);

            if (completedQuests == null)
                completedQuests = new HashSet<string>();
        }

        public void CompleteCurrentQuest()
        {
            if (string.IsNullOrEmpty(currentQuestId)) return;

            if (!QuestManager.AllQuests.ContainsKey(currentQuestId))
            {
                Log.Error("Expedition33_UnknownQuestError".Translate(currentQuestId));
                return;
            }

            var currentQuestData = QuestManager.AllQuests[currentQuestId];
            string completedQuestId = currentQuestId;
            completedQuests.Add(currentQuestId);
            TriggerSpecialEvents(completedQuestId);
            currentQuestId = currentQuestData.nextQuestId;

            if (!string.IsNullOrEmpty(currentQuestId) && QuestManager.AllQuests.ContainsKey(currentQuestId))
            {
                var nextQuestData = QuestManager.AllQuests[currentQuestId];
                TriggerBossSiteForQuest(currentQuestId);
                Messages.Message("Expedition33_QuestCompleted".Translate(nextQuestData.title), 
                    MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Expedition33_MainQuestlineCompleted".Translate(), 
                    MessageTypeDefOf.PositiveEvent);
            }

            if (currentQuestId == "ActeII_VersoArrival")
            {
                NarrativeEvents.TriggerVersoArrival();
            }
            else if (currentQuestId == "ActeI_Final")
            {
                Messages.Message("Expedition33_ActIEnding".Translate(),
                    MessageTypeDefOf.PositiveEvent);
            }
        }

        private void TriggerBossSiteForQuest(string questId)
        {
            if (questId == "ActeII_LesAxons")
            {
                TriggerIncident("Expedition33_SpawnSireneSite");
                TriggerIncident("Expedition33_SpawnVisagesSite");
                return;
            }

            string incidentDefName = GetIncidentForQuest(questId);
            if (string.IsNullOrEmpty(incidentDefName)) return;

            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Log.Warning("Expedition33_IncidentNotFound".Translate(incidentDefName));
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Nevrons")) ?? Find.FactionManager.RandomEnemyFaction();

            if (incidentDef.Worker.TryExecute(parms))
            {
                Log.Message("Expedition33_BossSiteGenerated".Translate(questId));
            }
        }

        private void TriggerIncident(string incidentDefName)
        {
            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Log.Warning("Expedition33_IncidentNotFound".Translate(incidentDefName));
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Nevrons")) ?? Find.FactionManager.RandomEnemyFaction();

            if (incidentDef.Worker.TryExecute(parms))
                Log.Message("Expedition33_BossSiteGenerated".Translate(incidentDefName));
        }

        private string GetIncidentForQuest(string questId)
        {
            return questId switch
            {
                "ActeI_VallonsFleuris" => "Expedition33_SpawnEvequeSite",
                "ActeI_OceanSuspendu" => "Expedition33_SpawnGobluSite",
                "ActeI_SanctuaireAncien" => "Expedition33_SpawnSakapatateUltimeSite",
                "ActeI_NidEsquie" => "Expedition33_SpawnFrancoisSite",
                "ActeI_Final" => "Expedition33_SpawnMaitreDesLampesSite",
                "ActeII_TerresOubliees" => "Expedition33_SpawnDualisteSite",
                "ActeII_Manoir" => "Expedition33_SpawnRenoirSite",
                "ActeII_LesAxons" => "Expedition33_SpawnSireneSite",
                "ActeII_Final" => "Expedition33_SpawnPaintressSite",
                _ => null
            };
        }

        private void TriggerSpecialEvents(string completedQuestId)
        {
            switch (completedQuestId)
            {
                case "ActeI_Final":
                    NarrativeEvents.TriggerActeICompletion();
                    break;
                case "ActeII_VersoArrival":
                    NarrativeEvents.TriggerVersoArrival();
                    break;
                case "ActeII_Final":
                    PaintressManager.SpawnPaintressOnObscurContinent();
                    break;
            }
        }

        public void TriggerQuestEvent(string eventType)
        {
            if (string.IsNullOrEmpty(currentQuestId)) return;
            if (completedQuests.Contains(currentQuestId)) return;

            if (QuestManager.AllQuests.ContainsKey(currentQuestId))
            {
                var currentQuest = QuestManager.AllQuests[currentQuestId];
                Log.Message("Expedition33_EventCheck".Translate(eventType, currentQuest.triggerCondition));
                
                if (currentQuest.triggerCondition == eventType)
                {
                    Log.Message("Expedition33_QuestProgression".Translate());
                    CompleteCurrentQuest();
                }
            }
        }

        public static void TriggerGlobalEvent(string eventType)
        {
            foreach (var colonist in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
            {
                var tracker = colonist.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker")) as Hediff_QuestTracker;
                
                if (tracker != null)
                {
                    Log.Message("Expedition33_GlobalEventTrigger".Translate(eventType));
                    tracker.TriggerQuestEvent(eventType);
                    return;
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class ExpeditionModInitializer
    {
        static ExpeditionModInitializer()
        {
            var harmony = new Harmony("mod_warult.expedition33");
            harmony.PatchAll();
            Log.Message("Expedition33_ModInitialized".Translate());
        }
    }
}
