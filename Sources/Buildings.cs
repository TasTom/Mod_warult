using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic; // AJOUTEZ CETTE LIGNE
using UnityEngine;

namespace Mod_warult
{
    // Détecteur d'âge maudit
    public class Building_CursedAgeDetector : Building
    {
        private int lastDetectedAge = -1;
        private int detectionCooldown = 0;
        private bool isActive = false;

        public override void Tick()
        {
            base.Tick();

            if (!isActive || detectionCooldown > 0)
            {
                detectionCooldown--;
                return;
            }

            if (Find.TickManager.TicksGame % 2500 == 0)
            {
                DetectCursedAge();
            }
        }

        private void DetectCursedAge()
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.paintressAlive) return;

            int currentAge = gameComp.currentPaintedAge;

            if (currentAge != lastDetectedAge && currentAge != -1)
            {
                lastDetectedAge = currentAge;

                string message = $"DÉTECTEUR D'ÂGE MAUDIT ACTIVÉ !\n\n" +
                               $"Âge actuellement peint sur le monolithe : {currentAge} ans\n\n" +
                               $"Tous les colons de cet âge sont en danger immédiat !";

                Find.LetterStack.ReceiveLetter(
                    "Âge Maudit Détecté",
                    message,
                    LetterDefOf.NeutralEvent,
                    this
                );

                detectionCooldown = 60000;
            }
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();

            if (lastDetectedAge != -1)
            {
                baseString += $"\nÂge maudit détecté : {lastDetectedAge} ans";
            }

            if (detectionCooldown > 0)
            {
                int hours = detectionCooldown / 2500;
                baseString += $"\nProchaine détection dans : {hours}h";
            }

            return baseString;
        }
    }

    // Générateur de champ anti-Gommage
    public class Building_AntiGommageField : Building
    {
        private int fieldRadius = 15;
        private bool fieldActive = false;
        private List<Pawn> protectedColonists = new List<Pawn>();

        public override void Tick()
        {
            base.Tick();

            if (this.TryGetComp<CompPowerTrader>()?.PowerOn == true)
            {
                fieldActive = true;
                MaintainProtectionField();
            }
            else
            {
                fieldActive = false;
                RemoveProtectionFromAll();
            }
        }

        private void MaintainProtectionField()
        {
            var colonistsInRange = Map.mapPawns.FreeColonists
                .Where(p => p.Position.DistanceTo(Position) <= fieldRadius)
                .ToList();

            foreach (var colonist in colonistsInRange)
            {
                if (!protectedColonists.Contains(colonist))
                {
                    AddProtectionTo(colonist);
                    protectedColonists.Add(colonist);
                }
            }

            var colonistsToRemove = protectedColonists
                .Where(p => p.Position.DistanceTo(Position) > fieldRadius)
                .ToList();

            foreach (var colonist in colonistsToRemove)
            {
                RemoveProtectionFrom(colonist);
                protectedColonists.Remove(colonist);
            }
        }

        private void AddProtectionTo(Pawn pawn)
        {
            Hediff fieldProtection = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FieldProtection"),
                pawn
            );

            if (fieldProtection != null && !pawn.health.hediffSet.HasHediff(fieldProtection.def))
            {
                pawn.health.AddHediff(fieldProtection);
            }
        }

        private void RemoveProtectionFrom(Pawn pawn)
        {
            var protection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FieldProtection")
            );

            if (protection != null)
            {
                pawn.health.RemoveHediff(protection);
            }
        }

        private void RemoveProtectionFromAll()
        {
            foreach (var colonist in protectedColonists.ToList())
            {
                RemoveProtectionFrom(colonist);
            }
            protectedColonists.Clear();
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            baseString += $"\nRayon de protection : {fieldRadius} cases";
            baseString += $"\nColons protégés : {protectedColonists.Count}";
            baseString += $"\nChamp actif : {(fieldActive ? "Oui" : "Non")}";
            return baseString;
        }
    }
}
