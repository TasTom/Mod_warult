using RimWorld;
using UnityEngine;
using Verse;

namespace Mod_warult
{
    public class ITab_Headquarters : ITab
    {
        private Building_Headquarters SelectedHQ => (Building_Headquarters)this.SelThing;

        public ITab_Headquarters()
        {
            this.size = new Vector2(500f, 400f);
            this.labelKey = "Expedition33_HeadquartersTab";
        }

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, this.size.x, this.size.y).ContractedBy(10f);
            
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, 200f, 30f), "Quartier Général");

            Text.Font = GameFont.Small;
            float curY = rect.y + 40f;

            if (SelectedHQ?.progressionManager != null)
            {
                var currentMission = SelectedHQ.progressionManager.GetCurrentMission();
                if (currentMission != null)
                {
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), $"Mission actuelle: {currentMission.title}");
                    curY += 30f;
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 60f), currentMission.description);
                }
                else
                {
                    Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), "Toutes les missions terminées !");
                }
            }
            else
            {
                Widgets.Label(new Rect(rect.x, curY, rect.width, 25f), "Système de progression non initialisé");
            }
        }
    }
}
