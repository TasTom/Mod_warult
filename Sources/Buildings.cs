using RimWorld;
using Verse;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


namespace Mod_warult
{
    // Détecteur d'âge maudit
    public class Building_CursedAgeDetector : Building
    {
        private int lastDetectedAge = -1;
        private int detectionCooldown = 0;
        private bool isActive = false;

        protected override void Tick()
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
                
                Find.LetterStack.ReceiveLetter(
                    "Expedition33_CursedAgeDetectedTitle".Translate(),
                    "Expedition33_CursedAgeDetectedDesc".Translate(currentAge),
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
                baseString += "\n" + "Expedition33_LastDetectedAge".Translate(lastDetectedAge);
            }

            if (detectionCooldown > 0)
            {
                int hours = detectionCooldown / 2500;
                baseString += "\n" + "Expedition33_NextDetectionIn".Translate(hours);
            }

            return baseString;
        }
    }
}
