using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace Mod_warult
{
    public class Dialog_ExpeditionMissions : Window
    {
        private Hediff_QuestTracker questTracker;
        private Vector2 scrollPosition = Vector2.zero;

        public Dialog_ExpeditionMissions(Pawn pawnWithTracker)
        {
            this.questTracker = pawnWithTracker.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamed("Expedition33_QuestTracker")) as Hediff_QuestTracker;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(700f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Missions Expédition 33");

            Text.Font = GameFont.Small;
            Rect scrollRect = new Rect(0f, 45f, inRect.width, inRect.height - 100f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, GetContentHeight());

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float curY = 0f;

            // Mission actuelle
            if (questTracker?.currentQuestId != null && QuestManager.AllQuests.ContainsKey(questTracker.currentQuestId))
            {
                var currentMissionData = QuestManager.AllQuests[questTracker.currentQuestId];
                DrawMission(currentMissionData, new Rect(0f, curY, viewRect.width, 200f), true);
                curY += 210f;
            }

            // Missions précédentes
            DrawCompletedMissions(ref curY, viewRect.width);

            Widgets.EndScrollView();

            // Boutons d'action
            DrawActionButtons(new Rect(0f, inRect.height - 50f, inRect.width, 40f));
        }

        private void DrawMission(QuestData mission, Rect rect, bool isCurrent = false)
        {
            Color originalColor = GUI.color;
            if (isCurrent)
                GUI.color = Color.cyan;

            Widgets.DrawBox(rect);
            GUI.color = originalColor;

            Rect titleRect = new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, mission.title);

            Text.Font = GameFont.Small;
            Rect descRect = new Rect(rect.x + 10f, rect.y + 35f, rect.width - 20f, 40f);
            Widgets.Label(descRect, mission.description);

            // Objectifs
            float objY = rect.y + 85f;
            Widgets.Label(new Rect(rect.x + 10f, objY, 100f, 25f), "Objectifs:");
            objY += 25f;

            foreach (string objective in mission.objectives)
            {
                bool completed = IsObjectiveCompleted(mission.questId, objective);
                Color textColor = completed ? Color.green : Color.white;

                GUI.color = textColor;
                string prefix = completed ? "✓ " : "• ";
                Widgets.Label(new Rect(rect.x + 20f, objY, rect.width - 40f, 20f), prefix + objective);
                objY += 22f;
            }

            GUI.color = originalColor;

            // Statut de la mission
            string status = isCurrent ? "EN COURS" : "TERMINÉE";
            Color statusColor = isCurrent ? Color.yellow : Color.green;
            GUI.color = statusColor;
            Widgets.Label(new Rect(rect.x + 10f, rect.y + rect.height - 25f, 100f, 20f), status);
            GUI.color = originalColor;
        }

        private bool IsObjectiveCompleted(string questId, string objective)
        {
            if (questTracker == null) return false;

            // Si la quête est terminée, tous ses objectifs sont considérés comme complétés
            if (questTracker.completedQuests.Contains(questId))
                return true;

            // Si c'est la quête actuelle, on peut ajouter une logique plus complexe ici
            // Pour l'instant, on considère qu'aucun objectif de la quête courante n'est terminé
            return false;
        }

        private void DrawCompletedMissions(ref float curY, float width)
        {
            if (questTracker?.completedQuests == null || questTracker.completedQuests.Count == 0)
                return;

            // Titre des missions terminées
            Widgets.Label(new Rect(10f, curY, width - 20f, 25f), "Missions Terminées:");
            curY += 30f;

            foreach (string completedQuestId in questTracker.completedQuests)
            {
                if (QuestManager.AllQuests.ContainsKey(completedQuestId))
                {
                    var missionData = QuestManager.AllQuests[completedQuestId];
                    DrawMission(missionData, new Rect(0f, curY, width, 150f), false);
                    curY += 160f;
                }
            }
        }

        private void DrawActionButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.x, rect.y-50f, 150f, rect.height), "Actualiser"))
            {
                // Fermer et rouvrir la fenêtre pour actualiser
                Close();
            }

            if (Widgets.ButtonText(new Rect(rect.x + 160f, rect.y-50f, 150f, rect.height), "Progression"))
            {
                if (questTracker != null)
                {
                    string progressText = $"Quête actuelle: {questTracker.currentQuestId}\n";
                    progressText += $"Quêtes terminées: {questTracker.completedQuests.Count}";

                    Messages.Message(progressText, MessageTypeDefOf.NeutralEvent);
                }
            }

                // ✅ BOUTON POUR FINIR LE PROLOGUE
            if (questTracker?.currentQuestId == "Prologue_Start")
            {
                if (Widgets.ButtonText(new Rect(rect.x + 320f, rect.y-50f, 150f, rect.height), "🚢 PARTIR !"))
                {
                    questTracker.TriggerQuestEvent("EVENT_DEPARTURE");
                    Messages.Message("🚀 L'Expédition 33 prend le départ vers l'inconnu !", MessageTypeDefOf.PositiveEvent);
                    Close();
                }
            }
        }

        

        private float GetContentHeight()
        {
            float height = 250f; // Hauteur pour la mission actuelle

            if (questTracker?.completedQuests != null)
            {
                height += 50f; // Titre des missions terminées
                height += questTracker.completedQuests.Count * 160f; // Chaque mission terminée
            }

            return Math.Max(height, 400f);
        }
        
        
    }
}
