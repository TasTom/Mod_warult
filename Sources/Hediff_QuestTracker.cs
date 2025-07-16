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

            // Initialise la collection si elle est null après le chargement
            if (completedQuests == null)
                completedQuests = new HashSet<string>();
        }

        public void CompleteCurrentQuest()
        {
            
            if (string.IsNullOrEmpty(currentQuestId)) return;


            

            // Vérification de sécurité
            if (!QuestManager.AllQuests.ContainsKey(currentQuestId))
            {
                Log.Error($"[Expedition33] Quête inconnue: {currentQuestId}");
                return;
            }

            var currentQuestData = QuestManager.AllQuests[currentQuestId];

            string completedQuestId = currentQuestId;
            completedQuests.Add(currentQuestId);
            TriggerSpecialEvents(completedQuestId);
            currentQuestId = currentQuestData.nextQuestId;

            // Messages de progression
            if (!string.IsNullOrEmpty(currentQuestId) && QuestManager.AllQuests.ContainsKey(currentQuestId))
            {
                var nextQuestData = QuestManager.AllQuests[currentQuestId];
                TriggerBossSiteForQuest(currentQuestId);
                Messages.Message($"✅ Quête terminée !\n🆕 Nouvelle quête : {nextQuestData.title}", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("🏆 Félicitations ! Trame narrative principale terminée !", MessageTypeDefOf.PositiveEvent);
            }

            if (currentQuestId == "ActeII_VersoArrival")
            {
                NarrativeEvents.TriggerVersoArrival();


            }
            else if (currentQuestId == "ActeI_Final")
            {
                Messages.Message("🌅 L'Acte I se termine. Une nouvelle phase commence...",
                                MessageTypeDefOf.PositiveEvent);
            }



        }

        private void TriggerBossSiteForQuest(string questId)
        {
            if (questId == "ActeII_LesAxons")
            {
                // Génère les deux sites Axons
                TriggerIncident("Expedition33_SpawnSireneSite");
                TriggerIncident("Expedition33_SpawnVisagesSite");
                return;
            }
            
            string incidentDefName = GetIncidentForQuest(questId);
            if (string.IsNullOrEmpty(incidentDefName)) return;

            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Log.Warning($"[Expedition33] IncidentDef introuvable: {incidentDefName}");
                return;
            }

            // Déclenche l'incident de génération de site
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33_Nevrons")) ?? Find.FactionManager.RandomEnemyFaction();

            if (incidentDef.Worker.TryExecute(parms))
            {
                Log.Message($"[Expedition33] Site boss généré pour la quête: {questId}");
            }

            
        }

        private void TriggerIncident(string incidentDefName)
        {
            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Log.Warning($"[Expedition33] IncidentDef introuvable: {incidentDefName}");
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33_Nevrons")) ?? Find.FactionManager.RandomEnemyFaction();
            if (incidentDef.Worker.TryExecute(parms))
                Log.Message($"[Expedition33] Site boss généré pour l’incident: {incidentDefName}");
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
                "ActeII_LesAxons" => "Expedition33_SpawnSireneSite", // Premier Axon
                "ActeII_Final" => "Expedition33_SpawnPeintresseSite",
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

        public void TriggerDeparture()
        {
            TriggerQuestEvent("EVENT_DEPARTURE");
        }

        private void DrawQuestProgress(Rect rect)
        {
            // Barre de progression des actes
            int totalQuests = QuestManager.AllQuests.Count;
            int finishedQuests = completedQuests.Count;
            float progress = (float)finishedQuests / totalQuests;

            // Création d'une texture de couleur pour la barre
            Color progressColor = Color.Lerp(Color.red, Color.green, progress);
            Texture2D progressTexture = SolidColorMaterials.NewSolidColorTexture(progressColor);


            Widgets.FillableBar(rect, progress, SolidColorMaterials.NewSolidColorTexture(Color.cyan));

            string progressText = $"Progression : {finishedQuests}/{totalQuests} quêtes";
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, progressText);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void GrantQuestReward(string rewardId)
        {
            switch (rewardId)
            {
                case "unlock_indigo_essence":
                    var essence = ThingMaker.MakeThing(
                        DefDatabase<ThingDef>.GetNamed("Expedition33_IndigoEssence"));
                    essence.stackCount = 10;
                    // Ajouter à l'inventaire du joueur
                    break;

                case "unlock_ancient_knowledge":
                    // Débloque des recherches spéciales
                    var researchProjects = DefDatabase<ResearchProjectDef>.AllDefs
                        .Where(r => r.defName.StartsWith("Expedition33_Ancient"));
                    foreach (var project in researchProjects)
                    {
                        Find.ResearchManager.FinishProject(project);
                    }
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
                Log.Message($"[Expedition33] Vérification événement: {eventType} vs {currentQuest.triggerCondition}");

                if (currentQuest.triggerCondition == eventType)
                {
                    Log.Message($"[Expedition33] Événement correspondant trouvé ! Progression de la quête...");
                    CompleteCurrentQuest();
                }
            }
        }
        

        public static void TriggerGlobalEvent(string eventType)
        {
            foreach (var colonist in PawnsFinder.AllCaravansAndTravellingTransporters_Alive)
            {
                var tracker = colonist.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker")) as Hediff_QuestTracker;

                if (tracker != null)
                {
                    Log.Message($"[Expedition33] Déclenchement global: {eventType}");
                    tracker.TriggerQuestEvent(eventType);
                    return;
                }
            }
        }

    }

    // Classe d'initialisation du mod avec Harmony
    [StaticConstructorOnStartup]
    public static class ExpeditionModInitializer
    {
        static ExpeditionModInitializer()
        {
            // Initialise Harmony
            var harmony = new Harmony("mod_warult.expedition33");
            harmony.PatchAll();

            Log.Message("[Expedition33] Mod initialisé avec succès !");
        }
    }

    // Patch pour l'initialisation de nouvelle partie
    [HarmonyPatch(typeof(Game), "InitNewGame")]
    public static class InitNewGame_Patch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            try
            {
                // Trouve le premier colon et lui ajoute le tracker de quête
                var firstColonist = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists.FirstOrDefault();

                if (firstColonist != null)
                {
                    var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_QuestTracker");
                    if (hediffDef != null)
                    {
                        var questTracker = HediffMaker.MakeHediff(hediffDef, firstColonist);
                        firstColonist.health.AddHediff(questTracker);

                        Messages.Message(
                            "🚀 L'Expédition 33 commence !\n📋 Consultez vos missions pour suivre votre progression.",
                            MessageTypeDefOf.PositiveEvent
                        );

                        Log.Message("[Expedition33] Quest Tracker ajouté avec succès au premier colon.");
                    }
                    else
                    {
                        Log.Error("[Expedition33] HediffDef 'Expedition33_QuestTracker' introuvable !");
                    }
                }
                else
                {
                    Log.Warning("[Expedition33] Aucun colon trouvé pour ajouter le Quest Tracker.");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Expedition33] Erreur lors de l'initialisation du Quest Tracker: {ex.Message}");
            }
        }
    }
    


}
