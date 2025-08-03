using RimWorld;
using Verse;
using System.Collections.Generic;
using System;
using System.Linq;
using LudeonTK;
using HarmonyLib;

namespace Mod_warult
{
    public class IncidentWorker_NevronNestDiscovery : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Trouve une position valide
            IntVec3 pos = CellFinderLoose.RandomCellWith(
                c => c.Standable(map) && !c.Fogged(map), map, 1000);

            if (!pos.IsValid)
            {
                return false;
            }

            // Utilise des ThingDefs vanilla ou tes ressources existantes
            ThingDef nestDef = ThingDefOf.Hive; // Utilise la ruche vanilla comme base
            ThingDef chromaDef = ThingDef.Named("Expedition33_ChromaRaw"); // Ton chroma

            if (chromaDef == null)
            {
                Log.Error("Expedition33_ChromaRawNotFound".Translate());
                return false;
            }

            // Spawn le nid (ruche vanilla)
            Thing nest = ThingMaker.MakeThing(nestDef);
            GenSpawn.Spawn(nest, pos, map);

            // Spawn du chroma brut autour
            for (int i = 0; i < Rand.RangeInclusive(3, 6); i++)
            {
                Thing chroma = ThingMaker.MakeThing(chromaDef);
                chroma.stackCount = Rand.RangeInclusive(2, 8);
                IntVec3 dropPos = pos.RandomAdjacentCell8Way();

                if (dropPos.InBounds(map))
                {
                    GenPlace.TryPlaceThing(chroma, dropPos, map, ThingPlaceMode.Near);
                }
            }

            // Utilise les paramètres de l'incident
            Find.LetterStack.ReceiveLetter(
                def.letterLabel ?? "Expedition33_NevronNestDiscoveredTitle".Translate(),
                def.letterText ?? "Expedition33_NevronNestDiscoveredDesc".Translate(),
                def.letterDef ?? LetterDefOf.NeutralEvent,
                new TargetInfo(pos, map)
            );

