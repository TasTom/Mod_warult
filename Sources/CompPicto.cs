using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mod_warult
{
    /* -------------------------------------------------------------------- *\
    * 1. COMPOSANT PORTÉ PAR CHAQUE PICTO
    \* -------------------------------------------------------------------- */
    public class CompPicto : ThingComp
    {
        private int combatsParticipated;
        private bool isLearned;
        private bool luminaUnlocked = false;
        private Pawn wearer;

        public bool IsLuminaUnlocked => luminaUnlocked;
        public CompProperties_Picto Props => (CompProperties_Picto)props;
        public bool IsLearned => isLearned;
        public int CombatsRemaining => Mathf.Max(0, Props.combatsToLearn - combatsParticipated);

        /* -------------------------- S A V E -------------------------------- */
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref combatsParticipated, "combatsParticipated");
            Scribe_Values.Look(ref isLearned, "isLearned");
            Scribe_Values.Look(ref luminaUnlocked, "luminaUnlocked", false);
        }

        /* -------------------- É Q U I P E M E N T ------------------------- */
        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            wearer = pawn;
            ApplyStatBonuses();
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
            
            if (Prefs.DevMode)
            {
                Log.Warning("Expedition33_PictoKillRegistered".Translate(wearer.LabelShort, combatsParticipated));
            }
            
            combatsParticipated++;
            
            if (Prefs.DevMode)
            {
                Log.Warning("Expedition33_PictoKillRegisteredAfter".Translate(wearer.LabelShort, combatsParticipated));
            }
            
            Log.Message("Expedition33_PictoProgress".Translate(wearer.LabelShortCap, combatsParticipated, Props.combatsToLearn));
            
            if (combatsParticipated >= Props.combatsToLearn)
                LearnPicto();
        }

        private void LearnPicto()
        {
            isLearned = true;
            luminaUnlocked = true;
            PictoManager.Instance.LearnPicto(Props.pictoType, Props.passiveAbility);
            
            // Ajouter la Lumina au porteur
            string luminaDefName = $"Expedition33_Lumina_{Props.pictoType}";
            HediffDef luminaDef = HediffDef.Named(luminaDefName);
            if (luminaDef != null)
            {
                wearer.health.AddHediff(luminaDef);
            }

            Messages.Message("Expedition33_PictoMastered".Translate(Props.pictoType),
                wearer, MessageTypeDefOf.PositiveEvent);
            FleckMaker.ThrowDustPuffThick(wearer.Position.ToVector3(), wearer.Map, 2f,
                new Color(0.8f, 0.3f, 0.9f));
        }

        /* ------------------------ B O N U S -------------------------------- */
        private void ApplyStatBonuses()
        {
            // Supprime l'ancien hediff, mais sans planter
            HediffDef oldDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_PictoBonus");
            if (oldDef != null)
            {
                Hediff old = wearer.health.hediffSet.GetFirstHediffOfDef(oldDef);
                if (old != null) wearer.health.RemoveHediff(old);
            }

            // Ajoute le nouveau hediff spécifique
            string defName = GetHediffDefNameForPictoType();
            if (string.IsNullOrEmpty(defName)) return;
            
            HediffDef bonusDef = HediffDef.Named(defName);
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
            ? "Expedition33_PictoMasteredStatus".Translate()
            : "Expedition33_PictoProgressStatus".Translate(combatsParticipated, Props.combatsToLearn);
    }

    /* -------------------------------------------------------------------- *\
    * 2. PROPRIÉTÉS XML DU COMPOSANT
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
    static class Unified_KillTracker
    {
        private static Dictionary<int, int> processedKillsThisTick = new Dictionary<int, int>();

        static void Prefix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            // Créer un ID unique pour ce kill spécifique
            int currentTick = GenTicks.TicksGame;
            int killHashCode = __instance.GetHashCode();
            
            // Vérifier si ce pawn a déjà été traité ce tick
            if (processedKillsThisTick.ContainsKey(killHashCode) &&
                processedKillsThisTick[killHashCode] == currentTick)
            {
                return;
            }

            // Marquer ce kill comme traité pour ce tick
            processedKillsThisTick[killHashCode] = currentTick;
            
            // Nettoyer le cache périodiquement
            if (currentTick % 100 == 0)
            {
                var keysToRemove = processedKillsThisTick
                    .Where(kvp => currentTick - kvp.Value > 100)
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var key in keysToRemove)
                    processedKillsThisTick.Remove(key);
            }

            if (dinfo == null) return;
            if (dinfo.Value.Instigator is not Pawn killer) return;
            if (killer.apparel == null || killer.apparel.WornApparel.NullOrEmpty()) return;
            
            foreach (var apparel in killer.apparel.WornApparel)
            {
                CompPicto comp = apparel.GetComp<CompPicto>();
                comp?.RegisterKill();
            }

            var progression = ExpeditionProgressionHelper.GetOrCreateProgression(killer);
            if (progression != null)
            {
                float xpAmount = IsBoss(__instance) ? 75f : 8f;
                progression.GainCombatXP(xpAmount, "Kill");
            }
        }

        static bool IsBoss(Pawn p)
        {
            if (p?.kindDef?.defName == null) return false;
            string defName = p.kindDef.defName;
            return defName.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0
                || defName.Contains("Alpha")
                || defName.Contains("Boss")
                || defName.Contains("Paintress")
                || defName.Contains("NevronDechut");
        }
    }

    [StaticConstructorOnStartup]
    public static class PictoInitPing
    {
        static PictoInitPing() => Log.Message("Expedition33_PictoPatchesLoaded".Translate());
    }
}
