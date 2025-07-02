using RimWorld;
using Verse;
using System;
using UnityEngine;

namespace Mod_warult
{
    public class HediffComp_GestralAging : HediffComp
    {
        // Propriétés pour définir les seuils d'âge
        public HediffCompProperties_GestralAging Props => (HediffCompProperties_GestralAging)this.props;

        // Intervalle de vérification (en ticks)
        private int ticksUntilNextCheck = 10000; // 1 jour de jeu

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // Vérifier périodiquement
            ticksUntilNextCheck--;
            if (ticksUntilNextCheck <= 0)
            {
                UpdateSeverityBasedOnAge();
                ticksUntilNextCheck = 60000; // Reset pour le prochain jour
            }
        }

        private void UpdateSeverityBasedOnAge()
        {
            if (parent?.pawn == null) return;

            // Obtenir l'âge du pawn en années
            float ageInYears = parent.pawn.ageTracker.AgeBiologicalYearsFloat;

            // Calculer la nouvelle sévérité basée sur l'âge
            float newSeverity = CalculateSeverityFromAge(ageInYears);

            // Mettre à jour la sévérité seulement si elle a changé significativement
            if (Math.Abs(parent.Severity - newSeverity) > 0.05f)
            {
                parent.Severity = newSeverity;
            }
        }

       private float CalculateSeverityFromAge(float ageInYears)
        {
            // Définir les seuils d'âge pour vos Gestrals
            // Ajustez ces valeurs selon vos besoins
            if (ageInYears < Props.adultAge)
            {
                // Jeune: severity 0-1
                return Mathf.Lerp(0.1f, 0.99f, ageInYears / Props.adultAge);
            }
            else if (ageInYears < Props.elderAge)
            {
                // Adulte: severity 1-2
                float adultProgress = (ageInYears - Props.adultAge) / (Props.elderAge - Props.adultAge);
                return Mathf.Lerp(1.0f, 1.99f, adultProgress);
            }
            else if (ageInYears < Props.ancientAge)
            {
                // Ancien: severity 2-3
                float elderProgress = (ageInYears - Props.elderAge) / (Props.ancientAge - Props.elderAge);
                return Mathf.Lerp(2.0f, 2.99f, elderProgress);
            }
            else
            {
                // Vénérable: severity 3+
                float ancientProgress = (ageInYears - Props.ancientAge) / 50f; // progression lente après
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
        public float adultAge = 18f;    // Âge adulte
        public float elderAge = 50f;    // Âge ancien  
        public float ancientAge = 100f; // Âge vénérable

        public HediffCompProperties_GestralAging()
        {
            this.compClass = typeof(HediffComp_GestralAging);
        }
    }
}
