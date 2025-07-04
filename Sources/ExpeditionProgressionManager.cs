using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace Mod_warult
{
    public class ExpeditionProgressionManager : IExposable
    {
        // ✅ AJOUT DU PATTERN SINGLETON
        private static ExpeditionProgressionManager _instance;
        private static readonly object _lock = new object();

        public static ExpeditionProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ExpeditionProgressionManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private List<ExpeditionMission> missions;
        private int currentMissionIndex = 0;
        private Dictionary<string, bool> completedObjectives;

        // ✅ AJOUT: HashSet pour les capacités débloquées
        private HashSet<string> unlockedAbilities = new HashSet<string>();

        public ExpeditionProgressionManager()
        {
            InitializeMissions();
            completedObjectives = new Dictionary<string, bool>();
            unlockedAbilities = new HashSet<string>();
            _instance = this; // Assigne l'instance
        }

        // ✅ NOUVELLE MÉTHODE: UnlockAbility pour CompExpeditionProgression
        public void UnlockAbility(string abilityType)
        {
            if (string.IsNullOrEmpty(abilityType)) return;

            if (unlockedAbilities.Add(abilityType))
            {
                Log.Message($"✅ Ability unlocked globally: {abilityType}");

                Messages.Message(
                    $"Capacité débloquée pour toute l'équipe : {abilityType}",
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        // ✅ NOUVELLE MÉTHODE: IsAbilityUnlocked
        public bool IsAbilityUnlocked(string abilityType)
        {
            return !string.IsNullOrEmpty(abilityType) && unlockedAbilities.Contains(abilityType);
        }

        private void InitializeMissions()
        {
            missions = new List<ExpeditionMission>
            {
                new ExpeditionMission
                {
                    id = "mission_01_awakening",
                    title = "Réveil de l'Expédition",
                    description = "Établir une base d'opérations et recruter des Gestrals pour rejoindre la lutte contre Le Gommage.",
                    objectives = new List<string>
                    {
                        "Construire un Quartier Général",
                        "Recruter 3 Gestrals",
                        "Rechercher 'Détection du Gommage'"
                    },
                    rewards = new List<string> { "Accès aux missions de reconnaissance" },
                    nextMissionId = "mission_02_reconnaissance"
                },

                new ExpeditionMission
                {
                    id = "mission_02_reconnaissance",
                    title = "Première Reconnaissance",
                    description = "Envoyer une équipe explorer les zones contaminées par Le Gommage pour cartographier les menaces.",
                    objectives = new List<string>
                    {
                        "Éliminer 10 entités du Gommage",
                        "Collecter 5 échantillons de corruption",
                        "Survivre 3 jours dans une zone contaminée"
                    },
                    rewards = new List<string> { "Déblocage des armes anti-Gommage" },
                    nextMissionId = "mission_03_purification"
                },

                new ExpeditionMission
                {
                    id = "mission_03_purification",
                    title = "Protocole de Purification",
                    description = "Développer et tester des méthodes de purification des zones corrompues par Le Gommage.",
                    objectives = new List<string>
                    {
                        "Fabriquer 3 dispositifs de purification",
                        "Purifier une zone corrompue",
                        "Protéger les dispositifs pendant 2 jours"
                    },
                    rewards = new List<string> { "Technologie de purification avancée" },
                    nextMissionId = "mission_04_boss_mineur"
                },

                new ExpeditionMission
                {
                    id = "mission_04_boss_mineur",
                    title = "Confrontation Majeure",
                    description = "Affronter un Avatar du Gommage, une manifestation puissante de la corruption.",
                    objectives = new List<string>
                    {
                        "Localiser l'Avatar du Gommage",
                        "Vaincre l'Avatar en combat",
                        "Récupérer son essence corrompue"
                    },
                    rewards = new List<string> { "Artefacts de pouvoir", "Accès aux missions finales" },
                    nextMissionId = "mission_05_finale"
                }
            };
        }

        // ... (garde toutes tes autres méthodes existantes) ...

        public ExpeditionMission GetCurrentMission()
        {
            if (currentMissionIndex < missions.Count)
                return missions[currentMissionIndex];
            return null;
        }

        public bool IsObjectiveCompleted(string objectiveId)
        {
            return completedObjectives.ContainsKey(objectiveId) && completedObjectives[objectiveId];
        }

        public void CheckAllObjectives()
        {
            var currentMission = GetCurrentMission();
            if (currentMission == null) return;

            foreach (string objective in currentMission.objectives)
            {
                if (!IsObjectiveCompleted(objective))
                {
                    CheckSpecificObjective(objective);
                }
            }
        }

        private void CheckSpecificObjective(string objective)
        {
            try
            {
                switch (objective.ToLower())
                {
                    case "construire un quartier général":
                        if (HasBuilding("Expedition33_Headquarters"))
                            CompleteObjective(objective);
                        break;

                    case "recruter 1 gestrals":
                        if (CountColonistsOfRace("Expedition33_Gestral") >= 1)
                            CompleteObjective(objective);
                        break;

                    case "rechercher 'détection du gommage'":
                        try
                        {
                            var researchDef = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_GommageDetection");
                            if (researchDef != null && researchDef.IsFinished)
                                CompleteObjective(objective);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Erreur lors de la vérification de recherche: {ex.Message}");
                        }
                        break;

                    case "fabriquer 3 dispositifs de purification":
                        if (CountItemsOfType("Expedition33_PurificationDevice") >= 3)
                            CompleteObjective(objective);
                        break;

                    case "récupérer son essence corrompue":
                        if (CountItemsOfType("Expedition33_CorruptedEssence") >= 1)
                            CompleteObjective(objective);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Erreur dans CheckSpecificObjective pour '{objective}': {ex.Message}");
            }
        }

        private bool HasBuilding(string buildingDefName)
        {
            return Find.Maps.Any(map =>
                map.listerBuildings.allBuildingsColonist.Any(b =>
                    b.def.defName == buildingDefName));
        }

        private int CountColonistsOfRace(string raceDefName)
        {
            return Find.Maps.SelectMany(map => map.mapPawns.FreeColonistsSpawned)
                .Count(p => p.def.defName == raceDefName);
        }

        private int CountItemsOfType(string itemDefName)
        {
            return Find.Maps.SelectMany(map => map.listerThings.AllThings)
                .Where(t => t.def.defName == itemDefName)
                .Sum(t => t.stackCount);
        }

        public bool CompleteObjective(string objectiveId)
        {
            if (completedObjectives.ContainsKey(objectiveId) && completedObjectives[objectiveId])
                return false;

            completedObjectives[objectiveId] = true;

            var currentMission = GetCurrentMission();
            if (currentMission != null && currentMission.IsCompleted(completedObjectives))
            {
                AdvanceToNextMission();
                return true;
            }
            return false;
        }

        private void AdvanceToNextMission()
        {
            var completedMission = missions[currentMissionIndex];
            currentMissionIndex++;

            TriggerMissionRewards(completedMission);
            SendProgressionLetter(completedMission);
        }

        private void TriggerMissionRewards(ExpeditionMission mission)
        {
            switch (mission.id)
            {
                case "mission_01_awakening":
                    UnlockResearch("Expedition33_GommageDetection");
                    break;
                case "mission_02_reconnaissance":
                    UnlockResearch("Expedition33_AntiGommageWeapons");
                    break;
                case "mission_03_purification":
                    UnlockResearch("Expedition33_AdvancedPurification");
                    break;
                case "mission_04_boss_mineur":
                    GrantSpecialRewards();
                    break;
            }
        }

        private void UnlockResearch(string researchDefName)
        {
            var researchDef = ResearchProjectDef.Named(researchDefName);
            if (researchDef != null && !researchDef.IsFinished)
            {
                Find.ResearchManager.FinishProject(researchDef);
            }
        }

        private void GrantSpecialRewards()
        {
            Map playerMap = Find.AnyPlayerHomeMap;
            if (playerMap != null)
            {
                DropCellFinder.FindSafeLandingSpot(out IntVec3 dropSpot, null, playerMap);

                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = 1000;
                GenPlace.TryPlaceThing(silver, dropSpot, playerMap, ThingPlaceMode.Near);
            }
        }

        private void SendProgressionLetter(ExpeditionMission completedMission)
        {
            var nextMission = GetCurrentMission();
            string letterText = $"**Mission '{completedMission.title}' terminée avec succès!**\n\n";

            if (nextMission != null)
            {
                letterText += $"**Nouvelle mission disponible:** '{nextMission.title}'\n\n{nextMission.description}\n\n";
                letterText += "**Objectifs:**\n";
                foreach (string objective in nextMission.objectives)
                {
                    letterText += $"• {objective}\n";
                }
            }
            else
            {
                letterText += "**Félicitations!** Vous avez terminé toutes les missions de l'Expédition 33!\n\n";
                letterText += "La menace du Gommage a été repoussée grâce à vos efforts héroïques.";
            }

            Find.LetterStack.ReceiveLetter(
                "Progression Expédition 33",
                letterText,
                LetterDefOf.PositiveEvent
            );
        }

        public List<ExpeditionMission> GetAllMissions()
        {
            return missions ?? new List<ExpeditionMission>();
        }

        public List<ExpeditionMission> GetCompletedMissions()
        {
            return missions?.Take(currentMissionIndex).ToList() ?? new List<ExpeditionMission>();
        }

        public float GetProgressPercentage()
        {
            if (missions == null || missions.Count == 0) return 0f;
            return (float)currentMissionIndex / missions.Count * 100f;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref missions, "missions", LookMode.Deep);
            Scribe_Values.Look(ref currentMissionIndex, "currentMissionIndex", 0);
            Scribe_Collections.Look(ref completedObjectives, "completedObjectives", LookMode.Value, LookMode.Value);
            // ✅ AJOUT: Sauvegarde des capacités débloquées
            Scribe_Collections.Look(ref unlockedAbilities, "unlockedAbilities");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (missions == null)
                    InitializeMissions();
                if (completedObjectives == null)
                    completedObjectives = new Dictionary<string, bool>();
                if (unlockedAbilities == null)
                    unlockedAbilities = new HashSet<string>();

                // ✅ RÉASSIGNE L'INSTANCE APRÈS CHARGEMENT
                _instance = this;
            }
        }
    }

    // ✅ CLASSE MANQUANTE: ExpeditionMission
    public class ExpeditionMission : IExposable
    {
        public string id;
        public string title;
        public string description;
        public List<string> objectives;
        public List<string> rewards;
        public string nextMissionId;

        public ExpeditionMission()
        {
            objectives = new List<string>();
            rewards = new List<string>();
        }

        // ✅ AMÉLIORATION dans ExpeditionMission
        public bool IsCompleted(Dictionary<string, bool> completedObjectives)
        {
            if (objectives?.Count == 0) return false;
            
            return objectives.All(obj => 
                !string.IsNullOrEmpty(obj) && 
                completedObjectives.ContainsKey(obj) && 
                completedObjectives[obj]);
        }
        public int GetCompletedObjectivesCount(Dictionary<string, bool> completedObjectives)
        {
            if (objectives == null) return 0;
            return objectives.Count(obj => completedObjectives.ContainsKey(obj) && completedObjectives[obj]);
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref description, "description");
            Scribe_Collections.Look(ref objectives, "objectives", LookMode.Value);
            Scribe_Collections.Look(ref rewards, "rewards", LookMode.Value);
            Scribe_Values.Look(ref nextMissionId, "nextMissionId");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (objectives == null)
                    objectives = new List<string>();
                if (rewards == null)
                    rewards = new List<string>();
            }
        }
    }


    // ✅ CLASSE MANQUANTE: GameComponent_ExpeditionProgress
    public class GameComponent_ExpeditionProgress : GameComponent
    {
        private int tickCounter = 0;
        private const int CHECK_INTERVAL = 250; // Vérifier toutes les ~4 secondes

        public GameComponent_ExpeditionProgress(Game game) : base() { }

        public override void GameComponentTick()
        {
            tickCounter++;
            if (tickCounter >= CHECK_INTERVAL)
            {
                tickCounter = 0;
                CheckExpeditionProgress();
            }
        }

        private void CheckExpeditionProgress()
        {
            // Vérification automatique des objectifs
            try
            {
                ExpeditionProgressionManager.Instance?.CheckAllObjectives();
            }
            catch (Exception ex)
            {
                Log.Warning($"Error in CheckExpeditionProgress: {ex.Message}");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tickCounter, "tickCounter", 0);
        }
    }
    // ... (garde tes autres classes ExpeditionMission et GameComponent_ExpeditionProgress) ...
}
