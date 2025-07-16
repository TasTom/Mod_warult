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
    // Incident du "Gommage" - disparition myst�rieuse - VERSION CORRIG�E
    public class IncidentWorker_Gommage : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || gameComp.currentPaintedAge == -1) return false;

            // Trouve tous les colons de l'�ge peint
            var cursedColonists = map.mapPawns.FreeColonists
                .Where(p => p.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge) // >= au lieu de ==
                .ToList();

            // Filtre les colons prot�g�s
            var unprotectedColonists = cursedColonists
                .Where(p => !IsProtectedFromGommage(p))
                .ToList();

            // Applique le traumatisme aux t�moins AVANT le Gommage
            ApplyGommageTrauma(map, cursedColonists.Count);

            if (!unprotectedColonists.Any())
            {
                // Tous les colons sont prot�g�s !
                HandleCompleteProtection(gameComp.currentPaintedAge, cursedColonists.Count);
                return true;
            }

            // Consomme les charges des boucliers utilis�s
            ConsumeProtectionCharges(cursedColonists.Except(unprotectedColonists));

            // Gomme seulement les colons non prot�g�s
            foreach (var victim in unprotectedColonists)
            {
                GommageEffects.CreateGommageEffect(victim.Position, map);
                victim.Destroy();
            }

            // Message adapt� selon le nombre de survivants
            HandlePartialGommage(gameComp.currentPaintedAge, unprotectedColonists.Count,
                cursedColonists.Count - unprotectedColonists.Count);

            return true;
        }

        private bool IsProtectedFromGommage(Pawn pawn)
        {
            // V�rifie la protection par bouclier personnel
            var shieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
            );

            // V�rifie la protection par champ
            var fieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FieldProtection")
            );

            // V�rifie la r�sistance naturelle
            var resistance = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageResistance")
            );

            return shieldProtection != null || fieldProtection != null || resistance != null;
        }

        private void ApplyGommageTrauma(Map map, int victimsCount)
        {
            // Applique le traumatisme aux t�moins du Gommage
            var witnesses = map.mapPawns.FreeColonists
                .Where(p => p.Spawned && !p.Dead)
                .ToList();

            foreach (var witness in witnesses)
            {
                // Plus il y a de victimes, plus le traumatisme est s�v�re
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

                // Chance d'inspiration artistique pour certains colons cr�atifs
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

                    // Si le bouclier est �puis�, applique l'effet d'�puisement
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
                    $"{pawn.Name.ToStringShort} est inspir�(e) par la lutte contre le Gommage !",
                    MessageTypeDefOf.PositiveEvent
                );
            }
        }

        private void HandleCompleteProtection(int age, int protectedCount)
        {
            Find.LetterStack.ReceiveLetter(
                "GOMMAGE BLOQU� !",
                $"Le Gommage a tent� de frapper vos {protectedCount} colon(s) de {age} ans, " +
                $"mais ils �taient tous prot�g�s par vos technologies anti-Gommage !\n\n" +
                $"La Paintress rugit de frustration...\n\n" +
                $"Vos recherches portent leurs fruits !",
                LetterDefOf.PositiveEvent
            );
        }

        private void HandlePartialGommage(int age, int gommageCount, int protectedCount)
        {
            string letterText = $"LE GOMMAGE A PARTIELLEMENT FRAPP� !\n\n" +
                                $"{gommageCount} colon(s) de {age} ans ont �t� gomm�s, " +
                                $"mais {protectedCount} ont surv�cu gr�ce � vos protections !\n\n";

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


    // Incident de mission d'exp�dition - VERSION CORRIG�E
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
                "Reconnaissance en territoire gomm�",
                "R�cup�ration d'artefacts perdus",
                "Sauvetage d'exp�ditionnaires disparus",
                "Investigation d'anomalies temporelles",
                "Surveillance des signes de la Panteresse",
                "Collecte de t�moignages sur le Gommage"
            };

            string mission = missionTypes[Rand.Range(0, missionTypes.Length)];

            string letterText = $"Un message radio crypt� de l'Exp�dition 33 arrive :\n\n" +
                                $"\"Mission assign�e : {mission}\"\n\n" +
                                $"Les rapports indiquent une activit� suspecte dans la r�gion. " +
                                $"Cette mission pourrait r�v�ler des informations cruciales sur Le Gommage, " +
                                $"mais elle comporte �galement des risques consid�rables.\n\n" +
                                $"Pr�parez vos exp�ditionnaires et vos �quipements. La v�rit� sur la Panteresse vous attend.";

            Find.LetterStack.ReceiveLetter(
                "Mission d'Exp�dition",
                letterText,
                LetterDefOf.NeutralEvent
            );

            // CORRIG� - Boost de moral sans TraitDefOf.Brave
            foreach (Pawn pawn in map.mapPawns.FreeColonists)
            {
                // M�thode s�curis�e avec DefDatabase
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

    // Incident d'apparition de La Panteresse - VERSION CORRIG�E
    public class IncidentWorker_PainterSighting : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) // CORRIG� - protected
        {
            Map map = (Map)parms.target;
            return map != null && map.mapPawns.FreeColonistsCount > 0;
        }

        protected override bool TryExecuteWorker(IncidentParms parms) // CORRIG� - protected
        {
            Map map = (Map)parms.target;
            if (map == null) return false;

            var colonists = map.mapPawns.FreeColonists.ToList();
            if (!colonists.Any()) return false;

            Pawn witness = colonists.RandomElement();

            // Spawn r�el de la Panteresse (si PawnKindDef existe)
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
                                        $"Elle semble hostile et extrèmement dangereuse. Ses yeux brillent d'une lueur " +
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
            string narrativeText = $"{witness.Name.ToStringShort} rapporte avoir aper�u une silhouette �trange " +
                                   $"dans la brume - une figure f�minine imposante tenant ce qui ressemble � un pinceau g�ant.\n\n" +
                                   $"\"C'�tait Elle... La Painteress,\" murmure {witness.Name.ToStringShort} en tremblant. " +
                                   $"\"Elle observait notre colonie, comme si elle choisissait sa prochaine toile. " +
                                   $"Ses yeux... ils brillaient d'une lumi�re qui n'appartient pas � ce monde...\"\n\n" +
                                   $"Cette apparition ne pr�sage rien de bon. Un Gommage pourrait survenir bient�t.";

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

    // Test Event - CORRIG�
    public class IncidentWorker_TestEvent : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms) // CORRIG� - protected
        {
            Log.Message("=== TEST EXP�DITION 33 FONCTIONNE ! ===");

            Find.LetterStack.ReceiveLetter(
                "Test Exp�dition 33",
                "Si tu vois ce message, tes �v�nements fonctionnent parfaitement ! Le syst�me d'incidents de ton mod Exp�dition 33 est op�rationnel.",
                LetterDefOf.PositiveEvent
            );

            return true;
        }
    }

    // Pens�e li�e � la peur du Gommage
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



  
}