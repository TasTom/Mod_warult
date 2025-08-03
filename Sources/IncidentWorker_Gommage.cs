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
    public class IncidentWorker_Gommage : IncidentWorker
    {
        
          // ✅ AJOUT : Méthode CanFireNowSub dans la bonne classe
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        Log.Message("Expedition33_Gommage: CanFireNowSub called");
        
        Map map = (Map)parms.target;
        if (map == null)
        {
            Log.Message("Expedition33_Gommage: CanFireNowSub - No map");
            return false;
        }

        var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
        if (gameComp == null)
        {
            Log.Message("Expedition33_Gommage: CanFireNowSub - No GameComponent");
            return false;
        }

        if (!gameComp.paintressAlive)
        {
            Log.Message("Expedition33_Gommage: CanFireNowSub - Paintress not alive");
            return false;
        }

        if (gameComp.currentPaintedAge == -1)
        {
            Log.Message("Expedition33_Gommage: CanFireNowSub - No painted age");
            return false;
        }

        // Vérifier qu'il y a au moins un colon sur la carte
        bool hasColonists = map.mapPawns.FreeColonists.Any();
        Log.Message($"Expedition33_Gommage: CanFireNowSub - Has colonists: {hasColonists}");

        return hasColonists;
    }
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null)
            {
                Log.Error("Expedition33_Gommage: No map found");
                return false;
            }

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null)
            {
                Log.Error("Expedition33_Gommage: GameComponent not found");
                return false;
            }
            if (!gameComp.paintressAlive)
            {
                Log.Message("Expedition33_Gommage: Paintress not alive");
                return false;
            }
            if (gameComp.currentPaintedAge == -1)
            {
                Log.Error("Expedition33_Gommage: No painted age set");
                return false;
            }

            Log.Message($"Expedition33_Gommage: Starting with painted age {gameComp.currentPaintedAge}");

            // Trouve tous les colons de l'âge peint
            var cursedColonists = map.mapPawns.FreeColonists
                .Where(p => p.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge)
                .ToList();

            Log.Message($"Expedition33_Gommage: Found {cursedColonists.Count} colonists of age {gameComp.currentPaintedAge}+");


            // Filtre les colons protégés
            var unprotectedColonists = cursedColonists
                .Where(p => !IsProtectedFromGommage(p))
                .ToList();

            Log.Message($"Expedition33_Gommage: {unprotectedColonists.Count} unprotected colonists will be affected");

            int totalThreatenedCount = cursedColonists.Count;
            int totalVictims = unprotectedColonists.Count;
            int protectedCount = totalThreatenedCount - totalVictims;

            // Enregistrer dans l'historique
            gameComp.RecordGommageEvent(
                gameComp.currentPaintedAge,
                totalVictims,
                protectedCount,
                GenTicks.TicksGame
            );

   
            if (unprotectedColonists.Count > 0)
            {
                // Gommage avec victimes : utilise le nombre de victimes
                ApplyGommageTrauma(map, unprotectedColonists.Count);
            }
            else
            {
                // Gommage sans victimes : passe 0 pour déclencher la logique spéciale
                ApplyGommageTrauma(map, 0);
            }
            

            // Consommer les charges des protections
            if (cursedColonists.Any() && unprotectedColonists.Count < cursedColonists.Count)
            {
                var actuallyProtectedPawns = cursedColonists.Except(unprotectedColonists);
                ConsumeProtectionCharges(actuallyProtectedPawns);
            }

            if (!unprotectedColonists.Any())
            {
                HandleCompleteProtection(gameComp.currentPaintedAge, cursedColonists.Count);
                return true;
            }

            // Gomme les colons non protégés
            foreach (var victim in unprotectedColonists)
            {
                GommageEffects.CreateGommageEffect(victim.Position, map);
                victim.Destroy();
            }

            HandlePartialGommage(gameComp.currentPaintedAge, unprotectedColonists.Count,
                cursedColonists.Count - unprotectedColonists.Count);

            return true;
        }

        private bool IsProtectedFromGommage(Pawn pawn)
        {
            // Protection par hediff du bouclier
            var shieldHediffProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
            );

            // Protection par hediff du champ
            var fieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_AntiGommageProtection")
            );

            // Vérification directe du bouclier équipé
            bool hasActiveShield = false;
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    if (apparel is Apparel_AntiGommageShield shield)
                    {
                        hasActiveShield = shield.isActive && shield.protectionCharges > 0;
                        if (hasActiveShield) break;
                    }
                }
            }

            // Vérification des générateurs de champ actifs
            bool inProtectionField = false;
            if (pawn.Map != null)
            {
                var generators = pawn.Map.listerBuildings.allBuildingsColonist
                    .OfType<Building_AntiGommageField>()
                    .Where(g => g.IsActive);

                foreach (var generator in generators)
                {
                    if (pawn.Position.DistanceTo(generator.Position) <= 15)
                    {
                        inProtectionField = true;
                        break;
                    }
                }
            }

            return shieldHediffProtection != null || fieldProtection != null ||
                   hasActiveShield || inProtectionField;
        }

        private void ApplyGommageTrauma(Map map, int victimsCount)
        {
            var witnesses = map.mapPawns.FreeColonists
                .Where(p => p.Spawned && !p.Dead)
                .ToList();

            foreach (var witness in witnesses)
            {
                if (victimsCount == 0)
                {
                    // ✅ NOUVEAU : Cas où personne n'est mort
                    ApplyNoDeathTrauma(witness);
                }
                else
                {
                    // ✅ EXISTANT : Cas avec des victimes
                    float traumaSeverity = 0.3f + (victimsCount * 0.1f);
                    var trauma = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageWitness"),
                        witness
                    );
                    if (trauma != null)
                    {
                        trauma.Severity = traumaSeverity;
                        witness.health.AddHediff(trauma);
                    }
                }

                // Chance d'inspiration artistique pour certains colons créatifs
                if (witness.skills.GetSkill(SkillDefOf.Artistic).Level >= 8 && Rand.Chance(0.2f))
                {
                    ApplyArtisticInspiration(witness);
                }
            }
        }


        // ✅ NOUVELLE MÉTHODE : Gestion du cas "aucune mort"
        private void ApplyNoDeathTrauma(Pawn witness)
        {
            // Anxiété du décompte (légère, 1 jour)
            var anxietyHediff = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageCountdownAnxiety"),
                witness
            );
            if (anxietyHediff != null)
            {
                anxietyHediff.Severity = 1.0f;
                witness.health.AddHediff(anxietyHediff);
            }

            // Motivation anti-Paintress (positive, 3 jours)
            var motivationHediff = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_AntiPaintressMotivation"),
                witness
            );
            if (motivationHediff != null)
            {
                motivationHediff.Severity = 1.0f;
                witness.health.AddHediff(motivationHediff);
            }

            Log.Message($"Expedition33: Applied no-death trauma to {witness.Name.ToStringShort}");
        }



        private void ConsumeProtectionCharges(IEnumerable<Pawn> protectedPawns)
        {
            foreach (var pawn in protectedPawns)
            {
                if (pawn.apparel?.WornApparel != null)
                {
                    foreach (var apparel in pawn.apparel.WornApparel)
                    {
                        if (apparel is Apparel_AntiGommageShield shield)
                        {
                            if (shield.isActive && shield.protectionCharges > 0)
                            {
                                shield.ConsumeCharge();
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void ApplyArtisticInspiration(Pawn pawn)
        {
            var inspiration = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_ArtisticInspiration"),
                pawn
            );
            if (inspiration != null)
            {
                pawn.health.AddHediff(inspiration);
                Messages.Message(
                    "Expedition33_ArtisticInspirationMessage".Translate(pawn.Name.ToStringShort),
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void HandleCompleteProtection(int age, int protectedCount)
        {
            Find.LetterStack.ReceiveLetter(
                "Expedition33_GommageBlockedTitle".Translate(),
                "Expedition33_GommageBlockedText".Translate(protectedCount, age),
                LetterDefOf.PositiveEvent
            );
        }

        private void HandlePartialGommage(int age, int gommageCount, int protectedCount)
        {
            string letterText = "Expedition33_PartialGommageText".Translate(gommageCount, age, protectedCount);

            if (protectedCount > 0)
            {
                letterText += "\n\n" + "Expedition33_ProtectionsWorking".Translate();
            }

            Find.LetterStack.ReceiveLetter(
                "Expedition33_PartialGommageTitle".Translate(age),
                letterText,
                protectedCount > 0 ? LetterDefOf.NeutralEvent : LetterDefOf.ThreatBig
            );
        }
    }

    public class IncidentWorker_ExpeditionMission : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map != null && map.mapPawns.FreeColonists.Any();
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            string[] missionKeys = {
                "Expedition33_MissionRecon",
                "Expedition33_MissionArtifact",
                "Expedition33_MissionRescue",
                "Expedition33_MissionInvestigation",
                "Expedition33_MissionSurveillance",
                "Expedition33_MissionTestimonies"
            };

            string missionKey = missionKeys[Rand.Range(0, missionKeys.Length)];
            string mission = missionKey.Translate();

            string letterText = "Expedition33_MissionLetterText".Translate(mission);

            Find.LetterStack.ReceiveLetter(
                "Expedition33_MissionTitle".Translate(),
                letterText,
                LetterDefOf.NeutralEvent
            );

            // Boost de moral pour certains colons
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                TraitDef sanguineTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Sanguine");
                TraitDef brawlerTrait = DefDatabase<TraitDef>.GetNamedSilentFail("Brawler");

                bool hasPositiveTrait = false;
                if (brawlerTrait != null && pawn.story?.traits?.HasTrait(brawlerTrait) == true)
                    hasPositiveTrait = true;
                if (sanguineTrait != null && pawn.story?.traits?.HasTrait(sanguineTrait) == true)
                    hasPositiveTrait = true;

                if (hasPositiveTrait)
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.NewColonyOptimism);
                }
            }

            return true;
        }
    }
     public class IncidentWorker_PainterSighting : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Find.LetterStack.ReceiveLetter(
                "Expedition33_PaintressApproaches".Translate(),
                "Expedition33_GommageWarning".Translate(),
                LetterDefOf.ThreatBig
            );
            return true;
        }
    }

    public class IncidentWorker_TestEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Messages.Message("Expedition33_TestEvent".Translate(), MessageTypeDefOf.PositiveEvent);
            return true;
        }
    }
}
