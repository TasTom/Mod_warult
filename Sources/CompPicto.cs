using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace Mod_warult
{
    /* -------------------------------------------------------------------- *\
     * 1.  COMPOSANT PORTÉ PAR CHAQUE PICTO
    \* -------------------------------------------------------------------- */
    public class CompPicto : ThingComp
    {
        private int combatsParticipated;
        private bool isLearned;
        private Pawn wearer;

        public CompProperties_Picto Props => (CompProperties_Picto)props;
        public bool IsLearned => isLearned;
        public int CombatsRemaining => Mathf.Max(0, Props.combatsToLearn - combatsParticipated);

        /* -------------------------- S A V E -------------------------------- */

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref combatsParticipated, "combatsParticipated");
            Scribe_Values.Look(ref isLearned, "isLearned");
        }

        /* -------------------- É Q U I P E M E N T ------------------------- */

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            wearer = pawn;

            ApplyStatBonuses();
            // Adjust the arguments below to match the RegisterPicto method signature
            PictoManager.Instance.RegisterPicto(this);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            RemoveStatBonuses();
            wearer = null;
        }

        /* --------------------- P R O G R E S S I O N ----------------------- */

    public void RegisterKill()
    {
        if (isLearned || wearer == null) return;

        combatsParticipated++;
        Log.Message($"[Picto] {wearer.LabelShortCap} progresse ({combatsParticipated}/{Props.combatsToLearn})");

        if (combatsParticipated >= Props.combatsToLearn)
            LearnPicto();
    }

        private void LearnPicto()
        {
            isLearned = true;
            PictoManager.Instance.LearnPicto(Props.pictoType, Props.passiveAbility);

            Messages.Message($"Picto « {Props.pictoType} » maîtrisé ! Bonus partagé via Luminas.",
                             wearer, MessageTypeDefOf.PositiveEvent);

            FleckMaker.ThrowDustPuffThick(wearer.Position.ToVector3(), wearer.Map, 2f,
                                          new Color(0.8f, 0.3f, 0.9f));
        }

        /* ------------------------ B O N U S -------------------------------- */

        private void ApplyStatBonuses()
        {
            // 1. Tente de supprimer l’ancien hediff, mais sans planter
            HediffDef oldDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_PictoBonus");
            if (oldDef != null)
            {
                Hediff old = wearer.health.hediffSet.GetFirstHediffOfDef(oldDef);
                if (old != null) wearer.health.RemoveHediff(old);
            }

            // 2. Ajoute le nouveau hediff spécifique
            string defName = GetHediffDefNameForPictoType();
            if (string.IsNullOrEmpty(defName)) return;

            HediffDef bonusDef = HediffDef.Named(defName);          // ce nom existe
            Hediff bonus = HediffMaker.MakeHediff(bonusDef, wearer);
            bonus.Severity = 1f;
            wearer.health.AddHediff(bonus);
        }


        private void RemoveStatBonuses()
        {
            string hediffDefName = GetHediffDefNameForPictoType();
            
            if (!string.IsNullOrEmpty(hediffDefName))
            {
                HediffDef bonusDef = HediffDef.Named(hediffDefName);
                Hediff existing = wearer.health.hediffSet.GetFirstHediffOfDef(bonusDef);
                if (existing != null)
                {
                    wearer.health.RemoveHediff(existing);
                }
            }
            
            wearer.health.capacities.Notify_CapacityLevelsDirty();
        }

        private string GetHediffDefNameForPictoType()
        {
            return Props.pictoType switch
            {
                "Vitality" => "Expedition33_PictoBonus_Vitality",
                "Defense" => "Expedition33_PictoBonus_Defense", 
                "Critical" => "Expedition33_PictoBonus_Critical",
                "Guardian" => "Expedition33_PictoBonus_Guardian",
                "Striker" => "Expedition33_PictoBonus_Striker",
                "Healer" => "Expedition33_PictoBonus_Healer",
                "Scholar" => "Expedition33_PictoBonus_Scholar",
                _ => null
            };
        }


        /* ---------------------- I N S P E C T ------------------------------ */

        public override string CompInspectStringExtra()
            => isLearned
               ? "Picto maîtrisé – bonus diffusé."
               : $"Progression : {combatsParticipated}/{Props.combatsToLearn} combats";
    }

    /* -------------------------------------------------------------------- *\
     * 2.  PROPRIÉTÉS XML DU COMPOSANT
    \* -------------------------------------------------------------------- */
    public class CompProperties_Picto : CompProperties
    {
        public int combatsToLearn = 4;
        public string pictoType = "";
        public string statBonus1 = "";
        public float bonusValue1 = 0f;
        public string statBonus2 = "";
        public float bonusValue2 = 0f;
        public string passiveAbility = "";

        public CompProperties_Picto() => compClass = typeof(CompPicto);
    }



    

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    static class Picto_KillTracker
    {
        static void Prefix(DamageInfo? dinfo, Hediff exactCulprit) // ✅ Signature complète
        {

             if (!dinfo.HasValue)
            return ;  
            Pawn killer = dinfo.HasValue ? dinfo.Value.Instigator as Pawn : null;
            if (killer == null) return;

            foreach (var apparel in killer.apparel.WornApparel)
                apparel.GetComp<CompPicto>()?.RegisterKill();
        }
    }

    [StaticConstructorOnStartup]
    public static class PictoInitPing
    {
        static PictoInitPing() => Log.Message("[Picto] Patches chargés !");
    }
                
    

}
