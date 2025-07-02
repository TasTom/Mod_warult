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
    // Syst�me d'alerte
    public class Alert_CurrentPaintedAge : Alert
    {
        public Alert_CurrentPaintedAge()
        {
            this.defaultLabel = "�ge Maudit Actuel";
            this.defaultPriority = AlertPriority.Critical;
        }

        public override AlertReport GetReport()
        {
            if (Current.Game == null) return AlertReport.Inactive;

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.paintressAlive || gameComp.currentPaintedAge == -1)
                return AlertReport.Inactive;

            // SUPPRIME les logs pour �viter le spam
            // Log.Message($"Alert_CurrentPaintedAge: Found {cursedColonists.Count} cursed colonists of age {gameComp.currentPaintedAge}");

            var maps = Find.Maps?.Where(m => m.IsPlayerHome);
            if (maps == null) return AlertReport.Inactive;

            List<Pawn> cursedColonists = new List<Pawn>();
            foreach (Map map in maps)
            {
                if (map?.mapPawns?.FreeColonists != null)
                {
                    foreach (Pawn pawn in map.mapPawns.FreeColonists)
                    {
                        if (pawn.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge) // >= au lieu de ==
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
                    ? (gameComp.nextPaintingTick - Find.TickManager.TicksGame) / 60000
                    : 999;

                return $"�GE PEINT SUR LE MONOLITHE : {gameComp.currentPaintedAge} ANS\n\n" +
                       $"Tous vos colons de cet �ge exact risquent d'�tre gomm�s � tout moment.\n\n" +
                       $"Prochaine peinture dans : {Math.Max(0, daysUntilNext)} jours\n\n" +
                       $"Seule la mort de la Paintress peut briser ce cycle maudit.";
            }

            return "Le monolithe de la Panteresse menace vos colons.";
        }

        public override string GetLabel()
        {
            var gameComp = Current.Game?.GetComponent<GameComponent_PaintressMonolith>();

            if (gameComp?.currentPaintedAge != null && gameComp.currentPaintedAge != -1)
            {
                return $"�ge Maudit : {gameComp.currentPaintedAge} ans";
            }

            return "�ge Maudit Actuel";
        }
    }

}