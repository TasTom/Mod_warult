using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Mod_warult
{
    public class Dialog_ExpeditionResearch : Window
    {
        public Dialog_ExpeditionResearch()
        {
            this.doCloseX = true;
            this.doCloseButton = true;
            this.closeOnClickedOutside = false;
            this.absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 400f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Expedition33_AntiGommageTech".Translate());
            
            Text.Font = GameFont.Small;
            float curY = 45f;
            
            var availableResearch = GetExpeditionResearch();
            foreach (var research in availableResearch)
            {
                DrawResearchProject(research, new Rect(0f, curY, inRect.width, 60f));
                curY += 70f;
            }
        }

        private List<ResearchProjectDef> GetExpeditionResearch()
        {
            return DefDatabase<ResearchProjectDef>.AllDefs
                .Where(r => r.defName.StartsWith("Expedition33_"))
                .ToList();
        }

        private void DrawResearchProject(ResearchProjectDef research, Rect rect)
        {
            bool isFinished = research.IsFinished;
            bool canStart = research.CanStartNow;
            
            Color bgColor = isFinished ? Color.green : (canStart ? Color.yellow : Color.gray);
            bgColor.a = 0.3f;
            
            Widgets.DrawBoxSolid(rect, bgColor);
            Widgets.DrawBox(rect);
            
            Widgets.Label(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 25f), research.LabelCap);
            Widgets.Label(new Rect(rect.x + 5f, rect.y + 25f, rect.width - 10f, 30f), research.description);
            
            // Afficher le statut
            string status = isFinished ? "Expedition33_ResearchCompleted".Translate() : 
                           canStart ? "Expedition33_ResearchAvailable".Translate() : 
                           "Expedition33_ResearchLocked".Translate();
            
            Widgets.Label(new Rect(rect.x + 5f, rect.y + 45f, rect.width - 10f, 15f), status);
        }
    }
}