            return true;
        }
    }

    public class IncidentWorker_ChromaticArtifactCrash : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Trouve une position de crash
            if (!DropCellFinder.TryFindDropSpotNear(map.Center, map, out IntVec3 intVec, true, true))
            {
                return false;
            }

            // Utilise tes ressources existantes
            ThingDef chromaDef = ThingDef.Named("Expedition33_ChromaRaw");
            if (chromaDef == null)
            {
                Log.Error("Expedition33_ChromaRawNotFound".Translate());
                return false;
            }

            // Crée la cargaison
            List<Thing> things = new List<Thing>();

            // Chroma brut (principal)
            Thing artifact = ThingMaker.MakeThing(chromaDef);
            artifact.stackCount = Rand.RangeInclusive(8, 20);
            things.Add(artifact);

            // Bonus aléatoires
            if (Rand.Chance(0.4f))
            {
                Thing gold = ThingMaker.MakeThing(ThingDefOf.Gold);
                gold.stackCount = Rand.RangeInclusive(5, 15);
                things.Add(gold);
            }

            if (Rand.Chance(0.3f))
            {
                Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
                steel.stackCount = Rand.RangeInclusive(15, 40);
                things.Add(steel);
            }

            // Drop pod avec explosion
            DropPodUtility.DropThingsNear(intVec, map, things, 110, false, true, true);

            // Utilise les paramètres de l'incident
            Find.LetterStack.ReceiveLetter(
                def.letterLabel ?? "Expedition33_ChromaticArtifactTitle".Translate(),
                def.letterText ?? "Expedition33_ChromaticArtifactDesc".Translate(),
                def.letterDef ?? LetterDefOf.PositiveEvent,
                new TargetInfo(intVec, map)
            );

            return true;
        }
    }

    public class IncidentWorker_NevronMigration : IncidentWorker
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // Vérifications et variété
            List<PawnKindDef> nevronTypes = new List<PawnKindDef>();
            PawnKindDef nevronBasic = PawnKindDef.Named("Nevron_Basic");
            PawnKindDef amphorien = PawnKindDef.Named("Amphorien");

            if (nevronBasic != null) nevronTypes.Add(nevronBasic);
            if (amphorien != null) nevronTypes.Add(amphorien);

            if (nevronTypes.Count == 0)
            {
                Log.Error("Expedition33_NoValidNevronTypes".Translate());
                return false;
            }

            // Détermine la composition du groupe
            int count = Rand.RangeInclusive(4, 12);

            // Trouve zone d'entrée
            if (!RCellFinder.TryFindRandomPawnEntryCell(out IntVec3 spawnSpot, map, CellFinder.EdgeRoadChance_Animal))
            {
                return false;
            }

            // SPAWN avec variété
            List<Pawn> nevrons = new List<Pawn>();
            for (int i = 0; i < count; i++)
            {
                // Variété dans les types
                PawnKindDef selectedKind = nevronTypes.RandomElement();
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(spawnSpot, map, 10);
                if (!loc.IsValid) loc = spawnSpot;

                Pawn nevron = PawnGenerator.GeneratePawn(selectedKind, null);
                GenSpawn.Spawn(nevron, loc, map, WipeMode.Vanish);
                nevrons.Add(nevron);

                // Chance variable de manhunter selon difficulté
                // float manhunterChance = 0.3f + (parms.points / 1000f) * 0.4f;
                // if (Rand.Chance(manhunterChance))
                // {
                //     nevron.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);
                // }
            }

            // Utilise les paramètres de l'incident
            string letterText = def.letterText ?? "Expedition33_NevronMigrationDesc".Translate(count);

            Find.LetterStack.ReceiveLetter(
                def.letterLabel ?? "Expedition33_NevronMigrationTitle".Translate(),
                letterText,
                def.letterDef ?? LetterDefOf.NeutralEvent,
                nevrons.Count > 0 ? nevrons[0] : null
            );

            return true;
        }
    }

    public class IncidentWorker_NevronMassRaid : IncidentWorker_RaidEnemy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Log.Message("Expedition33_NevronMassRaidAttempt".Translate());

            // FORCE ta faction Nevrons
            Faction nevronFaction = Find.FactionManager.FirstFactionOfDef(
                FactionDef.Named("Nevrons"));

            if (nevronFaction == null)
            {
                Log.Warning("Expedition33_NevronFactionNotFound".Translate());
                // FALLBACK: Utilise n'importe quelle faction hostile
                nevronFaction = Find.FactionManager.RandomEnemyFaction();
                if (nevronFaction == null) return false;
            }

            parms.faction = nevronFaction;
            // Force plus de points pour plus d'unités
            parms.points = Math.Max(parms.points * 1.8f, 800f);

            Log.Message("Expedition33_NevronMassRaidExecuting".Translate(nevronFaction.Name, parms.points));
            return base.TryExecuteWorker(parms);
        }
    }

    public class IncidentWorker_PitankAlphaAttack : IncidentWorker_RaidEnemy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Log.Message("Expedition33_PitankAlphaAttempt".Translate());

            // FORCE ta faction Nevrons
            Faction nevronFaction = Find.FactionManager.FirstFactionOfDef(
                FactionDef.Named("Nevrons"));

            if (nevronFaction == null)
            {
                Log.Warning("Expedition33_NevronFactionNotFound".Translate());
                // FALLBACK: Utilise n'importe quelle faction hostile
                nevronFaction = Find.FactionManager.RandomEnemyFaction();
                if (nevronFaction == null) return false;
            }

            parms.faction = nevronFaction;
            // Force BEAUCOUP de points pour garantir les gros types
            parms.points = Math.Max(parms.points * 2.5f, 1500f);

            Log.Message("Expedition33_PitankAlphaExecuting".Translate(nevronFaction.Name, parms.points));
            return base.TryExecuteWorker(parms);
        }
    }
