using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.Planet;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace Mod_warult
{
    // Système d'alerte
    public class Alert_CurrentPaintedAge : Alert
    {
        public Alert_CurrentPaintedAge()
        {
            this.defaultLabel = "Expedition33_CursedAgeAlert".Translate();
            this.defaultPriority = AlertPriority.Critical;
        }

        public override AlertReport GetReport()
        {
            if (Current.Game == null) return AlertReport.Inactive;
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.paintressAlive || gameComp.currentPaintedAge == -1)
                return AlertReport.Inactive;

            var maps = Find.Maps?.Where(m => m.IsPlayerHome);
            if (maps == null) return AlertReport.Inactive;

            List<Pawn> cursedColonists = new List<Pawn>();

            foreach (Map map in maps)
            {
                if (map?.mapPawns?.FreeColonists != null)
                {
                    foreach (Pawn pawn in map.mapPawns.FreeColonists)
                    {
                        if (pawn.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge)
                        {
                            cursedColonists.Add(pawn);
                        }
                    }
                }
            }

            if (cursedColonists.Any())
                return AlertReport.CulpritsAre(cursedColonists);

            return AlertReport.Active;
        }

        public override TaggedString GetExplanation()
        {
            var gameComp = Current.Game?.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp?.currentPaintedAge != null && gameComp.currentPaintedAge != -1)
            {
                int daysUntilNext = gameComp.nextPaintingTick > 0
                    ? (gameComp.nextPaintingTick - GenTicks.TicksGame) / 60000
                    : 999;

                return "Expedition33_CursedAgeExplanation".Translate(
                    gameComp.currentPaintedAge,
                    gameComp.currentPaintedAge
                );
            }

            return "Expedition33_MonolithThreat".Translate();
        }

        public override string GetLabel()
        {
            var gameComp = Current.Game?.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp?.currentPaintedAge != null && gameComp.currentPaintedAge != -1)
            {
                return "Expedition33_CursedAgeLabel".Translate(gameComp.currentPaintedAge);
            }

            return "Expedition33_CursedAgeAlert".Translate();
        }
    }
}
