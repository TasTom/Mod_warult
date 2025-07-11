using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System;

namespace Mod_warult
{
    public class Building_Headquarters : Building
    {
        public ExpeditionProgressionManager progressionManager;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (progressionManager == null)
            {
                progressionManager = new ExpeditionProgressionManager();
            }
        }

        // AJOUTEZ cette méthode pour forcer SEULEMENT vos onglets
        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            // Ne retournez QUE vos onglets personnalisés
            if (def.inspectorTabs != null)
            {
                foreach (Type tabType in def.inspectorTabs)
                {
                    if (tabType != typeof(ITab_Bills)) // Exclure ITab_Bills
                    {
                        yield return InspectTabManager.GetSharedInstance(tabType);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (Faction.OfPlayer.IsPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Consulter les missions",
                    defaultDesc = "Ouvrir le panneau de progression de l'Expédition 33",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Expedition33_MissionIcon", false),
                    action = () => OpenMissionWindow()
                };

                yield return new Command_Action
                {
                    defaultLabel = "Recherches anti-Gommage",
                    defaultDesc = "Accéder aux technologies secrètes contre Le Gommage",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Expedition33_ResearchIcon", false),
                    action = () => OpenResearchWindow()
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEBUG: Accessoires",
                    defaultDesc = "Afficher le nombre d'accessoires des colons",
                    action = () => {
                        var colonists = Find.Maps.SelectMany(m => m.mapPawns.FreeColonistsSpawned);
                        foreach (var colonist in colonists)
                        {
                            int count = colonist.GetAccessoryCount();
                            Log.Message($"{colonist.Name.ToStringShort}: {count}/4 accessoires");
                        }
                    }
                };
            }
        }

        // ✅ MÉTHODE CORRIGÉE - Résout l'erreur de compilation
        private void OpenMissionWindow()
        {
            try
            {
                // Trouve un pawn avec le QuestTracker
                var pawnWithTracker = FindPawnWithQuestTracker();
                
                if (pawnWithTracker != null)
                {
                    Find.WindowStack.Add(new Dialog_ExpeditionMissions(pawnWithTracker));
                }
                else
                {
                    Messages.Message("Aucun membre d'expédition avec tracker de quêtes trouvé.", MessageTypeDefOf.RejectInput);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Expedition33] Erreur lors de l'ouverture des missions: {ex.Message}");
            }
        }

        // ✅ NOUVELLE MÉTHODE - Trouve le pawn avec le QuestTracker
        private Pawn FindPawnWithQuestTracker()
        {
            // 1. Parcourt d’abord tous les colons libres vivants
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists)
            {
                var questTracker = pawn.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker"));

                if (questTracker != null)
                    return pawn;
            }

            // 2. Aucun tracker trouvé : on prend simplement le premier colon libre
            var firstColonist = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists
                                .FirstOrDefault();

            // 3. On ajoute automatiquement le tracker si nécessaire
            if (firstColonist != null)
            {
                var def = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_QuestTracker");
                if (def != null)
                {
                    var tracker = HediffMaker.MakeHediff(def, firstColonist);
                    firstColonist.health.AddHediff(tracker);

                    Messages.Message("Tracker de quêtes ajouté automatiquement.",
                                     MessageTypeDefOf.NeutralEvent);
                }
            }

            return firstColonist;   // Peut être null si aucun colon libre n’existe
        }


        private void OpenResearchWindow()
        {
            Find.WindowStack.Add(new Dialog_ExpeditionResearch());
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref progressionManager, "progressionManager");
        }
    }
}
