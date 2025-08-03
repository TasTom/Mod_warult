using RimWorld;
using Verse;
using System;
using UnityEngine;

namespace Mod_warult
{
    public class HediffComp_GestralAging : HediffComp
    {
        public HediffCompProperties_GestralAging Props => (HediffCompProperties_GestralAging)this.props;
        private int ticksUntilNextCheck = 10000;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            ticksUntilNextCheck--;
            if (ticksUntilNextCheck <= 0)
            {
                UpdateSeverityBasedOnAge();
                ticksUntilNextCheck = 60000;
            }
        }

        private void UpdateSeverityBasedOnAge()
        {
            if (parent?.pawn == null) return;

            float ageInYears = parent.pawn.ageTracker.AgeBiologicalYearsFloat;
            float newSeverity = CalculateSeverityFromAge(ageInYears);

            if (Math.Abs(parent.Severity - newSeverity) > 0.05f)
            {
                parent.Severity = newSeverity;
            }
        }

        private float CalculateSeverityFromAge(float ageInYears)
        {
            if (ageInYears < Props.adultAge)
            {
                return Mathf.Lerp(0.1f, 0.99f, ageInYears / Props.adultAge);
            }
            else if (ageInYears < Props.elderAge)
            {
                float adultProgress = (ageInYears - Props.adultAge) / (Props.elderAge - Props.adultAge);
                return Mathf.Lerp(1.0f, 1.99f, adultProgress);
            }
            else if (ageInYears < Props.ancientAge)
            {
                float elderProgress = (ageInYears - Props.elderAge) / (Props.ancientAge - Props.elderAge);
                return Mathf.Lerp(2.0f, 2.99f, elderProgress);
            }
            else
            {
                float ancientProgress = (ageInYears - Props.ancientAge) / 50f;
                return Mathf.Min(4.0f, 3.0f + ancientProgress);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticksUntilNextCheck, "ticksUntilNextCheck", 60000);
        }
    }

    public class HediffCompProperties_GestralAging : HediffCompProperties
    {
        public float adultAge = 18f;
        public float elderAge = 50f;
        public float ancientAge = 100f;

        public HediffCompProperties_GestralAging()
        {
            this.compClass = typeof(HediffComp_GestralAging);
        }
    }
}
