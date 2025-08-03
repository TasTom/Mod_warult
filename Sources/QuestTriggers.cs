using HarmonyLib;
using RimWorld;
using Verse;

namespace Mod_warult
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_BossKilled
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance)
        {
            if (__instance?.kindDef?.defName?.StartsWith("Expedition33_") == true)
            {
                string bossId = __instance.kindDef.defName;
                string trigger = $"BOSS_DEFEATED_{bossId}";
                
                Log.Message("Expedition33_BossKilledLog".Translate(bossId, trigger));
                QuestManager.TriggerQuestEvent(trigger);

                if (bossId == "Expedition33_Paintress")
                {
                    NarrativeEvents.TriggerFinalVictory();
                }
            }
        }
    }
}
