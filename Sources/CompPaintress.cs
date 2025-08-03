using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.Planet;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace Mod_warult
{
    public class ThinkNode_BossAbilities : ThinkNode
    {
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            // Vérifier si c'est un boss avec les capacités spéciales
            var comp = pawn.GetComp<CompBossAbilities>();
            if (comp != null)
            {
                // Le boss a des capacités spéciales, laisser l'IA normale gérer
                return ThinkResult.NoJob;
            }
            return ThinkResult.NoJob;
        }
    }

    public class CompPaintressDeath : ThingComp
    {
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            if (mode == DestroyMode.KillFinalize && parent is Pawn pawn)
            {
                if (pawn.kindDef?.defName == "Expedition33_Paintress")
                {
                    // La Paintress a été tuée !
                    OnPaintressKilled();
                }
            }
            base.PostDestroy(mode, previousMap);
        }

        private void OnPaintressKilled()
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp != null)
            {
                gameComp.OnPaintressKilled();
            }

            // Message de victoire épique
            string letterText = "Expedition33_PaintressDefeatedLetter".Translate();
            
            Find.LetterStack.ReceiveLetter(
                "Expedition33_VictoryTitle".Translate(),
                letterText,
                LetterDefOf.PositiveEvent
            );
            Log.Message("Expedition33_PaintressDefeatedLog".Translate());
        }
    }

    public class CompProperties_PaintressDeath : CompProperties
    {
        public CompProperties_PaintressDeath()
        {
            this.compClass = typeof(CompPaintressDeath);
        }
    }

    public class CompBossAbilities : ThingComp
    {
        private int tickCounter = 0;
        private int corruptionCounter = 0;
        private bool hasUsedSummon = false;
        private bool hasUsedTeleport = false;

        public override void CompTick()
        {
            base.CompTick();
            Pawn pawn = parent as Pawn;
            if (pawn?.Spawned != true || pawn.Dead) return;

            tickCounter++;

            // PHASE 1: Corruption Aura (constante)
            if (tickCounter % (3 * 60) == 0) // Toutes les 3 secondes
            {
                CorruptionWave(pawn);
            }

            // PHASE 2: Summon à 70% vie
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
            if (healthPercent < 0.7f && !hasUsedSummon)
            {
                SummonNevronArmy(pawn);
                hasUsedSummon = true;
            }

            // PHASE 3: Rage Mode à 40% vie
            if (healthPercent < 0.4f && tickCounter % (2 * 60) == 0)
            {
                RageMode(pawn);
            }

            // PHASE 4: Téléportation désespérée à 15% vie
            if (healthPercent < 0.15f && !hasUsedTeleport && tickCounter % (8 * 60) == 0)
            {
                EmergencyTeleport(pawn);
                hasUsedTeleport = false; // Peut refaire
            }
        }

        private void CorruptionWave(Pawn boss)
        {
            corruptionCounter++;
            // Trouve toutes les créatures dans un rayon de 12 cases
            var nearbyPawns = boss.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(boss.Position) <= 12f && p != boss && p.Faction != boss.Faction);

            foreach (Pawn target in nearbyPawns)
            {
                // 15% chance de corruption par wave
                if (Rand.Chance(0.15f))
                {
                    // Essaie d'appliquer un mental break
                    if (Rand.Chance(0.5f))
                    {
                        target.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk);
                    }

                    // Dégâts psychiques
                    target.TakeDamage(new DamageInfo(DamageDefOf.Psychic, Rand.Range(2, 8)));
                }
            }

            // Effet visuel toutes les 5 waves
            if (corruptionCounter % 5 == 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    IntVec3 pos = boss.Position.RandomAdjacentCell8Way();
                    if (pos.InBounds(boss.Map))
                    {
                        FleckMaker.ThrowDustPuffThick(pos.ToVector3(), boss.Map, 2.0f,
                            new Color(0.8f, 0.2f, 0.9f, 0.5f));
                    }
                }
            }
        }

        private void SummonNevronArmy(Pawn boss)
        {
            Messages.Message("Expedition33_PaintressSummons".Translate(),
                MessageTypeDefOf.ThreatBig);

            // Types de Nevrons à invoquer
            string[] nevronTypes = {
                "Nevron_Basic",
                "Amphorien",
                "Pitank"
            };

            int totalSummons = Rand.RangeInclusive(8, 15);
            for (int i = 0; i < totalSummons; i++)
            {
                string randomType = nevronTypes[Rand.Range(0, nevronTypes.Length)];
                PawnKindDef nevronKind = PawnKindDef.Named(randomType);
                if (nevronKind != null)
                {
                    // Trouve position de spawn
                    IntVec3 spawnPos = boss.Position.RandomAdjacentCell8Way();
                    for (int attempt = 0; attempt < 5 && (!spawnPos.InBounds(boss.Map) || !spawnPos.Standable(boss.Map)); attempt++)
                    {
                        spawnPos = boss.Position.RandomAdjacentCell8Way();
                    }

                    if (spawnPos.InBounds(boss.Map) && spawnPos.Standable(boss.Map))
                    {
                        Pawn nevron = PawnGenerator.GeneratePawn(nevronKind, boss.Faction);
                        GenSpawn.Spawn(nevron, spawnPos, boss.Map);
                        // Force l'agressivité
                        nevron.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                        // Effet de spawn
                        FleckMaker.ThrowDustPuffThick(spawnPos.ToVector3(), boss.Map, 1.5f, Color.red);
                    }
                }
            }
        }

        private void RageMode(Pawn boss)
        {
            // Augmente temporairement les stats
            if (boss.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PsychicShock) == null)
            {
                // Ajoute un boost de rage temporaire
                Hediff rageBoost = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, boss);
                rageBoost.Severity = 0.1f; // Léger pour éviter les débuffs
                boss.health.AddHediff(rageBoost);
                
                Messages.Message("Expedition33_PaintressRage".Translate(),
                    MessageTypeDefOf.ThreatSmall);
            }
        }

        private void EmergencyTeleport(Pawn boss)
        {
            // Trouve une position éloignée des ennemis
            List<IntVec3> candidates = new List<IntVec3>();
            for (int i = 0; i < 50; i++)
            {
                IntVec3 candidate = boss.Map.AllCells.RandomElement();
                if (candidate.Standable(boss.Map) && candidate.DistanceTo(boss.Position) > 20f)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count > 0)
            {
                IntVec3 teleportPos = candidates.RandomElement();
                // Effet de téléportation
                FleckMaker.ThrowDustPuffThick(boss.Position.ToVector3(), boss.Map, 3.0f, new Color(0.5f, 0f, 0.5f, 1f));
                boss.Position = teleportPos;
                boss.Notify_Teleported();
                FleckMaker.ThrowDustPuffThick(teleportPos.ToVector3(), boss.Map, 3.0f, new Color(0.5f, 0f, 0.5f, 1f));
                
                Messages.Message("Expedition33_PaintressTeleport".Translate(),
                    MessageTypeDefOf.NeutralEvent);
            }
        }
    }

    public class CompProperties_BossAbilities : CompProperties
    {
        public CompProperties_BossAbilities()
        {
            compClass = typeof(CompBossAbilities);
        }
    }
}
