using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using LudeonTK;
using Verse.Sound;

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
        public int power = 10;
        public int agility = 10;
        public int defense = 10;
        public int chance = 10;

        // Points d'attributs à dépenser
        public int availablePoints = 1;

        // ✅ NOUVEAU : Système de capacités
        public Dictionary<string, int> abilityCooldowns = new Dictionary<string, int>();
        public HashSet<string> unlockedAbilities = new HashSet<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentLevel, "currentLevel", 1);
            Scribe_Values.Look(ref combatXP, "combatXP", 0f);
            Scribe_Values.Look(ref soulXP, "soulXP", 0f);
            Scribe_Values.Look(ref vitality, "vitality", 10);
            Scribe_Values.Look(ref power, "power", 10);
            Scribe_Values.Look(ref agility, "agility", 10);
            Scribe_Values.Look(ref defense, "defense", 10);
            Scribe_Values.Look(ref chance, "chance", 10);
            Scribe_Values.Look(ref availablePoints, "availablePoints", 1);

            // ✅ NOUVEAU : Sauvegarde des capacités
            Scribe_Collections.Look(ref abilityCooldowns, "abilityCooldowns", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref unlockedAbilities, "unlockedAbilities", LookMode.Value);

            if (abilityCooldowns == null) abilityCooldowns = new Dictionary<string, int>();
            if (unlockedAbilities == null) unlockedAbilities = new HashSet<string>();
        }

        public float TotalXP()
        {
            return combatXP + (soulXP * 0.8f);
        }

        public float GetCurrentLevelXP()
        {
            float totalXPUsed = 0f;
            for (int level = 1; level < currentLevel; level++)
            {
                totalXPUsed += GetXPRequiredForLevel(level);
            }
            return totalXPUsed;
        }

        public float GetXPRequiredForLevel(int level)
        {
            return 100f + (level * 25f) + Mathf.Pow(level, 1.5f) * 10f;
        }

        public float XPRequiredForNextLevel()
        {
            return GetXPRequiredForLevel(currentLevel);
        }

        public float GetProgressToNextLevel()
        {
            float currentLevelXP = GetCurrentLevelXP();
            float nextLevelXP = GetXPRequiredForLevel(currentLevel);
            float totalXP = TotalXP();
            float progressXP = totalXP - currentLevelXP;
            return Mathf.Clamp01(progressXP / nextLevelXP);
        }

        public void GainCombatXP(float amount, string source = "")
        {
            combatXP += amount;
            CheckLevelUp();

            if (!string.IsNullOrEmpty(source))
            {
                Messages.Message($"{pawn.LabelShort}: +{amount} XP Combat ({source})", MessageTypeDefOf.PositiveEvent);
            }
        }

        public void GainSoulXP(float amount, string source = "")
        {
            soulXP += amount;
            CheckLevelUp();

            if (!string.IsNullOrEmpty(source))
            {
                Messages.Message($"{pawn.LabelShort}: +{amount} XP Âme ({source})", MessageTypeDefOf.PositiveEvent);
            }
        }

        private void CheckLevelUp()
        {
            if (currentLevel >= 99) return;

            float totalXP = TotalXP();
            float requiredXPForCurrentLevel = GetCurrentLevelXP();
            float xpRequiredForNextLevel = GetXPRequiredForLevel(currentLevel);

            if (totalXP >= requiredXPForCurrentLevel + xpRequiredForNextLevel)
            {
                LevelUp();
                CheckLevelUp();
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
                VisualEffects.PlayLevelUpEffect(pawn);
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
        }

        // ✅ NOUVELLES MÉTHODES D'ACTIVATION

        private void ActivateIronWill()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_IronWillBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayIronWillEffect(pawn);
        }

        private void ActivateCrushingBlow()
        {
            // Cette ability utilise le système d'explosion du XML, 
            // donc on peut créer un effet visuel ici
            VisualEffects.PlayCrushingBlowEffect(pawn);
            
            // Pour l'effet d'explosion, vous pouvez ajouter une logique personnalisée
            // ou laisser le système XML gérer l'explosion
            Messages.Message($"💥 {pawn.LabelShort} prépare un coup dévastateur !", MessageTypeDefOf.PositiveEvent);
        }

        private void ActivateTimeSlip()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_TimeSlipBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayTimeSlipEffect(pawn);
        }

        private void ActivateFortress()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FortressBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayFortressEffect(pawn);
        }

        private void ActivateMiracle()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_MiracleBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayMiracleEffect(pawn);
        }



        public bool TrySpendAttributePoint(AttributeType attribute)
        {
            if (availablePoints <= 0) return false;
            if (GetAttributeValue(attribute) >= 99) return false;

            availablePoints--;

            switch (attribute)
            {
                case AttributeType.Vitality: vitality++; break;
                case AttributeType.Power: power++; break;
                case AttributeType.Agility: agility++; break;
                case AttributeType.Defense: defense++; break;
                case AttributeType.Chance: chance++; break;
            }

            ApplyAttributeEffects(attribute);
            CheckAbilityUnlocks(); // ✅ CORRIGÉ : Vérifier les déverrouillages
            return true;
        }

        private int GetAttributeValue(AttributeType attribute)
        {
            switch (attribute)
            {
                case AttributeType.Vitality: return vitality;
                case AttributeType.Power: return power;
                case AttributeType.Agility: return agility;
                case AttributeType.Defense: return defense;
                case AttributeType.Chance: return chance;
                default: return 0;
            }
        }

        // ✅ SYSTÈME DE CAPACITÉS INTÉGRÉ
        public void CheckAbilityUnlocks()
        {
            foreach (var ability in SpecialAbilities.AllAbilities)
            {
                string abilityKey = ability.Key;
                var data = ability.Value;

                if (!unlockedAbilities.Contains(abilityKey) &&
                    GetAttributeValue(data.requiredAttribute) >= data.requiredLevel)
                {
                    unlockedAbilities.Add(abilityKey);

                    Messages.Message(
                        $"🌟 {pawn.LabelShort} déverrouille '{data.name}' !",
                        MessageTypeDefOf.PositiveEvent
                    );

                    VisualEffects.PlayAbilityUnlockEffect(pawn);
                }
            }
        }

        public bool CanUseAbility(string abilityKey)
        {
            if (!unlockedAbilities.Contains(abilityKey)) return false;

            int currentTick = GenTicks.TicksGame;
            int cooldownEnd = 0;
            abilityCooldowns.TryGetValue(abilityKey, out cooldownEnd);

            return currentTick >= cooldownEnd;
        }

        public bool TryActivateAbility(string abilityKey)
        {
            if (!CanUseAbility(abilityKey)) return false;
            
            var data = SpecialAbilities.AllAbilities[abilityKey];
            int currentTick = GenTicks.TicksGame;
            
            // ✅ SWITCH COMPLET AVEC TOUTES LES ABILITIES
            switch (abilityKey)
            {
                case "Regeneration":
                    ActivateRegeneration();
                    break;
                case "IronWill":
                    ActivateIronWill();
                    break;
                case "BerserkerRage":
                    ActivateBerserkerRage();
                    break;
                case "CrushingBlow":
                    ActivateCrushingBlow();
                    break;
                case "QuickStep":
                    ActivateQuickStep();
                    break;
                case "TimeSlip":
                    ActivateTimeSlip();
                    break;
                case "IronSkin":
                    ActivateIronSkin();
                    break;
                case "Fortress":
                    ActivateFortress();
                    break;
                case "LuckyStrike":
                    ActivateLuckyStrike();
                    break;
                case "Miracle":
                    ActivateMiracle();
                    break;
                default:
                    Log.Warning($"[Expedition33] Ability non reconnue: {abilityKey}");
                    return false;
            }
            
            abilityCooldowns[abilityKey] = currentTick + data.cooldownTicks;
            Messages.Message(
                $"⚡ {pawn.LabelShort} utilise {data.name} !",
                MessageTypeDefOf.PositiveEvent
            );
            return true;
        }


        // ✅ MÉTHODES D'ACTIVATION INTÉGRÉES (SANS PARAMÈTRE PAWN)
        private void ActivateRegeneration()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_RegenerationBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayRegenerationEffect(pawn);
        }

        private void ActivateBerserkerRage()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_BerserkerRageBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayBerserkerEffect(pawn);
        }

        private void ActivateQuickStep()
        {
            IntVec3 targetPos = FindDashDestination(pawn.Position, pawn.Rotation.FacingCell);

            if (targetPos.IsValid && IsValidDashTarget(targetPos))
            {
                // Effets visuels au point de départ
                VisualEffects.PlayDashEffect(pawn.Position, pawn.Map);
                
                // Téléportation sécurisée
                IntVec3 oldPosition = pawn.Position;
                
                // Méthode correcte pour déplacer un pawn
                if (pawn.Spawned)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                }
                
                GenSpawn.Spawn(pawn, targetPos, pawn.Map, pawn.Rotation);
                
                // Effets visuels au point d'arrivée
                VisualEffects.PlayDashEffect(targetPos, pawn.Map);
                
                // Notification de téléportation
                pawn.Notify_Teleported(false, true);
                
                // Message informatif
                Messages.Message(
                    $"⚡ {pawn.LabelShort} utilise Pas Éclair !",
                    new TargetInfo(targetPos, pawn.Map),
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                // Gestion d'échec
                Messages.Message(
                    $"❌ {pawn.LabelShort} ne peut pas se téléporter ici !",
                    new TargetInfo(pawn.Position, pawn.Map),
                    MessageTypeDefOf.RejectInput
                );
            }
        }

        // Méthode de validation de la cible
        private bool IsValidDashTarget(IntVec3 targetPos)
        {
            if (!targetPos.InBounds(pawn.Map))
                return false;
                
            // Vérification que la case est praticable
            if (!targetPos.Standable(pawn.Map))
                return false;
                
            // Vérification qu'il n'y a pas de pawn sur la case
            if (targetPos.GetFirstPawn(pawn.Map) != null)
                return false;
                
            // Vérification de la distance maximale (7 cases selon votre XML)
            float distance = pawn.Position.DistanceTo(targetPos);
            if (distance > 7f)
                return false;
                
            return true;
        }

        // Implémentation de FindDashDestination si elle manque
        private IntVec3 FindDashDestination(IntVec3 startPos, IntVec3 facingDirection)
        {
            // Cherche une position dans la direction regardée
            IntVec3 targetPos = startPos;
            
            // Essaie jusqu'à 4 cases dans la direction regardée
            for (int i = 1; i <= 4; i++)
            {
                IntVec3 candidate = startPos + (facingDirection * i);
                
                if (candidate.InBounds(pawn.Map) && candidate.Standable(pawn.Map))
                {
                    targetPos = candidate;
                }
                else
                {
                    break; // Arrête si on rencontre un obstacle
                }
            }
            
            return targetPos;
        }


        private void ActivateIronSkin()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_IronSkinBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayIronSkinEffect(pawn);
        }

        private void ActivateLuckyStrike()
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LuckyStrikeBuff");
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = 1.0f;
                pawn.health.AddHediff(hediff);
            }
            VisualEffects.PlayLuckyStrikeEffect(pawn);
        }

    

        // ✅ MÉTHODES D'EFFETS D'ATTRIBUTS (INCHANGÉES)
        private void ApplyAttributeEffects(AttributeType attribute)
        {
            RefreshAttributeEffects();
        }

        private void RefreshAttributeEffects()
        {
            RemoveAttributeHediffs();
            ApplyVitalityEffects();
            ApplyPowerEffects();
            ApplyAgilityEffects();
            ApplyDefenseEffects();
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

        private void ApplyPowerEffects()
        {
            if (power <= 10) return;

            try
            {
                var powerHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_PowerBonus");
                if (powerHediff != null)
                {
                    var existing = pawn.health.hediffSet.GetFirstHediffOfDef(powerHediff);
                    if (existing != null) pawn.health.RemoveHediff(existing);

                    var hediff = HediffMaker.MakeHediff(powerHediff, pawn);
                    hediff.Severity = Math.Min((power - 10) / 10f, 8.9f);
                    pawn.health.AddHediff(hediff);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Expedition33] Erreur appliquant bonus puissance: {ex.Message}");
            }
        }

        private void ApplyAgilityEffects()
        {
            if (agility <= 10) return;

            var agilityHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_AgilityBonus");
            if (agilityHediff != null)
            {
                var existing = pawn.health.hediffSet.GetFirstHediffOfDef(agilityHediff);
                if (existing != null) pawn.health.RemoveHediff(existing);

                var hediff = HediffMaker.MakeHediff(agilityHediff, pawn);
                hediff.Severity = (agility - 10) / 10f;
                pawn.health.AddHediff(hediff);
            }
        }

        private void ApplyDefenseEffects()
        {
            if (defense <= 10) return;

            var defenseHediff = DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DefenseBonus");
            if (defenseHediff != null)
            {
                var existing = pawn.health.hediffSet.GetFirstHediffOfDef(defenseHediff);
                if (existing != null) pawn.health.RemoveHediff(existing);

                var hediff = HediffMaker.MakeHediff(defenseHediff, pawn);
                hediff.Severity = (defense - 10) / 10f;
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
                "Expedition33_PowerBonus",
                "Expedition33_AgilityBonus",
                "Expedition33_DefenseBonus",
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

    // ✅ CLASSES SUPPORT POUR LES CAPACITÉS
    public static class SpecialAbilities
    {
        public static readonly Dictionary<string, AbilityData> AllAbilities = new Dictionary<string, AbilityData>
        {
            // 💪 Vitalité
            ["Regeneration"] = new AbilityData
            {
                requiredAttribute = AttributeType.Vitality,
                requiredLevel = 20,
                cooldownTicks = 20000, // Correspond au XML
                name = "Régénération",
                description = "Régénère progressivement les blessures pendant 30 secondes",
                iconPath = "UI/Icons/Abilities/AbilityRegeneration"
            },
            ["IronWill"] = new AbilityData
            {
                requiredAttribute = AttributeType.Vitality,
                requiredLevel = 40,
                cooldownTicks = 54000, // Correspond au XML
                name = "Volonté d'Acier",
                description = "Immunise temporairement contre les crises mentales",
                iconPath = "UI/Icons/Abilities/AbilityIronWill"
            },

            // ⚔️ Puissance
            ["BerserkerRage"] = new AbilityData
            {
                requiredAttribute = AttributeType.Power,
                requiredLevel = 20,
                cooldownTicks = 30000, // Correspond au XML
                name = "Rage Berserker",
                description = "Augmente les dégâts mais réduit la défense pendant 30 secondes",
                iconPath = "UI/Icons/Abilities/AbilityBerserker"
            },
            ["CrushingBlow"] = new AbilityData
            {
                requiredAttribute = AttributeType.Power,
                requiredLevel = 50,
                cooldownTicks = 45000, // Correspond au XML
                name = "Coup Dévastateur",
                description = "Inflige des dégâts dans une zone autour de la cible",
                iconPath = "UI/Icons/Abilities/AbilityCrush"
            },

            // 🏃 Agilité
            ["QuickStep"] = new AbilityData
            {
                requiredAttribute = AttributeType.Agility,
                requiredLevel = 20,
                cooldownTicks = 18000, // Correspond au XML
                name = "Pas Éclair",
                description = "Se téléporte instantanément vers un lieu proche",
                iconPath = "UI/Icons/Abilities/AbilityQuickStep"
            },
            ["TimeSlip"] = new AbilityData
            {
                requiredAttribute = AttributeType.Agility,
                requiredLevel = 60,
                cooldownTicks = 60000, // Correspond au XML
                name = "Glissement Temporel",
                description = "Augmente temporairement la vitesse d'action",
                iconPath = "UI/Icons/Abilities/AbilityTimeSlip"
            },

            // 🛡️ Défense
            ["IronSkin"] = new AbilityData
            {
                requiredAttribute = AttributeType.Defense,
                requiredLevel = 20,
                cooldownTicks = 32000, // Correspond au XML
                name = "Peau de Fer",
                description = "Réduit considérablement les dégâts reçus pendant 25 secondes",
                iconPath = "UI/Icons/Abilities/AbilityIronSkin"
            },
            ["Fortress"] = new AbilityData
            {
                requiredAttribute = AttributeType.Defense,
                requiredLevel = 80,
                cooldownTicks = 90000, // Correspond au XML
                name = "Forteresse",
                description = "Immunité presque totale aux dégâts pendant 8 secondes",
                iconPath = "UI/Icons/Abilities/AbilityFortress"
            },

            // 🍀 Chance
            ["LuckyStrike"] = new AbilityData
            {
                requiredAttribute = AttributeType.Chance,
                requiredLevel = 20,
                cooldownTicks = 20000, // Correspond au XML
                name = "Coup de Chance",
                description = "Augmente drastiquement la précision de la prochaine attaque",
                iconPath = "UI/Icons/Abilities/AbilityLuckyStrike"
            },
            ["Miracle"] = new AbilityData
            {
                requiredAttribute = AttributeType.Chance,
                requiredLevel = 99,
                cooldownTicks = 150000, // Correspond au XML
                name = "Miracle",
                description = "Protection divine temporaire contre les blessures mortelles",
                iconPath = "UI/Icons/Abilities/AbilityMiracle"
            }
        };
    
    }

    

    public class AbilityData
    {
        public AttributeType requiredAttribute;
        public int requiredLevel;
        public int cooldownTicks;
        public string name;
        public string description;
        public string iconPath;
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

    // ✅ ÉNUMÉRATIONS
    public enum AttributeType
    {
        Vitality,
        Power,
        Agility,
        Defense,
        Chance
    }

    // ✅ GIZMOS AVEC CAPACITÉS
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    static class Patch_ExpeditionGizmos
    {
        static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (var g in gizmos) yield return g;

            if (!__instance.IsColonistPlayerControlled) yield break;

            var progression = ExpeditionProgressionHelper.GetOrCreateProgression(__instance);
            if (progression == null) yield break;

            // Gizmos d'attributs
            if (progression.availablePoints > 0)
            {
                yield return new Command_Action
                {
                    defaultLabel = $"💪 Vitalité: {progression.vitality}",
                    defaultDesc = $"Améliorer vitalité (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = AttributeIcons.GetIcon(AttributeType.Vitality),
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Vitality))
                            Messages.Message($"Vitalité augmentée: {progression.vitality}", MessageTypeDefOf.PositiveEvent);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"⚔️ Puissance: {progression.power}",
                    defaultDesc = $"Améliorer puissance (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = AttributeIcons.GetIcon(AttributeType.Power),
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Power))
                            Messages.Message($"Puissance augmentée: {progression.power}", MessageTypeDefOf.PositiveEvent);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"🏃 Agilité: {progression.agility}",
                    defaultDesc = $"Améliorer agilité (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = AttributeIcons.GetIcon(AttributeType.Agility),
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Agility))
                            Messages.Message($"Agilité augmentée: {progression.agility}", MessageTypeDefOf.PositiveEvent);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"🛡️ Défense: {progression.defense}",
                    defaultDesc = $"Améliorer défense (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = AttributeIcons.GetIcon(AttributeType.Defense),
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Defense))
                            Messages.Message($"Défense augmentée: {progression.defense}", MessageTypeDefOf.PositiveEvent);
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = $"🍀 Chance: {progression.chance}",
                    defaultDesc = $"Améliorer chance (+1)\nPoints disponibles: {progression.availablePoints}",
                    icon = AttributeIcons.GetIcon(AttributeType.Chance),
                    action = () =>
                    {
                        if (progression.TrySpendAttributePoint(AttributeType.Chance))
                            Messages.Message($"Chance augmentée: {progression.chance}", MessageTypeDefOf.PositiveEvent);
                    }
                };
            }

            // ✅ NOUVEAU : Gizmos des capacités spéciales
            foreach (string abilityKey in progression.unlockedAbilities)
            {
                var data = SpecialAbilities.AllAbilities[abilityKey];
                bool canUse = progression.CanUseAbility(abilityKey);

                var gizmo = new Command_Action
                {
                    defaultLabel = data.name,
                    defaultDesc = GetAbilityDescription(abilityKey, progression),
                    icon = ContentFinder<Texture2D>.Get(data.iconPath, false) ?? BaseContent.BadTex,
                    action = () => progression.TryActivateAbility(abilityKey)
                };

                if (!canUse)
                {
                    int cooldownEnd;
                    if (!progression.abilityCooldowns.TryGetValue(abilityKey, out cooldownEnd))
                        cooldownEnd = 0;
                    int remainingTicks = cooldownEnd - GenTicks.TicksGame;
                    gizmo.Disable($"Cooldown: {(remainingTicks / 60f):F0}s");
                }

                yield return gizmo;
            }

            // Gizmos d'information
            yield return new Command_Action
            {
                defaultLabel = "📊 Progression Détaillée",
                defaultDesc = "Ouvrir la fenêtre de progression complète",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Progression", false) ?? BaseContent.BadTex,
                action = () => Find.WindowStack.Add(new Window_ExpeditionProgression(__instance))
            };

            yield return new Command_Action
            {
                defaultLabel = "📋 Info Progression",
                defaultDesc = $"Niveau: {progression.currentLevel}\n" +
                             $"💪 Vitalité: {progression.vitality} | ⚔️ Puissance: {progression.power}\n" +
                             $"🏃 Agilité: {progression.agility} | 🛡️ Défense: {progression.defense} | 🍀 Chance: {progression.chance}\n" +
                             $"XP Combat: {progression.combatXP:F1} | XP Âme: {progression.soulXP:F1}\n" +
                             $"Points disponibles: {progression.availablePoints}",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Progression", false) ?? BaseContent.BadTex,
                action = () => { }
            };
        }

        private static string GetAbilityDescription(string abilityKey, Hediff_ExpeditionProgression progression)
        {
            var data = SpecialAbilities.AllAbilities[abilityKey];
            string baseDesc = data.description;

            if (progression.CanUseAbility(abilityKey))
            {
                return $"{baseDesc}\n\n✅ Prêt à utiliser !";
            }
            else
            {
                int cooldownEnd;
                if (!progression.abilityCooldowns.TryGetValue(abilityKey, out cooldownEnd))
                    cooldownEnd = 0;
                progression.abilityCooldowns.TryGetValue(abilityKey, out cooldownEnd);
                int remainingTicks = cooldownEnd - GenTicks.TicksGame;
                return $"{baseDesc}\n\n⏱️ Cooldown: {(remainingTicks / 60f):F0}s";
            }
        }
        
        

    }

    // ✅ PATCHES XP (RÉÉQUILIBRÉS)
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
                // ✅ VALEURS RÉÉQUILIBRÉES
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
                    progression.GainSoulXP(8f, "Construction importante"); // ✅ Valeur réduite
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
    }

    // ✅ COMMANDES DE DEBUG
    public static class ExpeditionDebugCommands
    {
        [DebugAction("Expedition33", "Donner XP Test", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void GiveTestXP()
        {
            var selected = Find.Selector.SingleSelectedThing as Pawn;
            if (selected?.IsColonist == true)
            {
                ExpeditionProgressionHelper.GainXP(selected, 1000f, "combat");
                ExpeditionProgressionHelper.GainXP(selected, 200f, "soul");
                Messages.Message($"{selected.Name} reçoit 200 XP combat et âme", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Sélectionnez un colon d'abord", MessageTypeDefOf.RejectInput);
            }
        }

        [DebugAction("Expedition33", "Test Capacité", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        static void TestAbility()
        {
            var selected = Find.Selector.SingleSelectedThing as Pawn;
            if (selected?.IsColonist == true)
            {
                var progression = ExpeditionProgressionHelper.GetOrCreateProgression(selected);
                if (progression != null)
                {
                    // Déverrouillez toutes les capacités pour test
                    progression.vitality = 25;
                    progression.power = 25;
                    progression.agility = 25;
                    progression.defense = 25;
                    progression.chance = 25;
                    progression.CheckAbilityUnlocks();
                }
            }
        }
    }

    // ✅ EFFETS VISUELS ÉTENDUS
    public static class VisualEffects
    {
        public static void PlayLevelUpEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 20; i++)
                {
                    FleckMaker.ThrowDustPuffThick(
                        pawn.Position.ToVector3() + new Vector3(
                            UnityEngine.Random.Range(-1f, 1f),
                            0f,
                            UnityEngine.Random.Range(-1f, 1f)
                        ),
                        pawn.Map,
                        2.0f,
                        Color.yellow
                    );
                }

                SoundDefOf.Quest_Concluded.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
        }

        public static void PlayAbilityUnlockEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 15; i++)
                {
                    FleckMaker.ThrowDustPuffThick(
                        pawn.Position.ToVector3() + new Vector3(
                            UnityEngine.Random.Range(-1.5f, 1.5f),
                            0f,
                            UnityEngine.Random.Range(-1.5f, 1.5f)
                        ),
                        pawn.Map,
                        3.0f,
                        Color.cyan
                    );
                }
            }
        }

        public static void PlayRegenerationEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 1.5f, Color.green);
                }
            }
        }

        public static void PlayBerserkerEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 12; i++)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 2.0f, Color.red);
                }
            }
        }

        public static void PlayDashEffect(IntVec3 position, Map map)
        {
            for (int i = 0; i < 8; i++)
            {
                FleckMaker.ThrowDustPuffThick(position.ToVector3(), map, 1.0f, Color.blue);
            }
        }

        public static void PlayIronSkinEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 1.5f, Color.gray);
                }
            }
        }

        public static void PlayLuckyStrikeEffect(Pawn pawn)
        {
            if (pawn.Spawned)
            {
                for (int i = 0; i < 8; i++)
                {
                    FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 1.2f, Color.yellow);
                }
            }
        }

        public static void PlayIronWillEffect(Pawn pawn)
{
    if (pawn.Spawned)
    {
        for (int i = 0; i < 10; i++)
        {
            FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 1.5f, new Color(0.5f, 0.5f, 0.8f));
        }
    }
}

