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
    


    [StaticConstructorOnStartup]
    public class Expedition33Mod : Mod
    {
        public Expedition33Mod(ModContentPack content) : base(content)
        {
            Log.Message("Hello World!");
            Log.Message("Expédition 33 - Mod chargé avec succès !");
            Log.Message("Le Gommage guette... Restez vigilants, expéditionnaires.");
            var h = new Harmony("warult.expedition33");
            h.PatchAll();                              // ← indispensable
            Log.Message("[Expedition33] Harmony PatchAll exécuté");
        }
    }


    public class ThoughtWorker_ExpeditionMission : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            // Logique de la pens�e li�e aux missions d'exp�dition
            var gameComp = Current.Game.GetComponent<GameComponent_ExpeditionStats>();
            if (gameComp != null && gameComp.completedMissions > 0)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            return ThoughtState.Inactive;
        }
    }



    // Effets visuels pour le Gommage
    public static class GommageEffects
    {
        public static void CreateGommageEffect(IntVec3 position, Map map)
        {
            // Effet de fum�e grise myst�rieuse
            FleckMaker.ThrowSmoke(position.ToVector3(), map, 3f);

            // CORRIG� - ThrowDustPuff sans param�tre Color
            FleckMaker.ThrowDustPuff(position.ToVector3(), map, 2f);

            // Particules artistiques (simule des coups de pinceau)
            for (int i = 0; i < 8; i++)
            {
                Vector3 randomPos = position.ToVector3() + new Vector3(
                    Rand.Range(-1f, 1f),
                    0f,
                    Rand.Range(-1f, 1f)
                );
                // CORRIG� - ThrowDustPuff sans Color
                FleckMaker.ThrowDustPuff(randomPos, map, 1f);
            }

            // Son myst�rieux
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
                // Ignore silencieusement si le son ne peut pas �tre jou�
            }
        }
    }

    // Optimisations de performance
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
            paintressBoss = DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_PaintressMonster");
            expedition33Faction = DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33");
        }
    }

    






   




    




    public class Site_PersistentMonolith : Site
    {
        private bool paintressAlive = true;

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            // V�rifie si la Panteresse est encore vivante sur cette carte
            if (Map != null)
            {
                var paintress = Map.mapPawns.AllPawns
                    .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                paintressAlive = (paintress != null && !paintress.Dead);
            }

            if (paintressAlive)
            {
                // EMP�CHE la suppression tant que la Panteresse vit
                alsoRemoveWorldObject = false;
                return false;
            }

            // Permet la suppression apr�s la mort de la Panteresse
            alsoRemoveWorldObject = true;
            return true;
        }

        public override string GetInspectString()
        {
            if (paintressAlive)
            {
                return "Monolithe de la Paintress - La terreur artistique garde ce lieu";
            }
            else
            {
                return "Ancien Monolithe - Le cycle du Gommage est bris�";
            }
        }
    }

   
    

    public class HediffComp_ArtisticCorruption : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            // La corruption augmente pr�s de la Paintress
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp != null && gameComp.paintressAlive)
            {
                // CORRIG� - Cherche la Paintress sur la carte actuelle
                if (Pawn?.Map != null)
                {
                    var paintress = Pawn.Map.mapPawns.AllPawns
                        .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                    if (paintress != null && !paintress.Dead)
                    {
                        float distance = Pawn.Position.DistanceTo(paintress.Position);
                        if (distance < 50) // Dans un rayon de 50 cases
                        {
                            severityAdjustment += 0.001f; // Corruption acc�l�r�e
                        }
                    }
                }
            }
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);

            Messages.Message(
                $"{Pawn.Name.ToStringShort} d�veloppe une corruption artistique...",
                MessageTypeDefOf.NegativeHealthEvent
            );
        }
    }

}
