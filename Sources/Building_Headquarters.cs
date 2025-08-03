using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace Mod_warult
{
    public class Building_Headquarters : Building
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override IEnumerable<InspectTabBase> GetInspectTabs()
        {
            if (def.inspectorTabs != null)
            {
                foreach (Type tabType in def.inspectorTabs)
                {
                    if (tabType != typeof(ITab_Bills))
                        yield return InspectTabManager.GetSharedInstance(tabType);
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            if (Faction.OfPlayer.IsPlayer)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Expedition33_ConsultMissions".Translate(),
                    defaultDesc = "Expedition33_ConsultMissionsDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Expedition33_MissionIcon", false),
                    action = OpenMissionWindow
                };

                yield return new Command_Action
                {
                    defaultLabel = "Expedition33_AntiGommageResearch".Translate(),
                    defaultDesc = "Expedition33_AntiGommageResearchDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Expedition33_ResearchIcon", false),
                    action = OpenResearchWindow
                };
            }
        }

        private void OpenMissionWindow()
        {
            try
            {
                Find.WindowStack.Add(new Dialog_ExpeditionMissions());
            }
            catch (Exception ex)
            {
                Log.Error("Expedition33_MissionWindowError".Translate(ex.Message));
            }
        }

        private void OpenResearchWindow()
        {
            Find.WindowStack.Add(new Dialog_ExpeditionResearch());
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
