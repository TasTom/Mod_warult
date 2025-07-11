using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic; // AJOUTEZ CETTE LIGNE
using UnityEngine;

namespace Mod_warult
{
    // D�tecteur d'�ge maudit
    public class Building_CursedAgeDetector : Building
    {
        private int lastDetectedAge = -1;
        private int detectionCooldown = 0;
        private bool isActive = false;

        protected override void Tick()      // ← protected, pas public
        {
            base.Tick();



            if (!isActive || detectionCooldown > 0)
            {
                detectionCooldown--;
                return;
            }

            if (GenTicks.TicksGame % 2500 == 0)
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

                string message = $"D�TECTEUR D'�GE MAUDIT ACTIV� !\n\n" +
                               $"�ge actuellement peint sur le monolithe : {currentAge} ans\n\n" +
                               $"Tous les colons de cet �ge sont en danger imm�diat !";

                Find.LetterStack.ReceiveLetter(
                    "�ge Maudit D�tect�",
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
                baseString += $"\n�ge maudit d�tect� : {lastDetectedAge} ans";
            }

            if (detectionCooldown > 0)
            {
                int hours = detectionCooldown / 2500;
                baseString += $"\nProchaine d�tection dans : {hours}h";
            }

            return baseString;
        }
    }

    // G�n�rateur de champ anti-Gommage
    public class Building_AntiGommageField : Building
    {
        private int fieldRadius = 15;
        private bool fieldActive = false;
        private List<Pawn> protectedColonists = new List<Pawn>();

        protected override void Tick()
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
            baseString += $"\nColons prot�g�s : {protectedColonists.Count}";
            baseString += $"\nChamp actif : {(fieldActive ? "Oui" : "Non")}";
            return baseString;
        }
    }
}