public static void PlayCrushingBlowEffect(Pawn pawn)
{
    if (pawn.Spawned)
    {
        for (int i = 0; i < 15; i++)
        {
            FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 2.5f, Color.black);
        }
    }
}

public static void PlayTimeSlipEffect(Pawn pawn)
{
    if (pawn.Spawned)
    {
        for (int i = 0; i < 12; i++)
        {
            FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 1.8f, new Color(0.2f, 0.8f, 0.8f));
        }
    }
}

public static void PlayFortressEffect(Pawn pawn)
{
    if (pawn.Spawned)
    {
        for (int i = 0; i < 20; i++)
        {
            FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 2.0f, new Color(0.8f, 0.8f, 0.2f));
        }
    }
}

public static void PlayMiracleEffect(Pawn pawn)
{
    if (pawn.Spawned)
    {
        for (int i = 0; i < 25; i++)
        {
            FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3(), pawn.Map, 3.0f, Color.white);
        }
        FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3(), pawn.Map, 4f);
    }
}

    }

    // ✅ CLASSE ICÔNES
    public static class AttributeIcons
    {
        private static readonly Dictionary<AttributeType, Texture2D> iconCache = new Dictionary<AttributeType, Texture2D>();

        public static Texture2D GetIcon(AttributeType type)
        {
            if (!iconCache.ContainsKey(type))
            {
                string iconPath = type switch
                {
                    AttributeType.Vitality => "UI/Icons/Vitality",
                    AttributeType.Power => "UI/Icons/Power",
                    AttributeType.Agility => "UI/Icons/Agility",
                    AttributeType.Defense => "UI/Icons/Defense",
                    AttributeType.Chance => "UI/Icons/Chance",
                    _ => null
                };

                var texture = ContentFinder<Texture2D>.Get(iconPath, false);
                iconCache[type] = texture ?? BaseContent.BadTex;
            }

            return iconCache[type];
        }
    }

    // ✅ MOD PRINCIPAL
    
}