// ✅ PATCH DÉFINITIF - Empêcher le recrutement des Nevrons Déchus
[HarmonyPatch(typeof(Pawn_GuestTracker), "get_Recruitable")]
public static class Patch_NevronNotRecruitable
{
    public static void Postfix(Pawn_GuestTracker __instance, ref bool __result)
    {
        Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        if (pawn?.kindDef?.defName == "NevronDechut")
        {
            __result = false; // Les Nevrons Déchus ne sont jamais recrutables
            
            // Message optionnel pour informer le joueur
            if (__result != false) // Éviter les messages répétés
            {
                Log.Message($"[Nevron Mod] {pawn.Name} est un Nevron Déchu - non recrutables");
            }
        }
    }
}

}

    
    // [StaticConstructorOnStartup]
    // public static class FactionDebugger
    // {
    //     static FactionDebugger()
    //     {
    //         FactionDef faction = FactionDef.Named("Nevrons");
    //         if (faction == null)
    //         {
    //             Log.Error("❌ FactionDef Nevrons NOT FOUND");
    //             return;
    //         }

    //         Log.Message($"✅ FactionDef found: {faction.label}");

    //         if (faction.pawnGroupMakers == null || faction.pawnGroupMakers.Count == 0)
    //         {
    //             Log.Error("❌ FactionDef has NO pawnGroupMakers!");
    //             return;
    //         }

    //         Log.Message($"✅ PawnGroupMakers count: {faction.pawnGroupMakers.Count}");

    //         // Vérifie chaque pawnGroupMaker
    //         foreach (var groupMaker in faction.pawnGroupMakers)
    //         {
    //             Log.Message($"   GroupMaker: {groupMaker.kindDef} (commonality: {groupMaker.commonality})");

    //             if (groupMaker.options == null || groupMaker.options.Count == 0)
    //             {
    //                 Log.Error($"   ❌ {groupMaker.kindDef} has NO options!");
    //                 continue;
    //             }

    //             // Vérifie chaque PawnKindDef dans les options
    //             foreach (var option in groupMaker.options)
    //             {
    //                 if (option.kind == null)
    //                 {
    //                     Log.Error($"   ❌ NULL PawnKindDef in {groupMaker.kindDef} options!");
    //                 }
    //                 else
    //                 {
    //                     Log.Message($"   ✅ {option.kind.defName} (weight: {option.selectionWeight})");
    //                 }
    //             }
    //         }   }
    

    // [DebugAction("DEBUG: Expedition33 Full Test", "Spawning", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    //     public static void DebugExpedition33FullTest()
    //     {
    //         Log.Message("=== EXPEDITION33 FULL DIAGNOSTIC ===");

    //         // 1. Vérifier la faction
    //         var faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("Nevrons"));
    //         if (faction == null)
    //         {
    //             Log.Error("❌ FACTION NOT FOUND!");
    //             return;
    //         }
    //         Log.Message($"✅ Faction: {faction.Name} (hostile: {faction.HostileTo(Faction.OfPlayer)})");

    //         // 2. Vérifier les races
    //         string[] raceNames = { "Nevron", "Pitank", "NevronDechutRace" };
    //         foreach (string raceName in raceNames)
    //         {
    //             var race = DefDatabase<ThingDef>.GetNamedSilentFail(raceName);
    //             if (race != null)
    //             {
    //                 Log.Message($"✅ Race: {raceName}");
    //             }
    //             else
    //             {
    //                 Log.Error($"❌ Race: {raceName} NOT FOUND!");
    //             }
    //         }

    //         // 3. Vérifier les PawnKindDefs + leurs races
    //         string[] pawnKindNames = {
    //     "Nevron_Basic",
    //     "Amphorien",
    //     "Pitank",
    //     "NevronDechut"
    // };

    //         foreach (string kindName in pawnKindNames)
    //         {
    //             var pawnKind = DefDatabase<PawnKindDef>.GetNamedSilentFail(kindName);
    //             if (pawnKind != null)
    //             {
    //                 Log.Message($"✅ PawnKind: {kindName}");
    //                 Log.Message($"   - Race: {pawnKind.race?.defName ?? "NULL"}");
    //                 Log.Message($"   - CombatPower: {pawnKind.combatPower}");
    //                 Log.Message($"   - DefaultFaction: {pawnKind.defaultFactionDef?.defName ?? "NULL"}");

    //                 // Test génération individuelle
    //                 try
    //                 {
    //                     var testPawn = PawnGenerator.GeneratePawn(pawnKind, faction);
    //                     Log.Message($"   - ✅ Can generate: {testPawn.Label}");
    //                     testPawn.Destroy();
    //                 }
    //                 catch (System.Exception ex)
    //                 {
    //                     Log.Error($"   - ❌ Generation failed: {ex.Message}");
    //                 }
    //             }
    //             else
    //             {
    //                 Log.Error($"❌ PawnKind: {kindName} NOT FOUND!");
    //             }
    //         }

    //         // 4. Test PawnGroupMaker avec différents points
    //         Log.Message("=== TESTING PAWNGROUPMAKER ===");

    //         int[] testPointValues = { 100, 200, 500, 1000 };

    //         foreach (int points in testPointValues)
    //         {
    //             Log.Message($"--- Testing with {points} points ---");

    //             var parms = new PawnGroupMakerParms();
    //             parms.tile = Find.CurrentMap.Tile;
    //             parms.faction = faction;
    //             parms.points = points;
    //             parms.groupKind = PawnGroupKindDefOf.Combat;

    //             try
    //             {
    //                 // Test si le PawnGroupMaker peut calculer
    //                 var availableKinds = faction.def.pawnGroupMakers
    //                     .Where(pgm => pgm.kindDef == PawnGroupKindDefOf.Combat)
    //                     .SelectMany(pgm => pgm.options)
    //                     .ToList();

    //                 Log.Message($"   Available kinds in Combat group: {availableKinds.Count}");

    //                 foreach (var option in availableKinds)
    //                 {
    //                     Log.Message($"     - {option.kind?.defName} (weight: {option.selectionWeight}, cost: {option.Cost})");
    //                 }

    //                 // Test génération
    //                 var pawns = PawnGroupMakerUtility.GeneratePawns(parms).ToList();

    //                 if (pawns.Count > 0)
    //                 {
    //                     Log.Message($"   ✅ SUCCESS: Generated {pawns.Count} pawns");
    //                     foreach (var pawn in pawns)
    //                     {
    //                         Log.Message($"     - {pawn.kindDef.defName} ({pawn.def.defName})");
    //                     }

    //                     // Spawn quelques-uns pour test visuel
    //                     for (int i = 0; i < Math.Min(3, pawns.Count); i++)
    //                     {
    //                         GenSpawn.Spawn(pawns[i], Find.CurrentMap.Center + GenRadial.RadialPattern[i], Find.CurrentMap);
    //                     }
    //                 }
    //                 else
    //                 {
    //                     Log.Error($"   ❌ FAILED: No pawns generated with {points} points");
    //                 }
    //             }
    //             catch (System.Exception ex)
    //             {
    //                 Log.Error($"   ❌ EXCEPTION with {points} points: {ex.Message}");
    //                 Log.Error($"   Stack: {ex.StackTrace}");
    //             }
    //         }

    //         // 5. Test raid direct
    //         Log.Message("=== TESTING DIRECT RAID ===");
    //         try
    //         {
    //             IncidentParms incidentParms = new IncidentParms();
    //             incidentParms.target = Find.CurrentMap;
    //             incidentParms.faction = faction;
    //             incidentParms.points = 200;

    //             var raidDef = IncidentDefOf.RaidEnemy;
    //             bool result = raidDef.Worker.TryExecute(incidentParms);

    //             Log.Message($"Direct raid result: {result}");
    //         }
    //         catch (System.Exception ex)
    //         {
    //             Log.Error($"Direct raid failed: {ex.Message}");
    //         }
    //     }


    // }


