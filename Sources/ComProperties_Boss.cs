using Verse;
using RimWorld;
using System.Linq;
using Verse.AI;
using HarmonyLib;

namespace Mod_warult
{
    public class CompProperties_AutoHealBleed : CompProperties
    {
        public CompProperties_AutoHealBleed()
        {
            this.compClass = typeof(CompAutoHealBleed);
        }
    }

    public class CompAutoHealBleed : ThingComp
    {
        // ✅ COMP SIMPLIFIÉ : Juste un marqueur
        // Le vrai travail est fait par les patches Harmony

        public override void CompTick()
        {
            // Plus besoin de logique complexe, les patches s'en occupent !
        }

        // ✅ MÉTHODE UTILITAIRE : Pour debug
        public void LogBleedingStatus()
        {
            if (parent is Pawn pawn && Prefs.DevMode)
            {
                var bleedingWounds = pawn.health.hediffSet.hediffs
                    .OfType<Hediff_Injury>()
                    .Where(h => h.Bleeding)
                    .ToList();

                //Log.Message($"[AutoHeal] {pawn.LabelShort} - {bleedingWounds.Count} blessures, BleedRateTotal: {pawn.health.hediffSet.BleedRateTotal:F3}");
            }
        }
    }



    [HarmonyPatch(typeof(HediffSet), "BleedRateTotal", MethodType.Getter)]
    public static class Patch_BossBleedRateTotal
    {
        public static void Postfix(HediffSet __instance, ref float __result)
        {
            // Vérifier si c'est un boss d'Expedition33
            if (__instance.pawn?.kindDef?.defName?.StartsWith("Expedition33_") == true || __instance.pawn?.kindDef?.defName?.StartsWith("Gestral") == true)
            {
                var comp = __instance.pawn.GetComp<CompAutoHealBleed>();
                if (comp != null)
                {
                    if (Prefs.DevMode && __result > 0f)
                    {
                        //Log.Message($"[AutoHeal] Saignement bloqué pour {__instance.pawn.LabelShort} (était {__result:F2})");
                    }

                    __result = 0f; // ✅ Forcer BleedRateTotal à 0
                }
            }
        }
    }


    [HarmonyPatch(typeof(Hediff), "Bleeding", MethodType.Getter)]
    public static class Patch_BossHideBleedingIcon
    {
        public static void Postfix(Hediff __instance, ref bool __result)
        {
            // Vérifier que c'est bien une blessure et un boss
            if (__instance is Hediff_Injury injury &&
                (injury.pawn?.kindDef?.defName?.StartsWith("Expedition33_") == true || __instance.pawn?.kindDef?.defName?.StartsWith("Gestral") == true))
            {
                var comp = injury.pawn.GetComp<CompAutoHealBleed>();
                if (comp != null)
                {
                    if (__result && Prefs.DevMode)
                    {
                        //Log.Message($"[AutoHeal] Icône saignement masquée pour {__instance.LabelCap}");
                    }

                    __result = false; // ✅ Masquer l'icône de saignement
                }
            }
        }
    }




    public class CompProperties_NoNeeds : CompProperties
    {
        public CompProperties_NoNeeds()
        {
            this.compClass = typeof(CompNoNeeds);
        }
    }


    public class CompProperties_BossAggro : CompProperties
    {
        public float aggroRadius = 200f;
        public bool alwaysHostile = true;
        public float threatLevel = 1.0f;

        public CompProperties_BossAggro()
        {
            this.compClass = typeof(CompBossAggro);
        }
    }

