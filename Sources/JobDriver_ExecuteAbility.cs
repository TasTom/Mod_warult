using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using static Mod_warult.ThinkNode_AbilityUser;
using System.Linq;
using System;

namespace Mod_warult
{
    public class JobDriver_ExecuteAbility : JobDriver
    {
         private const int REEVALUATION_INTERVAL = 180;
        // private int lastReevaluationTick = 0;
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            /* ───────── VALIDATIONS ───────── */

            LocalTargetInfo targInfo = job.GetTarget(TargetIndex.A);
            if (!targInfo.IsValid)
                yield break;

            Verb_ExecuteAbility verbEx = job.verbToUse as Verb_ExecuteAbility;
            if (verbEx?.ability == null)
            {
                Log.Error("[Expedition33] JobDriver_ExecuteAbility : verb null !");
                yield break;
            }

            BossAbility ability = verbEx.ability;
            bool isMelee = IsMeleeAbility(ability);
            float range = ability.range > 0f ? ability.range
                         : verbEx.range > 0f ? verbEx.range   // fallback sur le verb
                         : 12f;                                     // valeur par défaut

            /* ───────── 1. DEPLACEMENT (si hors-portée) ───────── */

            // Distance actuelle à la cellule cible (pas au pawn, car le target peut être Thing)
            float dist = pawn.Position.DistanceTo(targInfo.Cell);

            if (dist > range)
            {
                IntVec3 dest;

                if (isMelee)
                {
                    // Mélee : aller au contact
                    dest = targInfo.Cell;
                    yield return Toils_Goto.GotoCell(dest, PathEndMode.Touch)
                                          .FailOnDestroyedOrNull(TargetIndex.A);
                }
                else
                {
                    // Distance : se placer à portée optimale (range - 1)
                    dest = GetOptimalRangedPosition(pawn, targInfo.Cell, range);

                    if (dest.IsValid)
                        yield return Toils_Goto.GotoCell(dest, PathEndMode.OnCell)
                                              .FailOnDestroyedOrNull(TargetIndex.A);
                    else
                    {
                        Log.Warning($"[Expedition33] Impossible de trouver une cellule de tir pour {pawn.LabelShort}");
                        yield break;
                    }
                }
            }

            /* ───────── 2. PRE-CAST : stopper le déplacement ───────── */

            yield return new Toil
            {
                initAction = () =>
                {
                    pawn.pather.StopDead();
                    pawn.stances.stunner.StopStun();
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = 10
            };

            /* ───────── 3. TOIL D’INCANTATION ───────── */

            yield return CreateCastingToil();

            /* ───────── 4. EXECUTION IMMÉDIATE ───────── */

            yield return new Toil
            {
                initAction = () =>
                {
                    Log.Message($"[Boss AI] {pawn.LabelShort} lance {ability.name} sur {targInfo.Thing.LabelShort}");
                    ExecuteAbilityDirect(pawn, ability, targInfo.Thing);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }





        // ✅ NOUVELLE MÉTHODE : Calculer position optimale pour capacités à distance
        private IntVec3 GetOptimalRangedPosition(Pawn caster, IntVec3 targetCell, float range)
        {
            // On vise une case à (range-1) cases de la cible, ligne de vue et walkable.
            float desiredDist = Mathf.Max(2f, range - 1f);

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(targetCell, desiredDist, true))
            {
                if (!cell.InBounds(caster.Map)) continue;
                if (!cell.Standable(caster.Map)) continue;
                if (cell.DistanceTo(targetCell) > range) continue;
                if (!caster.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)) continue;

                // ligne de vue
                if (GenSight.LineOfSight(cell, targetCell, caster.Map, true))
                    return cell;
            }

            // Fallback : rester où il est
            return caster.Position;
        }


