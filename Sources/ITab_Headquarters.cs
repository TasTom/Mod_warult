using RimWorld;
using UnityEngine;
using Verse;

namespace Mod_warult
{
    public class ITab_Headquarters : ITab
    {
        public ITab_Headquarters()
        {
            this.size = new Vector2(500f, 400f);
            this.labelKey = "Expedition33_HeadquartersTab";
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30f), "Expedition33_CurrentMission".Translate());

            Text.Font = GameFont.Small;
            var q = QuestManager.GetCurrentQuest();

            if (q != null)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 40f, rect.width, 25f), q.title);
                Widgets.Label(new Rect(rect.x, rect.y + 70f, rect.width, 60f), q.description);
            }
            else
            {
                Widgets.Label(new Rect(rect.x, rect.y + 40f, rect.width, 25f), "Expedition33_AllQuestsCompleted".Translate());
            }
        }
    }
}