    public class CompBossAggro : ThingComp
    {
        public CompProperties_BossAggro Props => (CompProperties_BossAggro)this.props;
        // private int lastReevaluationTick = 0;
        private const int REEVALUATION_INTERVAL = 120;
        private bool hasInitialized = false;
        private bool isProcessingSpawn = false; // ✅ NOUVEAU : Protection contre les boucles

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (parent is Pawn boss && Props.alwaysHostile && !respawningAfterLoad && !isProcessingSpawn)
            {
                  
                // Ne pas appliquer l'IA boss aux colons enrôlés
                if (parent is Pawn boss2 && boss2.IsColonistPlayerControlled)
                    return;
                // Log.Message($"[Boss AI] {boss.LabelShort} - PostSpawnSetup : interception immédiate");
                isProcessingSpawn = true; // ✅ Éviter les réentrances

                // Programmer le nettoyage pour le tick suivant (éviter les conflits)
                Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

               if (parent is Pawn boss3 && boss3.IsColonistPlayerControlled)
                    return;

            if (parent is Pawn boss && boss.Spawned && Props.alwaysHostile)
            {
                if (isProcessingSpawn)
                {
                    if (boss.jobs != null)
                    {
                        boss.jobs.StopAll();
                        ForceIntelligentMovement(boss);
                    }
                    isProcessingSpawn = false;
                    hasInitialized = true;
                    return;
                }

                // ✅ CORRECTION MAJEURE : Ne plus interrompre le combat
                if (hasInitialized)
                {
                    var currentJob = boss.jobs?.curJob;
                    if (currentJob != null)
                    {
                        // Interrompre SEULEMENT les Goto vanilla
                        if (currentJob.def == JobDefOf.Goto)
                        {
                            Log.Message($"[CompBossAggro] {boss.LabelShort} - Interruption Goto pour mouvement intelligent");
                            boss.jobs.EndCurrentJob(JobCondition.InterruptForced);
                            ForceIntelligentMovement(boss);
                            return;
                        }

                        // ✅ LAISSER AttackMelee et ExecuteAbility tranquilles
                        if (currentJob.def == JobDefOf.AttackMelee ||
                            currentJob.def.defName == "Expedition33_ExecuteAbility")
                        {
                            // Ne rien faire - laisser ces jobs se terminer naturellement
                            return;
                        }
                    }
                }

                // ✅ RÉDUCTION drastique de la fréquence pour éviter les conflits
                if (Find.TickManager.TicksGame % 300 == 0) // Toutes les 5 secondes seulement
                {
                    var nearbyColonists = boss.Map.mapPawns.FreeColonists
                        .Where(p => p.Position.DistanceTo(boss.Position) <= Props.aggroRadius && !p.Downed && !p.Dead)
                        .OrderBy(p => boss.Position.DistanceTo(p.Position))
                        .ToList();

                    if (nearbyColonists.Any())
                    {
                        var closestColonist = nearbyColonists.First();

                        // Ne changer de cible que si vraiment nécessaire
                        if (boss.mindState.enemyTarget != closestColonist)
                        {
                            boss.mindState.enemyTarget = closestColonist;
                            boss.mindState.anyCloseHostilesRecently = true;
                            boss.mindState.canFleeIndividual = false;
                            boss.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;
                        }
                    }
                }
            }
        }








        // ✅ MÉTHODE UNIQUE ET PROTÉGÉE contre les réentrances
        private void ForceIntelligentMovement(Pawn boss)
        {
            if (isProcessingSpawn) return; // Éviter les boucles pendant le spawn

            var target = FindBestTarget(boss);
            if (target != null)
            {
                // Log.Message($"[Boss AI] {boss.LabelShort} - Démarrage mouvement intelligent vers {target.LabelShort}");

                var jobDef = DefDatabase<JobDef>.GetNamed("Expedition33_BossMovement");
                if (jobDef != null)
                {
                    Job smartMovement = JobMaker.MakeJob(jobDef, target);
                    boss.jobs.StartJob(smartMovement, JobCondition.InterruptForced);
                    boss.mindState.enemyTarget = target;
                }
            }
        }

        private void ReevaluateBossTarget(Pawn boss)
        {
            var currentJob = boss.jobs?.curJob;
            if (currentJob == null) return;

            bool isMoving = currentJob.def == JobDefOf.Goto ||
                           currentJob.def.defName == "Expedition33_BossMovement";

            if (isMoving)
            {
                var currentTarget = currentJob.targetA.Thing as Pawn;
                var newTarget = FindBestTarget(boss);

                if (newTarget != null && newTarget != currentTarget)
                {
                    float currentDistance = currentTarget != null ?
                        boss.Position.DistanceTo(currentTarget.Position) : float.MaxValue;
                    float newDistance = boss.Position.DistanceTo(newTarget.Position);

                    if (newDistance < currentDistance - 3f)
                    {
                        // Log.Message($"[Boss AI] {boss.LabelShort} change de cible: {currentTarget?.LabelShort} → {newTarget.LabelShort}");

                        currentJob.SetTarget(TargetIndex.A, newTarget);
                        boss.pather.StartPath(newTarget.Position, PathEndMode.Touch);
                        boss.mindState.enemyTarget = newTarget;
                    }
                }
            }
        }

