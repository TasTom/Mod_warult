using RimWorld;
using Verse;
using System.Linq;
using HarmonyLib;

namespace Mod_warult
{
    public static class NarrativeEvents
    {
        public static void TriggerVersoArrival()
        {
            var verso = Find.Maps.SelectMany(m => m.mapPawns.AllPawns)
                .FirstOrDefault(p => p.kindDef?.defName == "Expedition_Verso");

            if (verso != null)
            {
                Log.Message("Expedition_VersoAlreadyExists".Translate());
                return;
            }

            try
            {
                var request = new PawnGenerationRequest(
                    kind: DefDatabase<PawnKindDef>.GetNamed("Expedition_Verso"),
                    faction: Faction.OfPlayer,
                    mustBeCapableOfViolence: true
                );

                var pawn = PawnGenerator.GeneratePawn(request);
                BossNameManager.InitializeBossName(pawn);

                Log.Message("Expedition_VersoFinalName".TranslateSimple().Formatted(pawn.Name.ToString()));

                var map = Find.Maps.FirstOrDefault(m => m.IsPlayerHome) ?? Find.CurrentMap;
                if (map != null)
                {
                    var spawnCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 15);
                    GenSpawn.Spawn(pawn, spawnCell, map);
                    Messages.Message("Expedition_VersoJoined".Translate(), MessageTypeDefOf.PositiveEvent);
                }
            }
            catch (System.Exception e)
            {
                Log.Error("Expedition_VersoGenerationError".Translate(e.Message));
            }
        }

        public static void TriggerActeICompletion()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "Expedition33_ActeICompleted".Translate()));
        }

        public static void TriggerFinalVictory()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "Expedition33_FinalVictory".Translate()));
        }
    }

    [HarmonyPatch]
    public static class VersoNamePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PawnBioAndNameGenerator), "GenerateFullPawnName")]
        public static bool InterceptVersoNameGeneration(ThingDef genFor, ref Name __result)
        {
            if (genFor?.defName == "Expedition_VersoColon")
            {
                __result = new NameSingle("Verso");
                Log.Message("Expedition_VersoNameIntercepted".Translate());
                return false;
            }

            return true;
        }
    }
}
