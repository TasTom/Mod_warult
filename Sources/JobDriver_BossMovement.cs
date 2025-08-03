using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Mod_warult
{
    public class JobDriver_BossMovement : JobDriver
    {
        private int stuckTicks = 0;
        private IntVec3 lastPosition = IntVec3.Invalid;
        private const int MAX_STUCK_TICKS = 300; // 5 secondes maximum bloqué

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return new Toil
            {
                initAction = () =>
                {
                    Log.Message($"[BossMovement] {pawn.LabelShort} démarre mouvement vers {TargetA}");

                    var target = job.targetA.Thing as Pawn;
                    if (target == null || target.Downed || target.Dead)
                    {
                        Log.Message($"[BossMovement] Cible invalide, fin du job");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    lastPosition = pawn.Position;
                    stuckTicks = 0;

                    if (pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                    {
                        pawn.pather.StartPath(target, PathEndMode.Touch);
                    }
                    else
                    {
                        Log.Message($"[BossMovement] Impossible d'atteindre la cible");
                        EndJobWith(JobCondition.Incompletable);
                    }
                },

                tickAction = () =>
                {
                    var target = job.targetA.Thing as Pawn;
                    if (target == null || target.Downed || target.Dead)
                    {
                        Log.Message($"[BossMovement] Cible perdue pendant mouvement");
                        EndJobWith(JobCondition.Incompletable);
                        return;
                    }

                    float distance = pawn.Position.DistanceTo(target.Position);

                    // ✅ CONDITION COHÉRENTE avec ThinkNode
                    if (distance <= 1.2f)
                    {
                        Log.Message($"[BossMovement] {pawn.LabelShort} arrivé près de {target.LabelShort} ({distance:F1})");
                        EndJobWith(JobCondition.Succeeded);
                        return;
                    }

                    // ✅ DÉTECTION DE BLOCAGE AMÉLIORÉE
                    if (pawn.Position == lastPosition)
                    {
                        stuckTicks++;

                        // Tentative de déblocage à mi-chemin
                        if (stuckTicks == MAX_STUCK_TICKS / 2)
                        {
                            Log.Message($"[BossMovement] Tentative de déblocage pour {pawn.LabelShort}");
                            if (pawn.CanReach(target, PathEndMode.ClosestTouch, Danger.Some))
                            {
                                pawn.pather.StartPath(target, PathEndMode.ClosestTouch);
                                stuckTicks = 0; // Reset après déblocage
                                return;
                            }
                        }

                        if (stuckTicks >= MAX_STUCK_TICKS)
                        {
                            Log.Warning($"[BossMovement] {pawn.LabelShort} bloqué définitivement, abandon");
                            EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                    }
                    else
                    {
                        lastPosition = pawn.Position;
                        stuckTicks = 0;
                    }

                    // ✅ GESTION DU PATHFINDING ROBUSTE
                    if (!pawn.pather.Moving && distance > 1.2f)
                    {
                        if (pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
                        {
                            pawn.pather.StartPath(target, PathEndMode.Touch);
                        }
                        else if (pawn.CanReach(target, PathEndMode.ClosestTouch, Danger.Some))
                        {
                            pawn.pather.StartPath(target, PathEndMode.ClosestTouch);
                        }
                        else
                        {
                            Log.Message($"[BossMovement] Aucun chemin disponible vers {target.LabelShort}");
                            EndJobWith(JobCondition.Incompletable);
                        }
                    }
                },

                defaultCompleteMode = ToilCompleteMode.Never
            };
        }



        // ✅ OVERRIDE pour gérer la fin du job proprement
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref stuckTicks, "stuckTicks", 0);
            Scribe_Values.Look(ref lastPosition, "lastPosition", IntVec3.Invalid);
        }
    }
}
