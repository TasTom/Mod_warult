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
using HarmonyLib;

namespace Mod_warult
{
    public class Expedition33Mod : Mod
    {
        public Expedition33Mod(ModContentPack content) : base(content)
        {
            // Messages temporaires sans traduction pour éviter les erreurs lors de l'initialisation
            Log.Message("[Expedition33] Hello World!");
            Log.Message("[Expedition33] Mod loaded successfully!");
            Log.Message("[Expedition33] Le Gommage guette... Restez vigilants, expéditionnaires.");

            var h = new Harmony("warult.expedition33");
            h.PatchAll();
            Log.Message("[Expedition33] Harmony PatchAll executed");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // Utilisation sécurisée des traductions dans l'interface utilisateur
            listing.Label(GetTranslationSafe("Expedition33_CombatXPMultiplier", 
                ExpeditionSettings.combatXPMultiplier.ToString("F1")));
            ExpeditionSettings.combatXPMultiplier = listing.Slider(ExpeditionSettings.combatXPMultiplier, 0.1f, 5.0f);
            listing.CheckboxLabeled(GetTranslationSafe("Expedition33_XPSharingEnabled"), 
                ref ExpeditionSettings.enableXPSharing);
            var harmony = new HarmonyLib.Harmony("mod.warult.nevrons");
            harmony.PatchAll();
            
            listing.End();
        }

        // Méthode helper pour les traductions sécurisées
  
    private string GetTranslationSafe(string key, params object[] args)
    {
        try
        {
            var translated = key.TranslateSimple();
            
            // Si on a des arguments, les formater manuellement
            if (args.Length > 0)
            {
                return string.Format(translated, args);
            }
            
            return translated;
        }
        catch
        {
            // Fallback en cas d'échec de traduction
            return key + (args.Length > 0 ? ": " + string.Join(", ", args) : "");
        }
    }



    }

    // Initialisation retardée pour les messages traduits
    [StaticConstructorOnStartup]
    public static class Expedition33PostInitializer
    {
        static Expedition33PostInitializer()
        {
            // Les traductions sont maintenant disponibles
            try
            {
                Log.Message("Expedition33_ModLoadedHello".Translate());
                Log.Message("Expedition33_ModLoadedSuccess".Translate());
                Log.Message("Expedition33_GommageWaits".Translate());
                Log.Message("Expedition33_HarmonyExecuted".Translate());
            }
            catch (Exception ex)
            {
                Log.Warning($"[Expedition33] Could not load translated messages: {ex.Message}");
            }
        }
    }

    public class ExpeditionSettings : ModSettings
    {
        public static float combatXPMultiplier = 1.0f;
        public static float soulXPMultiplier = 1.0f;
        public static int pointsPerLevel = 3;
        public static bool enableXPSharing = false;
        public static float bossXPBonus = 10.0f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref combatXPMultiplier, "combatXPMultiplier", 1.0f);
            Scribe_Values.Look(ref soulXPMultiplier, "soulXPMultiplier", 1.0f);
            Scribe_Values.Look(ref pointsPerLevel, "pointsPerLevel", 3);
            Scribe_Values.Look(ref enableXPSharing, "enableXPSharing", false);
            Scribe_Values.Look(ref bossXPBonus, "bossXPBonus", 10.0f);
            base.ExposeData();
        }
    }

    public class ThoughtWorker_ExpeditionMission : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var gameComp = Current.Game.GetComponent<GameComponent_ExpeditionStats>();
            if (gameComp != null && gameComp.completedMissions > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }

    public static class GommageEffects
    {
        public static void CreateGommageEffect(IntVec3 position, Map map)
        {
            FleckMaker.ThrowSmoke(position.ToVector3(), map, 3f);
            FleckMaker.ThrowDustPuff(position.ToVector3(), map, 2f);
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 randomPos = position.ToVector3() + new Vector3(
                    Rand.Range(-1f, 1f),
                    0f,
                    Rand.Range(-1f, 1f)
                );
                FleckMaker.ThrowDustPuff(randomPos, map, 1f);
            }

            PlaySoundAt(SoundDefOf.Psycast_Skip_Exit, position, map);
        }

        public static void PlaySoundAt(SoundDef soundDef, IntVec3 position, Map map)
        {
            try
            {
                soundDef.PlayOneShot(new TargetInfo(position, map, false));
            }
            catch
            {
                // Ignore silencieusement si le son ne peut pas être joué
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class ExpeditionOptimizations
    {
        static ExpeditionOptimizations()
        {
            CacheDefinitions();
        }

        public static IncidentDef gommageIncident;
        public static IncidentDef missionIncident;
        public static IncidentDef paintressIncident;
        public static PawnKindDef paintressBoss;
        public static FactionDef expedition33Faction;

        private static void CacheDefinitions()
        {
            gommageIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("Expedition33_Gommage");
            missionIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("Expedition33_Mission");
            paintressIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("Expedition33_PainterSighting");
            paintressBoss = DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_Paintress");
            expedition33Faction = DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33");
        }
    }

    public class Site_PersistentMonolith : Site
    {
        private bool paintressAlive = true;

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if (Map != null)
            {
                var paintress = Map.mapPawns.AllPawns
                    .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_Paintress");
                paintressAlive = (paintress != null && !paintress.Dead);
            }

            if (paintressAlive)
            {
                alsoRemoveWorldObject = false;
                return false;
            }

            alsoRemoveWorldObject = true;
            return true;
        }

        public override string GetInspectString()
        {
            try
            {
                if (paintressAlive)
                {
                    return "Expedition33_PaintressMonolithActive".Translate();
                }
                else
                {
                    return "Expedition33_PaintressMonolithInactive".Translate();
                }
            }
            catch
            {
                // Fallback si la traduction échoue
                return paintressAlive ? 
                    "Paintress Monolith - The artistic terror guards this place" : 
                    "Ancient Monolith - The Gommage cycle is broken";
            }
        }
    }

    public class HediffComp_ArtisticCorruption : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            
            if (gameComp != null && gameComp.paintressAlive)
            {
                if (Pawn?.Map != null)
                {
                    var paintress = Pawn.Map.mapPawns.AllPawns
                        .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_Paintress");
                        
                    if (paintress != null && !paintress.Dead)
                    {
                        float distance = Pawn.Position.DistanceTo(paintress.Position);
                        if (distance < 50)
                        {
                            severityAdjustment += 0.001f;
                        }
                    }
                }
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            try
            {
                Messages.Message(
                    "Expedition33_ArtisticCorruptionDeveloped".Translate(Pawn.Name.ToStringShort),
                    MessageTypeDefOf.NegativeHealthEvent
                );
            }
            catch
            {
                // Fallback si la traduction échoue
                Messages.Message(
                    $"{Pawn.Name.ToStringShort} develops artistic corruption...",
                    MessageTypeDefOf.NegativeHealthEvent
                );
            }
        }
    }
}
