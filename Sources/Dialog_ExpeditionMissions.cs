using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;

namespace Mod_warult
{
    public class Dialog_ExpeditionMissions : Window
    {
        private Vector2 scroll = Vector2.zero;

        public Dialog_ExpeditionMissions()
        {
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(850f, 520f);

        public override void DoWindowContents(Rect inRect)
        {
            float y = 0f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width, 35f), "Expedition33_MissionsTitle".Translate());
            y += 40f;

            Text.Font = GameFont.Small;
            var current = QuestManager.GetCurrentQuest();
            var completed = QuestManager.GetCompletedQuestDatas().ToList();

            float scrollHeight = 200f + completed.Count * 135f;
            Rect scrollRect = new Rect(0, y, inRect.width, inRect.height - 85f);
            Rect inner = new Rect(0, 0, scrollRect.width - 16, scrollHeight);

            Widgets.BeginScrollView(scrollRect, ref scroll, inner);

            float curY = 0f;

            if (current != null)
            {
                DrawMissionBlock(current, inner.width, ref curY, true);
                curY += 15f;
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(new Rect(0, curY, inner.width, 40f), "Expedition33_AllMissionsCompleted".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                curY += 50f;
            }

            if (completed.Count > 0)
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(0, curY, inner.width, 30f), "Expedition33_CompletedMissions".Translate());
                curY += 30f;

                foreach (var quest in completed)
                {
                    DrawMissionBlock(quest, inner.width, ref curY, false);
                    curY += 10f;
                }
            }

            Widgets.EndScrollView();

            // BOUTONS
            if (Prefs.DevMode)
            {
                if (current != null && Widgets.ButtonText(new Rect(10f, inRect.height - 90f, 170f, 32f), "ProgressDebug".Translate()))
                {
                    QuestManager.TriggerQuestEvent(current.triggerCondition);
                    Close();
                    Find.WindowStack.Add(new Dialog_ExpeditionMissions());
                }
            }

            if (Widgets.ButtonText(new Rect(200f, inRect.height - 90f, 140f, 32f), "Refresh".Translate()))
                {
                    Close();
                    Find.WindowStack.Add(new Dialog_ExpeditionMissions());
                }

            if (Widgets.ButtonText(new Rect(350f, inRect.height - 90f, 140f, 32f), "Close".Translate()))
            {
                Close();
            }

            if (current != null && current.questId == "Prologue_Start")
            {
                if (Widgets.ButtonText(new Rect(500f, inRect.height - 90f, 250f, 32f), "BeginExpedition".Translate()))
                {
                    QuestManager.TriggerQuestEvent("EVENT_DEPARTURE");
                    Messages.Message("ExpeditionStarted".Translate(), MessageTypeDefOf.PositiveEvent);
                    Close();
                }
            }
        }

        private void DrawMissionBlock(QuestData quest, float width, ref float y, bool isCurrent)
        {
            Color borderColor = isCurrent ? new Color(0.1f, 0.25f, 0.8f) : new Color(0.18f, 0.18f, 0.18f);
            Color contentColor = isCurrent ? new Color(0.88f, 0.95f, 1f) : new Color(0.95f, 0.95f, 0.95f);

            GUI.color = contentColor;
            Widgets.DrawBoxSolidWithOutline(new Rect(0, y, width, 140f), Color.clear, Color.white);
            GUI.color = borderColor;
            Widgets.DrawBox(new Rect(0, y, width, 140f), 2);
            GUI.color = Color.white;

            float blockY = y + 5f;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(10, blockY, width - 20, 25f), quest.title);
            blockY += 25f;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(15, blockY, width - 30, 38f), quest.description);
            blockY += 38f;

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(18, blockY, width - 36, 16f), "Expedition33_Objectives".Translate());
            blockY += 16f;

            foreach (var o in quest.objectives)
            {
                Widgets.Label(new Rect(30, blockY, width - 40, 18f), (isCurrent ? "• " : "✓ ") + o);
                blockY += 18f;
            }

            y += 120f;
            Text.Font = GameFont.Small;
        }
    }
}
