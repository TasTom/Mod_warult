using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Mod_warult
{
    public class QuestData
    {
        public string questId;
        public string title;
        public string description;
        public List<string> objectives;
        public string triggerCondition;
        public string nextQuestId;
    }

    public static class QuestManager
    {
        public static string CurrentQuestId = "Prologue_Start";
        internal static bool triggerVersoJoinedNextTick = false;
        public static HashSet<string> CompletedQuestIds = new HashSet<string>();
        private static readonly HashSet<string> _recentTriggers = new();
        private static int _lastClearTick = 0;

        public static readonly Dictionary<string, QuestData> AllQuests = new Dictionary<string, QuestData>
        {
            // ═══════════════════════════════════════════════════════════
            // PROLOGUE : LE DÉPART DE LUMIÈRE
            // ═══════════════════════════════════════════════════════════
            ["Prologue_Start"] = new QuestData
            {
                questId = "Prologue_Start",
                title = "Expedition33_Prologue_Title".Translate(),
                description = "Expedition33_Prologue_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_Prologue_Obj1".Translate(),
                    "Expedition33_Prologue_Obj2".Translate()
                },
                triggerCondition = "EVENT_DEPARTURE",
                nextQuestId = "ActeI_VallonsFleuris"
            },

            // ═══════════════════════════════════════════════════════════
            // ACTE I : L'EXPÉDITION DE GUSTAVE
            // ═══════════════════════════════════════════════════════════
            ["ActeI_VallonsFleuris"] = new QuestData
            {
                questId = "ActeI_VallonsFleuris",
                title = "Expedition33_VallonsFleuris_Title".Translate(),
                description = "Expedition33_VallonsFleuris_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_VallonsFleuris_Obj1".Translate(),
                    "Expedition33_VallonsFleuris_Obj2".Translate(),
                    "Expedition33_VallonsFleuris_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Eveque",
                nextQuestId = "ActeI_OceanSuspendu"
            },

            ["ActeI_OceanSuspendu"] = new QuestData
            {
                questId = "ActeI_OceanSuspendu",
                title = "Expedition33_OceanSuspendu_Title".Translate(),
                description = "Expedition33_OceanSuspendu_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_OceanSuspendu_Obj1".Translate(),
                    "Expedition33_OceanSuspendu_Obj2".Translate(),
                    "Expedition33_OceanSuspendu_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Goblu",
                nextQuestId = "ActeI_SanctuaireAncien"
            },

            ["ActeI_SanctuaireAncien"] = new QuestData
            {
                questId = "ActeI_SanctuaireAncien",
                title = "Expedition33_SanctuaireAncien_Title".Translate(),
                description = "Expedition33_SanctuaireAncien_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_SanctuaireAncien_Obj1".Translate(),
                    "Expedition33_SanctuaireAncien_Obj2".Translate(),
                    "Expedition33_SanctuaireAncien_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_SakapatateUltime",
                nextQuestId = "ActeI_NidEsquie"
            },

            ["ActeI_NidEsquie"] = new QuestData
            {
                questId = "ActeI_NidEsquie",
                title = "Expedition33_NidEsquie_Title".Translate(),
                description = "Expedition33_NidEsquie_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_NidEsquie_Obj1".Translate(),
                    "Expedition33_NidEsquie_Obj2".Translate(),
                    "Expedition33_NidEsquie_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Francois",
                nextQuestId = "ActeI_Final"
            },

            ["ActeI_Final"] = new QuestData
            {
                questId = "ActeI_Final",
                title = "Expedition33_ActeI_Final_Title".Translate(),
                description = "Expedition33_ActeI_Final_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_ActeI_Final_Obj1".Translate(),
                    "Expedition33_ActeI_Final_Obj2".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_MaitreDesLampes",
                nextQuestId = "ActeII_TerresOubliees"
            },

            // ═══════════════════════════════════════════════════════════
            // ACTE II : VERSO ET LA ROUTE VERS LE MONOLITHE
            // ═══════════════════════════════════════════════════════════
            ["ActeII_VersoArrival"] = new QuestData
            {
                questId = "ActeII_VersoArrival",
                title = "Expedition_VersoArrival_Title".Translate(),
                description = "Expedition_VersoArrival_Desc".Translate(),
                objectives = new List<string> { "Expedition_VersoArrival_Obj1".Translate() },
                triggerCondition = "EVENT_VERSO_JOINED",
                nextQuestId = "ActeII_TerresOubliees"
            },

            ["ActeII_TerresOubliees"] = new QuestData
            {
                questId = "ActeII_TerresOubliees",
                title = "Expedition33_TerresOubliees_Title".Translate(),
                description = "Expedition33_TerresOubliees_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_TerresOubliees_Obj1".Translate(),
                    "Expedition33_TerresOubliees_Obj2".Translate(),
                    "Expedition33_TerresOubliees_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Dualiste",
                nextQuestId = "ActeII_Manoir"
            },

            ["ActeII_Manoir"] = new QuestData
            {
                questId = "ActeII_Manoir",
                title = "Expedition33_Manoir_Title".Translate(),
                description = "Expedition33_Manoir_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_Manoir_Obj1".Translate(),
                    "Expedition33_Manoir_Obj2".Translate(),
                    "Expedition33_Manoir_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Renoir",
                nextQuestId = "ActeII_SireneQuest"
            },

            // ✅ NOUVELLES QUÊTES SÉPARÉES POUR LES AXONS
            ["ActeII_SireneQuest"] = new QuestData
            {
                questId = "ActeII_SireneQuest",
                title = "Expedition33_SireneQuest_Title".Translate(),
                description = "Expedition33_SireneQuest_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_SireneQuest_Obj1".Translate(),
                    "Expedition33_SireneQuest_Obj2".Translate(),
                    "Expedition33_SireneQuest_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Sirene",
                nextQuestId = "ActeII_VisagesQuest"
            },

            ["ActeII_VisagesQuest"] = new QuestData
            {
                questId = "ActeII_VisagesQuest",
                title = "Expedition33_VisagesQuest_Title".Translate(),
                description = "Expedition33_VisagesQuest_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_VisagesQuest_Obj1".Translate(),
                    "Expedition33_VisagesQuest_Obj2".Translate(),
                    "Expedition33_VisagesQuest_Obj3".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Visages",
                nextQuestId = "ActeII_Final"
            },

            // ═══════════════════════════════════════════════════════════
            // CONFRONTATION FINALE
            // ═══════════════════════════════════════════════════════════
            ["ActeII_Final"] = new QuestData
            {
                questId = "ActeII_Final",
                title = "Expedition33_ActeII_Final_Title".Translate(),
                description = "Expedition33_ActeII_Final_Desc".Translate(),
                objectives = new List<string> {
                    "Expedition33_ActeII_Final_Obj1".Translate(),
                    "Expedition33_ActeII_Final_Obj2".Translate(),
                    "Expedition33_ActeII_Final_Obj3".Translate(),
                    "Expedition33_ActeII_Final_Obj4".Translate()
                },
                triggerCondition = "BOSS_DEFEATED_Expedition33_Paintress",
                nextQuestId = null // Fin de la progression narrative
            }
        };

        public static QuestData GetCurrentQuest() =>
            CurrentQuestId != null && AllQuests.ContainsKey(CurrentQuestId) ? AllQuests[CurrentQuestId] : null;

        public static IEnumerable<QuestData> GetCompletedQuestDatas() =>
            CompletedQuestIds.Select(id => AllQuests.TryGetValue(id, out var q) ? q : null).Where(q => q != null);

        public static void TriggerQuestEvent(string trigger)
        {
            if (GenTicks.TicksGame > _lastClearTick + 60) // 1 seconde
            {
                _recentTriggers.Clear();
                _lastClearTick = GenTicks.TicksGame;
            }

            if (_recentTriggers.Contains(trigger))
                return;
            _recentTriggers.Add(trigger);

            if (string.IsNullOrEmpty(trigger))
            {
                Log.Error("Expedition33_TriggerNullError".Translate());
                return;
            }

            var current = GetCurrentQuest();

            // ✅ LOGIQUE SIMPLIFIÉE : Si le trigger correspond à la condition de la quête en cours
            if (current != null && current.triggerCondition == trigger)
            {
                Log.Message($"Expedition33_QuestEventTriggered".Translate(trigger));

                FireQuestSpecialEvents(current.questId);

                CompletedQuestIds.Add(CurrentQuestId);
                CurrentQuestId = (!string.IsNullOrEmpty(current.nextQuestId) && AllQuests.ContainsKey(current.nextQuestId))
                  ? current.nextQuestId
                  : null;

                if (CurrentQuestId != null && AllQuests.TryGetValue(CurrentQuestId, out var nextQuest))
                {
                    Messages.Message($"Expedition33_QuestCompletedNext".Translate(nextQuest.title), MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("Expedition33_QuestlineCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                }

                return;
            }

            // Trigger forcé auto-évènement pour Verso
            if (CurrentQuestId == "ActeII_VersoArrival")
            {
                AdvanceToNextQuest();
                return;
            }

            // Rien trouvé
            Log.Warning($"Expedition33_NoQuestEventFound".Translate(trigger));
        }

        public static void AdvanceToNextQuest()
        {
            var currentQuest = GetCurrentQuest();
            if (currentQuest != null)
            {
                CompletedQuestIds.Add(CurrentQuestId);
                string nextId = currentQuest.nextQuestId;
                CurrentQuestId = nextId != null && AllQuests.ContainsKey(nextId) ? nextId : null;

                // Fire quest special events pour la nouvelle quête
                if (CurrentQuestId != null)
                    FireQuestSpecialEvents(CurrentQuestId);

                // Si c'est la quête spéciale Verso : on la valide immédiatement
                if (CurrentQuestId == "ActeII_VersoArrival")
                {
                    TriggerQuestEvent("EVENT_VERSO_JOINED");
                }
            }
        }

        public static void FireQuestSpecialEvents(string questId)
        {
            switch (questId)
            {
                case "Prologue_Start":
                    TriggerIncident("Expedition33_SpawnEvequeSite");
                    Messages.Message("Expedition33_PrologueStarted".Translate(), MessageTypeDefOf.NeutralEvent);
                    break;

                case "ActeI_VallonsFleuris":
                    TriggerIncident("Expedition33_SpawnGobluSite");
                    Messages.Message("Expedition33_FloweringValleysCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeI_OceanSuspendu":
                    TriggerIncident("Expedition33_SpawnSakapatateUltimeSite");
                    Messages.Message("Expedition33_SuspendedOceanCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeI_SanctuaireAncien":
                    TriggerIncident("Expedition33_SpawnFrancoisSite");
                    Messages.Message("Expedition33_AncientSanctuaryCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeI_NidEsquie":
                    TriggerIncident("Expedition33_SpawnMaitreDesLampesSite");
                    Messages.Message("Expedition33_EsquieNestCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeI_Final":
                    // Fin d'acte I - LETTRE IMPORTANTE POUR VERSO
                    NarrativeEvents.TriggerActeICompletion();

                    Find.LetterStack.ReceiveLetter(
                        "Expedition_VersoArrivalTitle".Translate(),
                        "Expedition_VersoArrivalLetter".Translate(),
                        LetterDefOf.PositiveEvent
                    );

                    NarrativeEvents.TriggerVersoArrival();
                    TriggerIncident("Expedition33_SpawnDualisteSite");
                    Messages.Message("Expedition33_ActeIFinished".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeII_VersoArrival":
                    Find.LetterStack.ReceiveLetter(
                        "Expedition_VersoJoinedTitle".Translate(),
                        "Expedition_VersoJoinedLetter".Translate(),
                        LetterDefOf.PositiveEvent
                    );

                    NarrativeEvents.TriggerVersoArrival();
                    TriggerIncident("Expedition33_SpawnDualisteSite");
                    Messages.Message("Expedition_VersoJoined".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeII_TerresOubliees":
                    TriggerIncident("Expedition33_SpawnRenoirSite");
                    Messages.Message("Expedition33_ForgottenLandsCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeII_Manoir":
                    // ✅ SPAWN SEULEMENT LE SITE DE LA SIRÈNE
                    TriggerIncident("Expedition33_SpawnSireneSite");
                    Messages.Message("Expedition33_ManorCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                // ✅ NOUVELLES QUÊTES SÉPARÉES
                case "ActeII_SireneQuest":
                    // Sirène vaincue, spawn du site des Visages
                    TriggerIncident("Expedition33_SpawnVisagesSite");
                    Messages.Message("Expedition33_SireneCompleted".Translate(), MessageTypeDefOf.PositiveEvent);
                    break;

                case "ActeII_VisagesQuest":
                    // Visages vaincus, spawn de la Paintress
                    TriggerIncident("Expedition33_SpawnPaintressSite");
                    Messages.Message("Expedition33_VisagesCompleted".Translate(), MessageTypeDefOf.ThreatBig);
                    break;

                case "ActeII_Final":
                    Messages.Message("Expedition33_FinalQuestStarted".Translate(), MessageTypeDefOf.ThreatBig);
                    break;

                default:
                    Log.Warning($"Expedition33_UnknownQuestId".Translate(questId));
                    break;
            }
        }

        private static void TriggerIncident(string incidentDefName)
        {
            var incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (incidentDef == null)
            {
                Log.Warning($"Expedition33_IncidentNotFound".Translate(incidentDefName));
                return;
            }
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                                DefDatabase<FactionDef>.GetNamedSilentFail("Nevrons"))
                            ?? Find.FactionManager.RandomEnemyFaction();
            if (incidentDef.Worker.TryExecute(parms))
                Log.Message($"Expedition33_BossSiteGenerated".Translate(incidentDefName));
        }

        public static void ExposeData()
        {
            Scribe_Values.Look(ref CurrentQuestId, "questManagerCurrentQuestId");
            Scribe_Collections.Look(ref CompletedQuestIds, "questManager_completed", LookMode.Value);
        }
    }

    public class GameComponent_QuestProgress : GameComponent
    {
        public GameComponent_QuestProgress(Game game) : base() { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref QuestManager.CurrentQuestId, "CurrentQuestId", "Prologue_Start");
            Scribe_Collections.Look(ref QuestManager.CompletedQuestIds, "CompletedQuestIds", LookMode.Value);
            QuestManager.ExposeData();
        }
    }
}