        private Pawn FindBestTarget(Pawn boss)
        {
            var allTargets = boss.Map.mapPawns.FreeColonists
                .Where(p => !p.Downed && !p.Dead)
                .ToList();

            if (!allTargets.Any()) return null;

            return allTargets
                .OrderBy(p => boss.Position.DistanceTo(p.Position))
                .ThenByDescending(p => p.health.summaryHealth.SummaryHealthPercent)
                .FirstOrDefault();
        }
    }



    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class Patch_PreventBossMovementJobs
    {
        public static bool Prefix(Pawn_JobTracker __instance, Job newJob, ref Pawn ___pawn)
        {
            // ✅ EXCLUSION pour les colons enrôlés
            if (___pawn.IsColonistPlayerControlled)
                return true;

            if (___pawn.kindDef?.defName?.StartsWith("Expedition33_") == true
            || ___pawn.kindDef?.defName?.StartsWith("Nevron") == true
            || ___pawn.kindDef?.defName?.StartsWith("Amphorien") == true
            || ___pawn.kindDef?.defName?.StartsWith("Pitank") == true)
            {
                // ✅ AUTORISATION TOTALE des jobs de combat
                // ✅ BLOQUER SEULEMENT les Goto indésirables
                // ✅ LAISSER AttackMelee et ExecuteAbility tranquilles

                // Vérifier si le job est un job de combat
                if (newJob.def == JobDefOf.AttackMelee ||
                    newJob.def == JobDefOf.AttackStatic ||
                    newJob.def.defName == "Expedition33_ExecuteAbility")
                {
                    // ✅ AUTORISATION TOTALE des jobs de combat
                    if (newJob.def == JobDefOf.AttackMelee ||
                        newJob.def == JobDefOf.AttackStatic ||
                        newJob.def.defName == "Expedition33_ExecuteAbility")
                    {
                        // Laisser passer sans aucune intervention
                        return true;
                    }

                    // Bloquer seulement les Goto indésirables
                    if (newJob.def == JobDefOf.Goto)
                    {
                        // Logique de redirection existante...
                        return false;
                    }
                }

            }
            return true;
        }
    }



[HarmonyPatch(typeof(Pawn_JobTracker), "Notify_DamageTaken")]
public static class Patch_BossIgnoreDamageInterruption
{
    public static bool Prefix(Pawn_JobTracker __instance, DamageInfo dinfo, ref Pawn ___pawn)
    {
           // ✅ EXCLUSION pour les colons enrôlés
        if (___pawn.IsColonistPlayerControlled)
            return true;   


        if (___pawn.kindDef?.defName?.StartsWith("Expedition33_") == true)
            {
                var currentJob = __instance.curJob;

                // Protéger SEULEMENT ExecuteAbility
                if (currentJob?.def.defName == "Expedition33_ExecuteAbility")
                {
                    return false; // Empêcher l'interruption
                }

                // ✅ LAISSER AttackMelee suivre TOUTES les règles vanilla
                // Y compris les interruptions par dégâts qui font partie du système de cooldown
            }

        return true; // Comportement vanilla complet
    }
}








    public class CompNoNeeds : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();

            if (parent is Pawn pawn && pawn.needs != null)
            {
                // Supprime la faim
                var food = pawn.needs.TryGetNeed(NeedDefOf.Food);
                if (food != null)
                {
                    food.CurLevel = food.MaxLevel;
                }

                // Supprime le sommeil
                var rest = pawn.needs.TryGetNeed(NeedDefOf.Rest);
                if (rest != null)
                {
                    rest.CurLevel = rest.MaxLevel;
                }

                // Désactive le malus "sleep starved" éventuel
                if (pawn.health != null)
                {
                    var sleepHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.SleepSuppression);
                    if (sleepHediff != null)
                        pawn.health.RemoveHediff(sleepHediff);
                }
            }
        }
    }
    
    
}