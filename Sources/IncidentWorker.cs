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
    // Incident du "Gommage" - disparition mystérieuse - VERSION CORRIGÉE
    public class IncidentWorker_Gommage : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || gameComp.currentPaintedAge == -1) return false;

            // Trouve tous les colons de l'âge peint
            var cursedColonists = map.mapPawns.FreeColonists
                .Where(p => p.ageTracker.AgeBiologicalYears == gameComp.currentPaintedAge)
                .ToList();

            // Filtre les colons protégés
            var unprotectedColonists = cursedColonists
                .Where(p => !IsProtectedFromGommage(p))
                .ToList();

            // Applique le traumatisme aux témoins AVANT le Gommage
            ApplyGommageTrauma(map, cursedColonists.Count);

            if (!unprotectedColonists.Any())
            {
                // Tous les colons sont protégés !
                HandleCompleteProtection(gameComp.currentPaintedAge, cursedColonists.Count);
                return true;
            }

            // Consomme les charges des boucliers utilisés
            ConsumeProtectionCharges(cursedColonists.Except(unprotectedColonists));

            // Gomme seulement les colons non protégés
            foreach (var victim in unprotectedColonists)
            {
                GommageEffects.CreateGommageEffect(victim.Position, map);
                victim.Destroy();
            }

            // Message adapté selon le nombre de survivants
            HandlePartialGommage(gameComp.currentPaintedAge, unprotectedColonists.Count,
                cursedColonists.Count - unprotectedColonists.Count);

            return true;
        }

        private bool IsProtectedFromGommage(Pawn pawn)
        {
            // Vérifie la protection par bouclier personnel
            var shieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
            );

            // Vérifie la protection par champ
            var fieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FieldProtection")
            );

            // Vérifie la résistance naturelle
            var resistance = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageResistance")
            );

            return shieldProtection != null || fieldProtection != null || resistance != null;
        }

        private void ApplyGommageTrauma(Map map, int victimsCount)
        {
            // Applique le traumatisme aux témoins du Gommage
            var witnesses = map.mapPawns.FreeColonists
                .Where(p => p.Spawned && !p.Dead)
                .ToList();

            foreach (var witness in witnesses)
            {
                // Plus il y a de victimes, plus le traumatisme est sévère
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

                // Chance d'inspiration artistique pour certains colons créatifs
                if (witness.skills.GetSkill(SkillDefOf.Artistic).Level >= 8 && Rand.Chance(0.2f))
                {
                    ApplyArtisticInspiration(witness);
                }
            }
        }

        private void ConsumeProtectionCharges(IEnumerable<Pawn> protectedPawns)
        {
            foreach (var pawn in protectedPawns)
            {
                // Consomme une charge du bouclier s'il en a un
                var shield = pawn.apparel?.WornApparel?.FirstOrDefault(a =>
                    a.def.defName == "Expedition33_AntiGommageShield") as Apparel_AntiGommageShield;

                if (shield != null)
                {
                    shield.ConsumeCharge();

                    // Si le bouclier est épuisé, applique l'effet d'épuisement
                    if (!shield.isActive)
                    {
                        var exhaustion = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_ShieldExhaustion"),
                            pawn
                        );

                        if (exhaustion != null)
                        {
                            pawn.health.AddHediff(exhaustion);
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
                    $"{pawn.Name.ToStringShort} est inspiré(e) par la lutte contre le Gommage !",
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void HandleCompleteProtection(int age, int protectedCount)
        {
            Find.LetterStack.ReceiveLetter(
                "GOMMAGE BLOQUÉ !",
                $"Le Gommage a tenté de frapper vos {protectedCount} colon(s) de {age} ans, " +
                $"mais ils étaient tous protégés par vos technologies anti-Gommage !\n\n" +
                $"La Paintress rugit de frustration...\n\n" +
                $"Vos recherches portent leurs fruits !",
                LetterDefOf.PositiveEvent
            );
        }

        private void HandlePartialGommage(int age, int gommageCount, int protectedCount)
        {
            string letterText = $"LE GOMMAGE A PARTIELLEMENT FRAPPÉ !\n\n" +
                                $"{gommageCount} colon(s) de {age} ans ont été gommés, " +
                                $"mais {protectedCount} ont survécu grâce à vos protections !\n\n";

            if (protectedCount > 0)
            {
                letterText += $"Vos recherches anti-Gommage sauvent des vies !";
            }

            Find.LetterStack.ReceiveLetter(
                $"GOMMAGE PARTIEL : {age} ANS",
                letterText,
                protectedCount > 0 ? LetterDefOf.NeutralEvent : LetterDefOf.ThreatBig
            );
        }
    }


    // Incident de mission d'expédition - VERSION CORRIGÉE
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

            string[] missionTypes =
            {
                "Reconnaissance en territoire gommé",
                "Récupération d'artefacts perdus",
                "Sauvetage d'expéditionnaires disparus",
                "Investigation d'anomalies temporelles",
                "Surveillance des signes de la Panteresse",
                "Collecte de témoignages sur le Gommage"
            };

            string mission = missionTypes[Rand.Range(0, missionTypes.Length)];

            string letterText = $"Un message radio crypté de l'Expédition 33 arrive :\n\n" +
                                $"\"Mission assignée : {mission}\"\n\n" +
                                $"Les rapports indiquent une activité suspecte dans la région. " +
                                $"Cette mission pourrait révéler des informations cruciales sur Le Gommage, " +
                                $"mais elle comporte également des risques considérables.\n\n" +
                                $"Préparez vos expéditionnaires et vos équipements. La vérité sur la Panteresse vous attend.";

            Find.LetterStack.ReceiveLetter(
                "Mission d'Expédition",
                letterText,
                LetterDefOf.NeutralEvent
            );

            // CORRIGÉ - Boost de moral sans TraitDefOf.Brave
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                // Méthode sécurisée avec DefDatabase
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

            var gameComp = Current.Game.GetComponent<GameComponent_ExpeditionStats>();
            if (gameComp != null)
            {
                gameComp.RecordMissionCompleted();
            }

            return true;
        }
    }

    // Incident d'apparition de La Panteresse - VERSION CORRIGÉE
    public class IncidentWorker_PainterSighting : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) // CORRIGÉ - protected
        {
            Map map = (Map)parms.target;
            return map != null && map.mapPawns.FreeColonistsCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) // CORRIGÉ - protected
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var colonists = map.mapPawns.FreeColonists.ToList();
            if (!colonists.Any()) return false;

            Pawn witness = colonists.RandomElement();

            // Spawn réel de la Panteresse (si PawnKindDef existe)
            PawnKindDef paintressKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_PaintressBoss");

            if (paintressKind != null)
            {
                IntVec3 spawnSpot;
                if (CellFinder.TryFindRandomEdgeCellWith(
                        (IntVec3 c) => map.reachability.CanReachColony(c),
                        map,
                        CellFinder.EdgeRoadChance_Neutral,
                        out spawnSpot))
                {
                    Faction faction =
                        Find.FactionManager.FirstFactionOfDef(
                            DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33"));
                    Pawn paintress = PawnGenerator.GeneratePawn(paintressKind, faction);

                    GenSpawn.Spawn(paintress, spawnSpot, map);
                    paintress.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);

                    string letterText = $"La terrifiante Painteress est apparue près de votre colonie !\n\n" +
                                        $"Cette entité mystérieuse manipule la réalité avec son pinceau magique. " +
                                        $"Elle semble hostile et extrêmement dangereuse. Ses yeux brillent d'une lueur " +
                                        $"artistique malveillante alors qu'elle évalue votre colonie comme sa prochaine toile.\n\n" +
                                        $"Préparez vos défenses ! Un Gommage massif pourrait suivre...";

                    Find.LetterStack.ReceiveLetter(
                        "Apparition de la Painteress",
                        letterText,
                        LetterDefOf.ThreatBig,
                        paintress
                    );

                    return true;
                }
            }

            // Fallback : Apparition narrative
            string narrativeText = $"{witness.Name.ToStringShort} rapporte avoir aperçu une silhouette étrange " +
                                   $"dans la brume - une figure féminine imposante tenant ce qui ressemble à un pinceau géant.\n\n" +
                                   $"\"C'était Elle... La Painteress,\" murmure {witness.Name.ToStringShort} en tremblant. " +
                                   $"\"Elle observait notre colonie, comme si elle choisissait sa prochaine toile. " +
                                   $"Ses yeux... ils brillaient d'une lumière qui n'appartient pas à ce monde...\"\n\n" +
                                   $"Cette apparition ne présage rien de bon. Un Gommage pourrait survenir bientôt.";

            Find.LetterStack.ReceiveLetter(
                "Apparition de la Painteress",
                narrativeText,
                LetterDefOf.ThreatSmall,
                witness
            );

            witness.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.WitnessedDeathAlly);

            return true;
        }
    }

    // Test Event - CORRIGÉ
    public class IncidentWorker_TestEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms) // CORRIGÉ - protected
        {
            Log.Message("=== TEST EXPÉDITION 33 FONCTIONNE ! ===");

            Find.LetterStack.ReceiveLetter(
                "Test Expédition 33",
                "Si tu vois ce message, tes événements fonctionnent parfaitement ! Le système d'incidents de ton mod Expédition 33 est opérationnel.",
                LetterDefOf.PositiveEvent
            );

            return true;
        }
    }

    // Pensée liée à la peur du Gommage
    public class ThoughtWorker_GommageAnxiety : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            int age = p.ageTracker.AgeBiologicalYears;

            if (age >= 30 && age <= 50)
            {
                if (age % 5 == 0)
                {
                    return ThoughtState.ActiveAtStage(2);
                }

                return ThoughtState.ActiveAtStage(1);
            }

            return ThoughtState.Inactive;
        }
    }



    public class IncidentWorker_SpawnPaintressOnSite : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.siteRevealed) return false;

            // CORRECTION - Utilise le bon nom
            var existingPaintress = map.mapPawns.AllPawns
                .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

            return existingPaintress == null;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            // CORRECTION - Utilise le bon nom qui correspond à ton XML
            PawnKindDef paintressKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_PaintressMonster");
            if (paintressKind == null)
            {
                Log.Error("PawnKindDef Expedition33_PaintressMonster non trouvé !");
                return false;
            }

            Log.Message($"PawnKindDef trouvé : {paintressKind.defName}");

            // Position centrale
            IntVec3 spawnPos = map.Center;
            if (!CellFinder.TryFindRandomCellNear(spawnPos, map, 10,
                (IntVec3 c) => c.Standable(map) && c.Walkable(map), out spawnPos))
            {
                Log.Warning("Impossible de trouver une position valide pour la Paintress");
                return false;
            }

            // Génère et spawn la Paintress
            Faction expedition33 = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33")
            );

            Pawn paintress = PawnGenerator.GeneratePawn(paintressKind, expedition33);
            if (paintress == null)
            {
                Log.Error("Impossible de générer la Paintress !");
                return false;
            }

            GenSpawn.Spawn(paintress, spawnPos, map);
            paintress.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);

            Log.Message($"Paintress spawnée avec succès à {spawnPos}");

            string letterText = "Vous avez atteint le Monolithe de la Paintress !\n\n" +
                               "La terrifiante entité artistique se dresse devant vous, " +
                               "pinceau mystique en main. Comme dans Clair Obscur: Expedition 33, " +
                               "elle garde jalousement son monolithe au sommet.\n\n" +
                               "C'est votre seule chance de briser le cycle du Gommage !";

            Find.LetterStack.ReceiveLetter(
                "CONFRONTATION FINALE !",
                letterText,
                LetterDefOf.ThreatBig,
                paintress
            );

            return true;
        }
    }

}