        private Toil CreateSmartMovementToil(LocalTargetInfo target, float range, bool isMelee)
        {
            return new Toil
            {
                initAction = () =>
                {
                    IntVec3 destination = isMelee ? target.Cell :
                        RCellAroundTargetAtRange(pawn, target.Cell, range);
                    pawn.pather.StartPath(destination, isMelee ? PathEndMode.Touch : PathEndMode.OnCell);
                },
                tickAction = () =>
                {
                    // Réévaluer toutes les 60 ticks (1 seconde)
                    if (Find.TickManager.TicksGame % 60 == 0)
                    {
                        // Vérifier si la cible est toujours valide
                        if (target.Thing == null || target.Thing.Destroyed ||
                            (target.Thing is Pawn p && (p.Dead || p.Downed)))
                        {
                            // Chercher une nouvelle cible
                            var newTarget = FindNewTarget();
                            if (newTarget != null)
                            {
                                job.SetTarget(TargetIndex.A, newTarget);
                                IntVec3 newDest = isMelee ? newTarget.Position :
                                    RCellAroundTargetAtRange(pawn, newTarget.Position, range);
                                pawn.pather.StartPath(newDest, isMelee ? PathEndMode.Touch : PathEndMode.OnCell);
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.PatherArrival
            };
        }


        private Thing FindNewTarget()
        {
            return pawn.Map.mapPawns.FreeColonists
                .Where(p => !p.Downed && !p.Dead)
                .OrderBy(p => pawn.Position.DistanceTo(p.Position))
                .FirstOrDefault();
        }


        private bool IsMeleeAbility(BossAbility ability)
        {
            return ability.name switch
            {
                // Ajoutez ici vos capacités de mêlée
                _ => false
            };
        }

        private IntVec3 RCellAroundTargetAtRange(Pawn pawn, IntVec3 target, float range)
        {
            Map map = pawn.Map;
            IntVec3 best = IntVec3.Invalid;
            float closestDist = float.MaxValue;
            
            foreach (var cell in GenRadial.RadialCellsAround(target, range, true))
            {
                if (!cell.InBounds(map) || !cell.Walkable(map))
                    continue;
                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                    continue;
                    
                float dist = pawn.Position.DistanceTo(cell);
                if (dist < closestDist)
                {
                    best = cell;
                    closestDist = dist;
                }
            }
            return best;
        }

        private Toil CreateCastingToil()
        {
            var verb = job.verbToUse as Verb_ExecuteAbility;
            int castingTicks = GetCastingTime(verb?.ability);
            
            return new Toil
            {
                initAction = () =>
                {
                    if (verb?.ability?.name != "RangeAttack")
                    {
                        Messages.Message("Expedition33_CastingStarted".Translate(pawn.LabelShort, verb?.ability?.name),
                            MessageTypeDefOf.NeutralEvent);
                        StartCastingEffects(pawn, verb?.ability);
                    }
                },
                tickAction = () =>
                {
                    if (!CanContinueCasting(pawn, verb?.ability))
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
                        return;
                    }
                    ContinueCastingEffects(pawn, verb?.ability);
                },
                defaultCompleteMode = ToilCompleteMode.Delay,
                defaultDuration = castingTicks
            };
        }

        private int GetCastingTime(BossAbility ability)
        {
            if (ability == null) return 60;
            
            return ability.name switch
            {
                "IndigoBlast" => 180,
                "DivinePurification" => 240,
                "SacredLight" => 120,
                "ClairStrike" => 90,
                "ObscurBlast" => 150,
                "DualityShift" => 300,
                "LethalStrike" => 60,
                "FearAura" => 180,
                "RapidMovement" => 30,
                "HeavyArmor" => 120,
                "AreaAttacks" => 150,
                "SlowRegeneration" => 180,
                "EnergyShields" => 240,
                _ => 120
            };
        }

        private bool CanContinueCasting(Pawn caster, BossAbility ability)
        {
            if (caster.Downed || caster.Dead) return false;

            // ✅ IGNORER COMPLÈTEMENT LES STUNS pour les boss
            if (caster.kindDef?.defName?.StartsWith("Expedition33_") == true)
            {
                if (caster.stances.stunner.Stunned)
                {
                    caster.stances.stunner.StopStun();
                }
            }

            if (ability != null && !ability.selfCast)
            {
                var target = job.GetTarget(TargetIndex.A).Thing;
                if (target == null || target.Destroyed) return false;

                // ✅ AJOUT : Vérification ligne de vue pour les attaques à distance
                if (ability.name == "RangeAttack" || ability.name == "Expedition33_RangeAttack")
                {
                    if (!GenSight.LineOfSight(caster.Position, target.Position, caster.Map, true))
                    {
                        Log.Warning($"[ExecuteAbility] {ability.name} annulé : pas de ligne de vue");
                        return false;
                    }
                }

                // ✅ MARGE ULTRA-TOLÉRANTE pour les boss
                float maxRange = caster.kindDef?.defName?.StartsWith("Expedition33_") == true ?
                    ability.range + 5f : ability.range;

                if (caster.Position.DistanceTo(target.Position) > maxRange)
                {
                    Log.Warning($"[ExecuteAbility] {ability.name} annulé : cible trop loin ({caster.Position.DistanceTo(target.Position):F1} > {maxRange:F1})");
                    return false;
                }
            }
            return true;
        }







        private void StartCastingEffects(Pawn caster, BossAbility ability)
        {
            if (ability == null) return;
            
            Color effectColor = ability.name switch
            {
                "IndigoBlast" => new Color(0.3f, 0f, 0.8f),
                "DivinePurification" => Color.white,
                "SacredLight" => Color.yellow,
                "ClairStrike" => Color.cyan,
                "ObscurBlast" => Color.black,
                "DualityShift" => Color.gray,
                _ => Color.blue
            };

            FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                caster.Map, 2f, effectColor);
        }

        private void ContinueCastingEffects(Pawn caster, BossAbility ability)
        {
            if (GenTicks.TicksGame % 10 == 0)
            {
                StartCastingEffects(caster, ability);
            }
        }

        private void ExecuteAbilityDirect(Pawn caster, BossAbility ability, Thing target)
        {
            string abilityKey = ability?.name ?? "";
            if (abilityKey.StartsWith("Expedition33_") == false && abilityKey.StartsWith("Verso") == false)
            {
                abilityKey = "Expedition33_" + abilityKey;
            }

            if (Prefs.DevMode)
            {
                Log.Message("Expedition33_AbilityUsed".Translate(caster.LabelShort, abilityKey));
            }

            var currentPhase = GetCurrentPhase(caster);
            if (currentPhase == BossAbility.BossPhase.Phase3_Desperate && ShouldUseUltimate(caster, ability))
            {
                ExecuteUltimateAbility(caster);
                return;
            }

            DisplayBossDialogue(caster, abilityKey);

            bool handled = true;
            switch (abilityKey)
            {
                // Capacités Verso natives
                case "ImmortalityFragment":
                case "Expedition33_ImmortalityFragment":
                    AbilityExecuter.ExecuteImmortalityFragment(caster);
                    break;
                case "FractureParfaite":
                case "Expedition33_FractureParfaite":
                    AbilityExecuter.ExecuteFractureParfaite(caster, target);
                    break;

                // ✅ CAPACITÉS GOBLU MANQUANTES
                case "RapidMovement":
                case "Expedition33_RapidMovement":
                    AbilityExecuter.ExecuteRapidMovement(caster);
                    break;

                case "PrimalHeal":
                case "Expedition33_PrimalHeal":
                    AbilityExecuter.ExecutePrimalHeal(caster);
                    break;

                // ✅ AUTRES CAPACITÉS BOSS
                case "IndigoBlast":
                case "Expedition33_IndigoBlast":
                    AbilityExecuter.ExecuteIndigoBlast(caster, target);
                    break;

                case "DivinePurification":
                case "Expedition33_DivinePurification":
                    AbilityExecuter.ExecuteDivinePurification(caster);
                    break;

                case "SacredLight":
                case "Expedition33_SacredLight":
                    AbilityExecuter.ExecuteSacredLight(caster, target);
                    break;

                case "ClairStrike":
                case "Expedition33_ClairStrike":
                    AbilityExecuter.ExecuteClairStrike(caster, target);
                    break;

                case "ObscurBlast":
                case "Expedition33_ObscurBlast":
                    AbilityExecuter.ExecuteObscurBlast(caster, target);
                    break;

                case "DualityShift":
                case "Expedition33_DualityShift":
                    AbilityExecuter.ExecuteDualityShift(caster);
                    break;

                case "LethalStrike":
                case "Expedition33_LethalStrike":
                    AbilityExecuter.ExecuteLethalStrike(caster, target);
                    break;

                case "FearAura":
                case "Expedition33_FearAura":
                    AbilityExecuter.ExecuteFearAura(caster);
                    break;

                case "HeavyArmor":
                case "Expedition33_HeavyArmor":
                    AbilityExecuter.ExecuteHeavyArmor(caster);
                    break;

                case "CannonShot":
                case "Expedition33_CannonShot":
                    AbilityExecuter.ExecuteCannonShot(caster, target);
                    break;

                case "AreaAttacks":
                case "Expedition33_AreaAttacks":
                    AbilityExecuter.ExecuteAreaAttacks(caster, target);
                    break;

                case "SlowRegeneration":
                case "Expedition33_SlowRegeneration":
                    AbilityExecuter.ExecuteSlowRegeneration(caster);
                    break;

                case "EnergyShields":
                case "Expedition33_EnergyShields":
                    AbilityExecuter.ExecuteEnergyShields(caster);
                    break;

                case "RangeAttack":
                case "Expedition33_RangeAttack":
                    AbilityExecuter.ExecuteRangeAttack(caster, target);
                    break;

                case "Expedition33_LightManipulation":
                    AbilityExecuter.ExecuteLightManipulation(caster, target);
                    break;

                case "Expedition33_IlluminationAttacks":
                    AbilityExecuter.ExecuteIlluminationAttacks(caster, target);
                    break;

                case "Expedition33_LampMastery":
                    AbilityExecuter.ExecuteLampMastery(caster);
                    break;

                case "Expedition33_CommanderAura":
                    AbilityExecuter.ExecuteCommanderAura(caster);
                    break;

                case "Expedition33_ExpeditionMemory":
                    AbilityExecuter.ExecuteExpeditionMemory(caster);
                    break;

                case "Expedition33_TacticalStrike":
                    AbilityExecuter.ExecuteTacticalStrike(caster, target);
                    break;

                case "Expedition33_SonicWave":
                    AbilityExecuter.ExecuteSonicWave(caster, target);
                    break;

                case "Expedition33_MindControl":
                    AbilityExecuter.ExecuteMindControl(caster, target);
                    break;

                case "Expedition33_MonolithPower":
                    AbilityExecuter.ExecuteMonolithPower(caster);
                    break;

                case "Expedition33_FaceShift":
                    AbilityExecuter.ExecuteFaceShift(caster);
                    break;

                case "Expedition33_RealityDistortion":
                    AbilityExecuter.ExecuteRealityDistortion(caster);
                    break;

                case "Expedition33_GommageRitual":
                    AbilityExecuter.ExecuteGommageRitual(caster);
                    break;
                case "Expedition33_CosmicBrush":
                    AbilityExecuter.ExecuteCosmicBrush(caster, target);
                    break;
                case "Expedition33_NumberInscription":
                    AbilityExecuter.ExecuteNumberInscription(caster, target);
                    break;

                case "Expedition33_NevronPainting":
                    AbilityExecuter.ExecuteNevronPainting(caster);
                    break;

                case "Expedition33_CorruptedRage":
                    AbilityExecuter.ExecuteCorruptedRage(caster);
                    break;

                case "Expedition33_SilenceAura":
                    AbilityExecuter.ExecuteSilenceAura(caster);
                    break;

                case "Expedition33_InvisibleBarriers":
                    AbilityExecuter.ExecuteInvisibleBarriers(caster, target);
                    break;

                default:
                    handled = false;
                    break;
            }

            // ✅ AJOUT : Forcer le retour au combat après capacité
            if (!handled)
            {
                Log.Warning("Expedition33_UnknownAbility".Translate(abilityKey));
            }

            // Redémarrer l'IA de combat après l'exécution de la capacité
            caster.jobs.EndCurrentJob(JobCondition.Succeeded);

            // Forcer la recherche d'une nouvelle cible
            if (caster.Faction != Faction.OfPlayer)
            {
                var nearbyEnemies = caster.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p.Faction?.IsPlayer == true && !p.Downed)
                    .OrderBy(p => caster.Position.DistanceTo(p.Position))
                    .FirstOrDefault();

                if (nearbyEnemies != null)
                {
                    caster.mindState.enemyTarget = nearbyEnemies;
                }
            }
        }



        private BossAbility.BossPhase GetCurrentPhase(Pawn pawn)
        {
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
            return healthPercent switch
            {
                >= 0.7f => BossAbility.BossPhase.Phase1_Normal,
                >= 0.4f => BossAbility.BossPhase.Phase2_Aggressive,
                _ => BossAbility.BossPhase.Phase3_Desperate
            };
        }

        private bool ShouldUseUltimate(Pawn caster, BossAbility ability)
        {
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
                  "Expedition33_BishopDialoguePurification".Translate() },
                { ("Expedition33_Eveque", "SacredLight"),
                  "Expedition33_BishopDialogueLight".Translate() },
                { ("Expedition33_Eveque", "IndigoBlast"),
                  "Expedition33_BishopDialogueIndigo".Translate() },
                // Dualiste
                { ("Expedition33_Dualiste", "DualityShift"),
                  "Expedition33_DualistDialogueShift".Translate() },
                { ("Expedition33_Dualiste", "ClairStrike"),
                  "Expedition33_DualistDialogueLight".Translate() },
                { ("Expedition33_Dualiste", "ObscurBlast"),
                  "Expedition33_DualistDialogueDark".Translate() },
                // François
                { ("Expedition33_Francois", "LethalStrike"),
                  "Expedition33_FrancoisDialogueLethal".Translate() },
                { ("Expedition33_Francois", "FearAura"),
                  "Expedition33_FrancoisDialogueFear".Translate() }
            };

            if (dialogues.TryGetValue((boss.kindDef.defName, abilityName), out string dialogue))
            {
                Messages.Message(dialogue, MessageTypeDefOf.ThreatBig);
            }
        }
    }
}
