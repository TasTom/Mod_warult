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
        private ExpeditionProgressionManager progressionManager;
        private Vector2 scrollPosition = Vector2.zero;

        public Dialog_ExpeditionMissions(ExpeditionProgressionManager manager)
        {
            this.progressionManager = manager;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(600f, 500f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Missions Expédition 33");

            Text.Font = GameFont.Small;
            Rect scrollRect = new Rect(0f, 45f, inRect.width, inRect.height - 100f);
            Rect viewRect = new Rect(0f, 0f, scrollRect.width - 16f, GetContentHeight());

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float curY = 0f;
            var currentMission = progressionManager.GetCurrentMission();

            if (currentMission != null)
            {
                DrawMission(currentMission, new Rect(0f, curY, viewRect.width, 200f), true);
                curY += 210f;
            }

            // Dessiner les missions précédentes (optionnel)
            DrawCompletedMissions(ref curY, viewRect.width);

            Widgets.EndScrollView();

            // Boutons d'action
            DrawActionButtons(new Rect(0f, inRect.height - 50f, inRect.width, 40f));
        }

        private void DrawMission(ExpeditionMission mission, Rect rect, bool isCurrent)
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
                bool completed = progressionManager.IsObjectiveCompleted(objective);
                Color textColor = completed ? Color.green : Color.white;

                GUI.color = textColor;
                string prefix = completed ? "✓ " : "• ";
                Widgets.Label(new Rect(rect.x + 20f, objY, rect.width - 40f, 20f), prefix + objective);
                objY += 22f;
                GUI.color = originalColor;
            }
        }

        private void DrawCompletedMissions(ref float curY, float width)
        {
            // Implémenter l'affichage des missions terminées si nécessaire
        }

        private void DrawActionButtons(Rect rect)
        {
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 150f, rect.height), "Actualiser"))
            {
                // Vérifier les objectifs manuellement
                progressionManager.CheckAllObjectives();
            }
        }

        private float GetContentHeight()
        {
            return 400f; // Ajuster selon le contenu
        }
    }
    
    
}
