using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using LudeonTK;

namespace Mod_warult
{
    // ✅ SYSTÈME HEDIFF POUR LA PROGRESSION
    public class Hediff_ExpeditionProgression : Hediff
    {
        // Progression de base
        public int currentLevel = 1;
        public float combatXP = 0f;
        public float soulXP = 0f;

        // Attributs principaux
        public int vitality = 10;
        public int dexterity = 10;
        public int chance = 10;

        // Points d'attributs à dépenser
        public int availablePoints = 1;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentLevel, "currentLevel", 1);
            Scribe_Values.Look(ref combatXP, "combatXP", 0f);
            Scribe_Values.Look(ref soulXP, "soulXP", 0f);
            Scribe_Values.Look(ref vitality, "vitality", 10);
            Scribe_Values.Look(ref dexterity, "dexterity", 10);
            Scribe_Values.Look(ref chance, "chance", 10);
            Scribe_Values.Look(ref availablePoints, "availablePoints", 1);
        }

        // Calcul XP requise pour niveau suivant
        public float XPRequiredForNextLevel()
        {
            return 100f + (currentLevel * 25f) + Mathf.Pow(currentLevel, 1.5f) * 10f;
        }

        public float TotalXP()
        {
            return combatXP + (soulXP * 0.8f);
        }

        public void GainCombatXP(float amount, string source = "")
        {
            combatXP += amount;
            CheckLevelUp();
            // ShareXPWithTeam(amount * 0.3f, 0f);

            if (!string.IsNullOrEmpty(source))
            {
                Messages.Message($"{pawn.LabelShort}: +{amount} XP Combat ({source})", MessageTypeDefOf.PositiveEvent);
            }
        }

        public void GainSoulXP(float amount, string source = "")
        {
            soulXP += amount;
            CheckLevelUp();
            // ShareXPWithTeam(0f, amount * 0.5f);

            if (!string.IsNullOrEmpty(source))
            {
                Messages.Message($"{pawn.LabelShort}: +{amount} XP Âme ({source})", MessageTypeDefOf.PositiveEvent);
            }
        }

        private void ShareXPWithTeam(float combatXPShare, float soulXPShare)
        {
            foreach (Pawn colonist in PawnsFinder.AllMaps_FreeColonists)
            {
                if (colonist == pawn) continue;

                var otherProgression = ExpeditionProgressionHelper.GetOrCreateProgression(colonist);
                if (otherProgression != null)
                {
                    if (combatXPShare > 0) otherProgression.combatXP += combatXPShare;
                    if (soulXPShare > 0) otherProgression.soulXP += soulXPShare;
                    otherProgression.CheckLevelUp();
                }
            }
        }

        private void CheckLevelUp()
        {
            if (currentLevel >= 99) return;

            float requiredXP = XPRequiredForNextLevel();
            if (TotalXP() >= requiredXP)
            {
                LevelUp();
            }
        }

        private void LevelUp()
        {
            currentLevel++;
            availablePoints += 3;
            ApplyLevelUpBonuses();

            Messages.Message($"{pawn.LabelShort} atteint le niveau {currentLevel} ! (+3 points d'attribut)", MessageTypeDefOf.PositiveEvent);

            if (pawn?.Spawned == true)
            {
                FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 3.0f, new Color(1f, 0.8f, 0.2f));
            }
        }

        private void ApplyLevelUpBonuses()
        {
            if (currentLevel % 5 == 0)
            {
                var levelBonusDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LevelBonus");
                if (levelBonusDef != null)
                {
                    var existing = pawn.health.hediffSet.GetFirstHediffOfDef(levelBonusDef);
                    if (existing != null) pawn.health.RemoveHediff(existing);

                    var regen = HediffMaker.MakeHediff(levelBonusDef, pawn);
                    regen.Severity = currentLevel / 20f;
                    pawn.health.AddHediff(regen);
                }
            }

            switch (currentLevel)
            {
                case 10: GainSoulXP(100f, "Milestone niveau 10"); break;
                case 25: availablePoints += 5; break;
                case 50: UnlockSpecialAbility("MidGamePower"); break;
                case 75: UnlockSpecialAbility("LateGamePower"); break;
                case 99: UnlockSpecialAbility("MaxLevelTranscendence"); break;
            }
        }

        private void UnlockSpecialAbility(string abilityType)
        {
            Messages.Message($"{pawn.LabelShort} déverrouille une capacité spéciale : {abilityType}!", MessageTypeDefOf.PositiveEvent);
            Log.Message($"[Expedition33] Capacité débloquée : {abilityType}");
        }

        public bool TrySpendAttributePoint(AttributeType attribute)
        {
            if (availablePoints <= 0) return false;
            if (GetAttributeValue(attribute) >= 99) return false;

            availablePoints--;

            switch (attribute)
            {
                case AttributeType.Vitality: vitality++; break;
                case AttributeType.Dexterity: dexterity++; break;
                case AttributeType.Chance: chance++; break;
            }

            ApplyAttributeEffects(attribute);
            return true;
        }

        private int GetAttributeValue(AttributeType attribute)
        {
            switch (attribute)
            {
                case AttributeType.Vitality: return vitality;
                case AttributeType.Dexterity: return dexterity;
                case AttributeType.Chance: return chance;
                default: return 0;
            }
        }

        private void ApplyAttributeEffects(AttributeType attribute)
        {
            RefreshAttributeEffects();
        }

        private void RefreshAttributeEffects()
        {
            RemoveAttributeHediffs();
            ApplyVitalityEffects();
            ApplyDexterityEffects();
            ApplyChanceEffects();
        }

        private void ApplyVitalityEffects()
        {
            if (vitality <= 10) return;

            try
            {
                var vitalityHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_VitalityBonus");
                if (vitalityHediff != null)
                {
                    var existing = pawn.health.hediffSet.GetFirstHediffOfDef(vitalityHediff);
                    if (existing != null) pawn.health.RemoveHediff(existing);

                    var hediff = HediffMaker.MakeHediff(vitalityHediff, pawn);
                    hediff.Severity = Math.Min((vitality - 10) / 10f, 8.9f);
                    pawn.health.AddHediff(hediff);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Expedition33] Erreur appliquant bonus vitalité: {ex.Message}");
            }
        }

        private void ApplyDexterityEffects()
        {
            if (dexterity <= 10) return;

            var dexHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DexterityBonus");
            if (dexHediff != null)
            {
                var existing = pawn.health.hediffSet.GetFirstHediffOfDef(dexHediff);
                if (existing != null) pawn.health.RemoveHediff(existing);

                var hediff = HediffMaker.MakeHediff(dexHediff, pawn);
                hediff.Severity = (dexterity - 10) / 10f;
                pawn.health.AddHediff(hediff);
            }
        }

        private void ApplyChanceEffects()
        {
            if (chance <= 10) return;

            var chanceHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_ChanceBonus");
            if (chanceHediff != null)
            {
                var existing = pawn.health.hediffSet.GetFirstHediffOfDef(chanceHediff);
                if (existing != null) pawn.health.RemoveHediff(existing);

                var hediff = HediffMaker.MakeHediff(chanceHediff, pawn);
                hediff.Severity = (chance - 10) / 10f;
                pawn.health.AddHediff(hediff);
            }
        }

        private void RemoveAttributeHediffs()
        {
            string[] attributeHediffs = {
                "Expedition33_VitalityBonus",
                "Expedition33_DexterityBonus", 
                "Expedition33_ChanceBonus"
            };

            foreach (string hediffName in attributeHediffs)
            {
                var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffName);
                if (hediffDef != null)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                    if (hediff != null) pawn.health.RemoveHediff(hediff);
                }
            }
        }
    }

    // ✅ MÉTHODES UTILITAIRES
    public static class ExpeditionProgressionHelper
    {
        public static Hediff_ExpeditionProgression GetOrCreateProgression(Pawn pawn)
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_Progression");
            if (hediffDef == null) return null;

            var existing = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) as Hediff_ExpeditionProgression;

            if (existing == null)
            {
                existing = (Hediff_ExpeditionProgression)HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(existing);
            }

            return existing;
        }

        public static void GainXP(Pawn pawn, float amount, string type)
        {
            var progression = GetOrCreateProgression(pawn);
            if (progression == null) return;

            if (type == "combat") progression.GainCombatXP(amount, "");
            else progression.GainSoulXP(amount, "");
        }
    }

    // ✅ ÉNUMÉRATIONS ET CLASSES SUPPORT
    public enum AttributeType
    {
        Vitality,
        Dexterity,
        Chance
    }

    // ✅ GIZMOS UNIFIÉS
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    static class Patch_ExpeditionGizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (var g in gizmos) yield return g;

            if (!__instance.IsColonistPlayerControlled) yield break;

            var progression = ExpeditionProgressionHelper.GetOrCreateProgression(__instance);
            if (progression == null) yield break;

            // Gizmos d'amélioration (seulement si points disponibles)
            if (progression.availablePoints > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = $"Vitalité: {progression.vitality}",
                    defaultDesc = $"Améliorer vitalité (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = BaseContent.BadTex,
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Vitality))
                            Messages.Message($"Vitalité augmentée: {progression.vitality}", MessageTypeDefOf.PositiveEvent);
                        else
                            Messages.Message("Impossible d'améliorer la vitalité", MessageTypeDefOf.RejectInput);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"Dextérité: {progression.dexterity}",
                    defaultDesc = $"Améliorer dextérité (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = BaseContent.BadTex,
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Dexterity))
                            Messages.Message($"Dextérité augmentée: {progression.dexterity}", MessageTypeDefOf.PositiveEvent);
                        else
                            Messages.Message("Impossible d'améliorer la dextérité", MessageTypeDefOf.RejectInput);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"Chance: {progression.chance}",
                    defaultDesc = $"Améliorer chance (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = BaseContent.BadTex,
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Chance))
                            Messages.Message($"Chance augmentée: {progression.chance}", MessageTypeDefOf.PositiveEvent);
                        else
                            Messages.Message("Impossible d'améliorer la chance", MessageTypeDefOf.RejectInput);
                    }
                };
            }

            // Gizmo d'information (toujours visible)
            yield return new Command_Action
            {
                defaultLabel = "Info Progression",
                defaultDesc = $"Niveau: {progression.currentLevel}\n" +
                             $"Vitalité: {progression.vitality} | Dextérité: {progression.dexterity} | Chance: {progression.chance}\n" +
                             $"XP Combat: {progression.combatXP:F1} | XP Âme: {progression.soulXP:F1}\n" +
                             $"Points disponibles: {progression.availablePoints}",
                icon = BaseContent.BadTex,
                action = () => { /* Rien, juste affichage */ }
            };
        }
    }

    // ✅ PATCHES XP UNIFIÉS
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    static class Patch_KillXP
    {
        static void Postfix(Pawn __instance, DamageInfo? dinfo, Hediff exactCulprit)
        {
            var killer = dinfo.HasValue ? dinfo.Value.Instigator as Pawn : null;
            if (killer?.IsColonist != true) return;

            var progression = ExpeditionProgressionHelper.GetOrCreateProgression(killer);
            if (progression != null)
            {
                float xpAmount = IsBoss(__instance) ? 500f : 50f;
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

//     [HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
// static class Patch_ResearchXP
// {
//     static void Postfix(ResearchProjectDef proj)
//     {
//         try
//         {
//             // Créer une copie de la liste pour éviter la modification pendant l'itération
//             var colonists = PawnsFinder.AllMaps_FreeColonists.ToList();
            
//             foreach (var colonist in colonists)
//             {
//                 var progression = ExpeditionProgressionHelper.GetOrCreateProgression(colonist);
//                 if (progression != null)
//                 {
//                     progression.GainSoulXP(30f, "Recherche terminée");
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             Log.Error($"[Expedition33] Erreur dans Patch_ResearchXP: {ex.Message}");
//         }
//     }
// }


    [HarmonyPatch(typeof(Building), nameof(Building.SpawnSetup))]
    static class Patch_BuildingXP
    {
        static void Postfix(Building __instance, Map map, bool respawningAfterLoad)
        {
            if (respawningAfterLoad || map == null) return;
            if (!IsImportantBuilding(__instance.def)) return;

            var builder = map.mapPawns.FreeColonists.RandomElementWithFallback();
            if (builder != null)
            {
                var progression = ExpeditionProgressionHelper.GetOrCreateProgression(builder);
                if (progression != null)
                {
                    progression.GainSoulXP(30f, "Construction importante");
                }
            }
        }

        static bool IsImportantBuilding(ThingDef d)
        {
            if (d?.building == null) return false;

            return d.defName.ToLower().Contains("research")
                || d.designationCategory == DesignationCategoryDefOf.Production
                || (d.costList?.Any(c => c.count > 50) ?? false);
        }
    

    // ✅ COMMANDE DE TEST
        [DebugAction("Expedition33", "Donner XP Test", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void GiveTestXP()
        {
            var selected = Find.Selector.SingleSelectedThing as Pawn;
            if (selected?.IsColonist == true)
            {
                ExpeditionProgressionHelper.GainXP(selected, 200f, "combat");
                ExpeditionProgressionHelper.GainXP(selected, 200f, "soul");
                Messages.Message($"{selected.Name} reçoit 200 XP combat et âme", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Sélectionnez un colon d'abord", MessageTypeDefOf.RejectInput);
            }
        }
    }


}
