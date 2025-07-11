using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using static Mod_warult.ThinkNode_AbilityUser;

namespace Mod_warult
{
    public class JobDriver_ExecuteAbility : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Aller vers la cible si nécessaire
            if (job.GetTarget(TargetIndex.A).Thing != pawn)
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }

            // Toil de préparation/incantation avec temps d'attente
            yield return CreateCastingToil();

            // Toil d'exécution après incantation
            yield return new Toil
            {
                initAction = () =>
                {
                    var verb = job.verbToUse as Verb_ExecuteAbility;
                    if (verb != null)
                    {
                        ExecuteAbilityDirect(verb.caster, verb.ability, job.GetTarget(TargetIndex.A).Thing);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }

        private Toil CreateCastingToil()
        {
            var verb = job.verbToUse as Verb_ExecuteAbility;
            int castingTicks = GetCastingTime(verb?.ability);

            return new Toil
            {
                initAction = () =>
                {
                    // Afficher message de début d'incantation
                    Messages.Message($"🔮 {pawn.LabelShort} commence à incanter {verb?.ability?.name}...",
                        MessageTypeDefOf.NeutralEvent);

                    // Effet visuel pendant l'incantation
                    StartCastingEffects(pawn, verb?.ability);
                },
                tickAction = () =>
                {
                    // Vérifier si l'incantation peut continuer
                    if (!CanContinueCasting(pawn, verb?.ability))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }

                    // Effets visuels continus
                    ContinueCastingEffects(pawn, verb?.ability);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = castingTicks
            };
        }

        private int GetCastingTime(BossAbility ability)
        {
            if (ability == null) return 60; // 1 seconde par défaut

            return ability.name switch
            {
                "IndigoBlast" => 180,        // 3 secondes - capacité puissante
                "DivinePurification" => 240, // 4 secondes - auto-soin
                "SacredLight" => 120,        // 2 secondes - capacité de zone
                "ClairStrike" => 90,         // 1.5 secondes - attaque rapide
                "ObscurBlast" => 150,        // 2.5 secondes - attaque sombre
                "DualityShift" => 300,       // 5 secondes - transformation
                "LethalStrike" => 60,        // 1 seconde - attaque létale
                "FearAura" => 180,           // 3 secondes - aura de peur
                "RapidMovement" => 30,       // 0.5 secondes - mouvement
                "HeavyArmor" => 120,         // 2 secondes - armure
                "AreaAttacks" => 150,        // 2.5 secondes - attaque de zone
                "SlowRegeneration" => 180,   // 3 secondes - régénération
                "EnergyShields" => 240,      // 4 secondes - boucliers
                _ => 120 // 2 secondes par défaut
            };
        }

        private bool CanContinueCasting(Pawn caster, BossAbility ability)
        {
            // Vérifier si le caster peut continuer l'incantation
            if (caster.Downed || caster.Dead) return false;
            if (caster.stances.stunner.Stunned) return false;

            // Vérifier si la cible est toujours valide pour les capacités ciblées
            if (ability != null && !ability.selfCast)
            {
                var target = job.GetTarget(TargetIndex.A).Thing;
                if (target == null || target.Destroyed) return false;

                // Vérifier la distance pour les capacités à distance
                if (caster.Position.DistanceTo(target.Position) > ability.range)
                {
                    Messages.Message($"⚠️ {caster.LabelShort} annule son incantation - cible trop éloignée",
                        MessageTypeDefOf.RejectInput);
                    return false;
                }
            }

            return true;
        }

        private void StartCastingEffects(Pawn caster, BossAbility ability)
        {
            if (ability == null) return;

            // Effets visuels selon le type de capacité
            Color effectColor = ability.name switch
            {
                "IndigoBlast" => new Color(0.3f, 0f, 0.8f),      // Violet indigo
                "DivinePurification" => Color.white,              // Blanc divin
                "SacredLight" => Color.yellow,                    // Jaune sacré
                "ClairStrike" => Color.cyan,                      // Cyan clair
                "ObscurBlast" => Color.black,                     // Noir obscur
                "DualityShift" => Color.gray,                     // Gris dualité
                _ => Color.blue                                   // Bleu par défaut
            };

            // Particules d'incantation
            FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                caster.Map, 2f, effectColor);
        }

        private void ContinueCastingEffects(Pawn caster, BossAbility ability)
        {
            // Effets visuels continus pendant l'incantation
            if (GenTicks.TicksGame % 10 == 0) // Toutes les 10 ticks
            {
                StartCastingEffects(caster, ability);
            }
        }



        private void ExecuteAbilityDirect(Pawn caster, BossAbility ability, Thing target)
        {
            if (Prefs.DevMode)
            {
                Log.Message($"[Expedition33] {caster.LabelShort} utilise {ability.name}");
            }

            // NOUVEAU : Vérifier si c'est une capacité ultime en phase désespérée
            var currentPhase = GetCurrentPhase(caster);
            if (currentPhase == BossPhase.Phase3_Desperate && ShouldUseUltimate(caster, ability))
            {
                ExecuteUltimateAbility(caster);
                return;
            }

            // NOUVEAU : Afficher dialogue épique
            DisplayBossDialogue(caster, ability.name);

            // Exécution normale des capacités
            switch (ability.name)
            {
                // Capacités existantes...
                case "IndigoBlast": AbilityExecuter.ExecuteIndigoBlast(caster, target); break;
                case "DivinePurification": AbilityExecuter.ExecuteDivinePurification(caster); break;
                case "SacredLight": AbilityExecuter.ExecuteSacredLight(caster, target); break;
                case "RapidMovement": AbilityExecuter.ExecuteRapidMovement(caster); break;
                case "HeavyArmor": AbilityExecuter.ExecuteHeavyArmor(caster); break;
                case "AreaAttacks": AbilityExecuter.ExecuteAreaAttacks(caster, target); break;
                case "SlowRegeneration": AbilityExecuter.ExecuteSlowRegeneration(caster); break;
                case "EnergyShields": AbilityExecuter.ExecuteEnergyShields(caster); break;
                case "ClairStrike": AbilityExecuter.ExecuteClairStrike(caster, target); break;
                case "ObscurBlast": AbilityExecuter.ExecuteObscurBlast(caster, target); break;
                case "DualityShift": AbilityExecuter.ExecuteDualityShift(caster); break;
                case "LethalStrike": AbilityExecuter.ExecuteLethalStrike(caster, target); break;
                case "FearAura": AbilityExecuter.ExecuteFearAura(caster); break;

                // Nouveaux boss
                case "LightManipulation": AbilityExecuter.ExecuteLightManipulation(caster, target); break;
                case "IlluminationAttacks": AbilityExecuter.ExecuteIlluminationAttacks(caster, target); break;
                case "LampMastery": AbilityExecuter.ExecuteLampMastery(caster); break;
                case "CommanderAura": AbilityExecuter.ExecuteCommanderAura(caster); break;
                case "TacticalStrike": AbilityExecuter.ExecuteTacticalStrike(caster, target); break;
                case "ExpeditionMemory": AbilityExecuter.ExecuteExpeditionMemory(caster); break;
                case "SonicWave": AbilityExecuter.ExecuteSonicWave(caster, target); break;
                case "MindControl": AbilityExecuter.ExecuteMindControl(caster, target); break;
                case "FaceShift": AbilityExecuter.ExecuteFaceShift(caster); break;
                case "RealityDistortion": AbilityExecuter.ExecuteRealityDistortion(caster); break;
                case "GommageRitual": AbilityExecuter.ExecuteGommageRitual(caster); break;
                case "CosmicBrush": AbilityExecuter.ExecuteCosmicBrush(caster, target); break;
                case "NumberInscription": AbilityExecuter.ExecuteNumberInscription(caster, target); break;
                case "MonolithPower": AbilityExecuter.ExecuteMonolithPower(caster); break;
                case "ImmortalityFragment": AbilityExecuter.ExecuteImmortalityFragment(caster); break;
                case "FractureParfaite": AbilityExecuter.ExecuteFractureParfaite(caster, target); break;
                case "CorruptedRage": AbilityExecuter.ExecuteCorruptedRage(caster); break;
                case "SilenceAura": AbilityExecuter.ExecuteSilenceAura(caster); break;
                case "InvisibleBarriers": AbilityExecuter.ExecuteInvisibleBarriers(caster, target); break;

                default:
                    Log.Warning($"[Expedition33] Capacité inconnue : {ability.name}");
                    break;
            }
        }

        private BossPhase GetCurrentPhase(Pawn pawn)
        {
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;

            return healthPercent switch
            {
                >= 0.7f => BossPhase.Phase1_Normal,
                >= 0.4f => BossPhase.Phase2_Aggressive,
                _ => BossPhase.Phase3_Desperate
            };
        }


        private bool ShouldUseUltimate(Pawn caster, BossAbility ability)
        {
            // 20% de chance d'utiliser l'ultime en phase désespérée
            return Rand.Chance(0.2f) && caster.health.summaryHealth.SummaryHealthPercent < 0.3f;
        }


        private void ExecuteUltimateAbility(Pawn caster)
        {
            switch (caster.kindDef.defName)
            {
                case "Expedition33_Eveque":
                    AbilityExecuter.ExecuteJudgmentDay(caster);
                    break;
                case "Expedition33_Dualiste":
                    AbilityExecuter.ExecuteDualityShift(caster);
                    break;
                case "Expedition33_Francois":
                    AbilityExecuter.ExecuteFearAura(caster);
                    break;
            }
        }


        private void DisplayBossDialogue(Pawn boss, string abilityName)
        {
            var dialogues = new Dictionary<(string, string), string>
            {
                // Évêque
                { ("Expedition33_Eveque", "DivinePurification"), 
                "💬 \"La lumière divine me purifie ! Vous ne pouvez vaincre la foi !\"" },
                { ("Expedition33_Eveque", "SacredLight"), 
                "💬 \"Que la lumière sacrée guide mes alliés et aveugle mes ennemis !\"" },
                { ("Expedition33_Eveque", "IndigoBlast"), 
                "💬 \"Contemplez la colère divine dans cette explosion d'indigo !\"" },
                
                // Dualiste
                { ("Expedition33_Dualiste", "DualityShift"), 
                "💬 \"Je suis la lumière ET les ténèbres ! Contemplez ma vraie nature !\"" },
                { ("Expedition33_Dualiste", "ClairStrike"), 
                "💬 \"Que la lumière vous aveugle !\"" },
                { ("Expedition33_Dualiste", "ObscurBlast"), 
                "💬 \"Les ténèbres vous engloutissent !\"" },
                
                // François
                { ("Expedition33_Francois", "LethalStrike"), 
                "💬 \"Votre heure a sonné... Préparez-vous à mourir !\"" },
                { ("Expedition33_Francois", "FearAura"), 
                "💬 \"Laissez la terreur envahir vos cœurs !\"" },
                
                // Sakapatates
                { ("Expedition33_SakapatateRobuste", "HeavyArmor"), 
                "💬 \"Mon blindage est impénétrable !\"" },
                { ("Expedition33_SakapatateUltime", "EnergyShields"), 
                "💬 \"Mes boucliers énergétiques me protègent !\"" }
            };

            if (dialogues.TryGetValue((boss.kindDef.defName, abilityName), out string dialogue))
            {
                Messages.Message(dialogue, MessageTypeDefOf.ThreatBig);
            }
        }



    }

    public class Verb_ExecuteAbility : Verb
    {
        public BossAbility ability;
        public new Pawn caster;

        protected override bool TryCastShot() => true;
    }
    
    
}
