using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;
using System.Linq;
using LudeonTK;

namespace Mod_warult
{
    public class ThinkNode_AbilityUser : ThinkNode
    {
        // Variables statiques partagées
        private static Dictionary<string, List<BossAbility>> bossAbilitiesDict = new Dictionary<string, List<BossAbility>>();
        private static Dictionary<string, int> lastAbilityUsage = new Dictionary<string, int>();


        // Méthode principale appelée par RimWorld
        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            // ✅ EXCLUSION VERSO (sauf si c'est un colon enrôlé)
            if (pawn?.kindDef?.defName == "Expedition_Verso" && 
                !pawn.IsColonistPlayerControlled)
            {
                return ThinkResult.NoJob;
            }
            
            if (!IsBoss(pawn) || !pawn.Spawned)
                return ThinkResult.NoJob;

            // ✅ VÉRIFICATIONS PRÉLIMINAIRES
            if (pawn?.Map?.mapPawns == null)
            {
                Log.Warning($"[Boss AI] TryIssueJobPackage: Map null pour {pawn?.LabelShort ?? "unknown"}");
                return ThinkResult.NoJob;
            }

            if (!IsBoss(pawn) || !pawn.Spawned)
                return ThinkResult.NoJob;

            // ✅ PROTECTION DES JOBS EN COURS (CORRIGÉE)
            var curJob = pawn.jobs?.curJob;
            if (curJob != null)
            {
                // Protéger ExecuteAbility
                if (curJob.def.defName == "Expedition33_ExecuteAbility")
                {
                    var jobAge = Find.TickManager.TicksGame - (curJob.startTick != 0 ? curJob.startTick : Find.TickManager.TicksGame);
                    if (jobAge < 60)
                    {
                        Log.Message($"[ThinkNode] PROTECTION - {pawn.LabelShort} ExecuteAbility récent ({jobAge} ticks)");
                        return ThinkResult.NoJob;
                    }
                }

                // Protection ABSOLUE des jobs AttackMelee
                if (curJob.def == JobDefOf.AttackMelee)
                {
                    var target = curJob.targetA.Thing as Pawn;

                    if (target != null && !target.Downed && !target.Dead)
                    {
                        var jobAge = Find.TickManager.TicksGame - curJob.startTick;

                        if (jobAge < 60)
                        {
                            Log.Message($"[ThinkNode] PROTECTION - {pawn.LabelShort} AttackMelee en cours sur {target.LabelShort} ({jobAge} ticks)");
                            return ThinkResult.NoJob;
                        }

                        float distanceToTarget = pawn.Position.DistanceTo(target.Position);
                        if (distanceToTarget <= 1.4f)
                        {
                            Log.Message($"[ThinkNode] PROTECTION - {pawn.LabelShort} AttackMelee proche cible ({distanceToTarget:F1})");
                            return ThinkResult.NoJob;
                        }
                    }
                    else
                    {
                        Log.Message($"[ThinkNode] AttackMelee sur cible invalide, autorisation d'interruption");
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }

                // ✅ CORRECTION : Vérification AttackMelee DANS le bloc curJob != null
                if (curJob.def == JobDefOf.AttackMelee)
                {
                    var currentTarget = curJob.targetA.Thing as Pawn;
                    var bestTarget = FindBestTarget(pawn);

                    if (currentTarget == bestTarget && currentTarget != null && !currentTarget.Downed && !currentTarget.Dead)
                    {
                        Log.Message($"[ThinkNode] AttackMelee déjà en cours sur la bonne cible");
                        return ThinkResult.NoJob;
                    }
                    else if (bestTarget != null)
                    {
                        Log.Message($"[ThinkNode] AttackMelee sur mauvaise cible, interruption");
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                }
            } // ✅ Fermeture correcte du bloc curJob

            // ✅ PRIORITÉ 1 : COMBAT IMMÉDIAT (CORRIGÉE)
            var enemies = GetNearbyEnemies(pawn, 100f);
            var immediateEnemies = enemies.Where(e => pawn.Position.DistanceTo(e.Position) <= 1.2f).ToList();

            if (immediateEnemies.Any())
            {
                var closestEnemy = immediateEnemies.OrderBy(e =>
                    pawn.Position.DistanceTo(e.Position)).First();
                float distanceToClosest = pawn.Position.DistanceTo(closestEnemy.Position);

                if (pawn.CanReach(closestEnemy, PathEndMode.Touch, Danger.Deadly))
                {
                    Job attackJob = JobMaker.MakeJob(JobDefOf.AttackMelee, closestEnemy);
                    attackJob.expiryInterval = 300;
                    Log.Message($"[Boss AI] {pawn.LabelShort} → Combat immédiat sur {closestEnemy.LabelShort} ({distanceToClosest:F1})");
                    return new ThinkResult(attackJob, this);
                }
            }

            // ✅ RECHERCHE DE CIBLE PRINCIPALE (SÉCURISÉE)
            Pawn target2 = FindBestTarget(pawn);
            if (target2 == null)
            {
                Log.Message($"[Boss AI] {pawn.LabelShort} - Aucune cible trouvée, mouvement aléatoire");

                // ✅ MOUVEMENT ALÉATOIRE au lieu d'attente
                var randomPos = pawn.Position + new IntVec3(Rand.Range(-5, 6), 0, Rand.Range(-5, 6));
                if (randomPos.InBounds(pawn.Map) && randomPos.Walkable(pawn.Map) &&
                    pawn.CanReach(randomPos, PathEndMode.OnCell, Danger.Deadly))
                {
                    Job randomMoveJob = JobMaker.MakeJob(JobDefOf.Goto, randomPos);
                    randomMoveJob.expiryInterval = 120;
                    return new ThinkResult(randomMoveJob, this);
                }

                Job waitJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                waitJob.expiryInterval = 60;
                return new ThinkResult(waitJob, this);
            }

            float distance = pawn.Position.DistanceTo(target2.Position);

            // ✅ PRIORITÉ 2 : CAPACITÉS DISPONIBLES (SÉCURISÉE)
            if (distance <= 50f)
            {
                var available = GetAbilitiesForBoss(pawn)
                    .Where(a => IsAbilityAvailable(pawn, a))
                    .ToList();

                if (available.Any())
                {
                    var best = SelectBestAbility(pawn, available);
                    if (best != null)
                    {
                        var abilityTarget = FindTarget(pawn, best);
                        if (abilityTarget != null)
                        {
                            Job abilityJob = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Expedition33_ExecuteAbility"), abilityTarget);
                            abilityJob.verbToUse = new Verb_ExecuteAbility { ability = best, caster = pawn };
                            RecordAbilityUsage(pawn, best);
                            Log.Message($"[Boss AI] {pawn.LabelShort} → UseAbility {best.name} on {abilityTarget.LabelShort}");
                            return new ThinkResult(abilityJob, this);
                        }
                    }
                }
            }

            // ✅ PRIORITÉ 3 : MOUVEMENT INTELLIGENT (SÉCURISÉ)
            if (distance > 1.5f) // ✅ Cohérent avec JobDriver (1.2f + marge)
            {
                var jobDef = DefDatabase<JobDef>.GetNamed("Expedition33_BossMovement", false);
                if (jobDef != null && pawn.CanReach(target2, PathEndMode.Touch, Danger.Deadly))
                {
                    Log.Message($"[Boss AI] {pawn.LabelShort} → Mouvement intelligent vers {target2.LabelShort} (distance: {distance:F1})");
                    Job moveJob = JobMaker.MakeJob(jobDef, target2);
                    moveJob.expiryInterval = 400;
                    return new ThinkResult(moveJob, this);
                }
                else
                {
                    // Fallback vers Goto standard
                    Job gotoJob = JobMaker.MakeJob(JobDefOf.Goto, target2.Position);
                    gotoJob.expiryInterval = 300;
                    Log.Message($"[Boss AI] {pawn.LabelShort} → Goto standard vers {target2.LabelShort}");
                    return new ThinkResult(gotoJob, this);
                }
            }

            // ✅ FALLBACK ROBUSTE - PLUS de "Situation non gérée"
            Log.Message($"[Boss AI] {pawn.LabelShort} - Recherche d'action alternative");

            // 1. Mouvement vers position tactique
            var tacticalPos = GetTacticalPosition(pawn, target2);
            if (tacticalPos.IsValid)
            {
                Job tacticalMoveJob = JobMaker.MakeJob(JobDefOf.Goto, tacticalPos);
                tacticalMoveJob.expiryInterval = 200;
                Log.Message($"[Boss AI] {pawn.LabelShort} → Mouvement tactique");
                return new ThinkResult(tacticalMoveJob, this);
            }

            // 2. Mouvement aléatoire de déblocage
            var randomDirection = new IntVec3(Rand.Range(-4, 5), 0, Rand.Range(-4, 5));
            var emergencyPos = pawn.Position + randomDirection;
            if (emergencyPos.InBounds(pawn.Map) && emergencyPos.Walkable(pawn.Map))
            {
                Job emergencyMoveJob = JobMaker.MakeJob(JobDefOf.Goto, emergencyPos);
                emergencyMoveJob.expiryInterval = 150;
                Log.Message($"[Boss AI] {pawn.LabelShort} → Mouvement d'urgence");
                return new ThinkResult(emergencyMoveJob, this);
            }

            // 3. Attente ultra-courte en dernier recours
            Job finalWaitJob = JobMaker.MakeJob(JobDefOf.Wait_Combat);
            finalWaitJob.expiryInterval = 30; // Très court pour réévaluer rapidement
            Log.Message($"[Boss AI] {pawn.LabelShort} → Attente courte");
            return new ThinkResult(finalWaitJob, this);
        }


        // ✅ NOUVELLE MÉTHODE AUXILIAIRE
        private IntVec3 GetTacticalPosition(Pawn pawn, Pawn target)
        {
            if (target == null) return IntVec3.Invalid;

            // Position à 3-5 cases du target
            var direction = (target.Position - pawn.Position).ToVector3().normalized;
            for (int dist = 3; dist <= 5; dist++)
            {
                var testPos = pawn.Position + new IntVec3(
                    (int)(direction.x * dist),
                    0,
                    (int)(direction.z * dist)
                );

                if (testPos.InBounds(pawn.Map) &&
                    testPos.Walkable(pawn.Map) &&
                    pawn.CanReach(testPos, PathEndMode.OnCell, Danger.Deadly))
                {
                    return testPos;
                }
            }

            return IntVec3.Invalid;
        }









        // ✅ MÉTHODES STATIQUES PUBLIQUES

      public static List<BossAbility> GetAbilitiesForBoss(Pawn pawn)
        {
            return pawn.kindDef.defName switch
            {
                "NevronDechut" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 25f, false),
                },
                // Boss existants
                "Expedition33_Eveque" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 25f, false),
                    new BossAbility("IndigoBlast", 25f, false),
                    new BossAbility("DivinePurification", 0f, true),
                    new BossAbility("SacredLight", 15f, false)
                },

                "Expedition33_Goblu" => new List<BossAbility>
                {
                    new BossAbility("RapidMovement", 0f, true),
                    new BossAbility("PrimalHeal", 0f, true), // Ajout du soin
                },

                "Expedition33_SakapatateRobuste" => new List<BossAbility>
                {
                    // new BossAbility("RapidMovement", 0f, true),
                    new BossAbility("HeavyArmor", 0f, true),
                    new BossAbility("AreaAttacks", 30f, false),
                    new BossAbility("CannonShot", 25f, false),
                },

                "Expedition33_SakapatateUltime" => new List<BossAbility>
                {
                    new BossAbility("HeavyArmor", 0f, true),
                    new BossAbility("SlowRegeneration", 0f, true),
                    new BossAbility("EnergyShields", 0f, true),
                    new BossAbility("AreaAttacks", 30f, false),
                    new BossAbility("CannonShot", 30f, false),

                },

                "Expedition33_Dualiste" => new List<BossAbility>
                {
                    new BossAbility("ClairStrike", 25f, false),
                    new BossAbility("ObscurBlast", 30f, false),
                    new BossAbility("DualityShift", 0f, true)
                },

                "Expedition33_Francois" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 25f, false),
                    new BossAbility("LethalStrike", 25f, false),
                    new BossAbility("FearAura", 0f, true)
                },

                // NOUVEAUX BOSS
                "Expedition33_MaitreDesLampes" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 25f, false),
                    new BossAbility("LightManipulation", 30f, false),
                    new BossAbility("IlluminationAttacks", 35f, false),
                    new BossAbility("LampMastery", 0f, true)
                },

                "Expedition33_Renoir" => new List<BossAbility>
                {
                    new BossAbility("CommanderAura", 0f, true),
                    new BossAbility("TacticalStrike", 40f, false),
                    new BossAbility("ExpeditionMemory", 0f, true)
                },

                "Expedition33_Sirene" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 30f, false),
                    new BossAbility("SonicWave", 40f, false),
                    new BossAbility("MindControl", 25f, false)
                },

                "Expedition33_Visages" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 30f, false),
                    new BossAbility("FaceShift", 0f, true),
                    new BossAbility("RealityDistortion", 0f, true)
                },

                "Expedition33_Paintress" => new List<BossAbility>
                {
                    new BossAbility("RangeAttack", 35f, false),
                    new BossAbility("GommageRitual", 0f, true),
                    new BossAbility("CosmicBrush", 35f, false),
                    new BossAbility("NumberInscription", 20f, false),
                    new BossAbility("MonolithPower", 0f, true),
                    new BossAbility("NevronPainting", 0f, true),
                },

                "Expedition33_Mime" => new List<BossAbility>
                {
                    new BossAbility("SilenceAura", 0f, true),
                    new BossAbility("InvisibleBarriers", 25f, false)
                },

                "Expedition33_Corrompu" => new List<BossAbility>
                {
                    new BossAbility("CorruptedRage", 0f, true)
                },

                _ => new List<BossAbility>()
            };
        }

        public static bool IsAbilityAvailable(Pawn pawn, BossAbility ability)
        {
            var cooldownTicks = GetAbilityCooldownTicks(ability.name);
            var lastUsed = GetLastAbilityUseTickSafe(pawn, ability.name);
            bool isReady = Find.TickManager.TicksGame - lastUsed >= cooldownTicks;
            return isReady;
        }

        public static int GetAbilityCooldownTicks(string abilityName)
        {
            return abilityName switch
            {
                "RangeAttack" => 100,
                "IndigoBlast" => 3000,
                "DivinePurification" => 2000,
                "SacredLight" => 2000,
                "RapidMovement" => 3000,
                "PrimalHeal" => 4000,
                "ClairStrike" => 2400,
                "ObscurBlast" => 3600,
                "DualityShift" => 4800,
                "LethalStrike" => 1500,
                "FearAura" => 6000,
                "HeavyArmor" => 9000,
                "CannonShot" => 1000,
                "AreaAttacks" => 2000,
                "SlowRegeneration" => 1800,
                "EnergyShields" => 2000,
                "VersoImmortalityFragment" => 2400,
                "VersoFractureParfaite" => 1800,
                "JudgmentDay" => 5000,
                "LightManipulation" => 3000,
                "SonicWave" => 3000,
                "MindControl" => 4000,

                "IlluminationAttacks" => 2500,
                "LampMastery" => 6000,
                "CommanderAura" => 6000,
                "ExpeditionMemory" => 8000,
                "TacticalStrike" => 2200,
                "FaceShift" => 4500,
                "RealityDistortion" => 6500,
                "GommageRitual" => 8000,
                "CosmicBrush" => 4000,
                "NumberInscription" => 2800,
                "CorruptedRage" => 7500,
                "SilenceAura" => 5000,
                "InvisibleBarriers" => 4200,
                "NevronPainting" => 10000,
                "MonolithPower" => 18000,
                _ => 2000
            };
        }

        public static int GetLastAbilityUseTickSafe(Pawn caster, string abilityName)
        {
            try
            {
                if (lastAbilityUsage.TryGetValue($"{caster.ThingID}_{abilityName}", out int lastTick))
                {
                    return lastTick;
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public static BossAbility SelectBestAbility(Pawn pawn, List<BossAbility> availableAbilities)
        {
            // Protection contre listes vides ou nulles
            if (availableAbilities == null || !availableAbilities.Any())
            {
                Log.Warning($"[Boss AI] SelectBestAbility: Aucune capacité disponible pour {pawn.LabelShort}");
                return null;
            }

            // Filtrer les abilities nulles
            availableAbilities = availableAbilities.Where(a => a != null).ToList();
            if (!availableAbilities.Any())
            {
                Log.Warning($"[Boss AI] SelectBestAbility: Toutes les capacités sont nulles pour {pawn.LabelShort}");
                return null;
            }

            float hp = pawn.health.summaryHealth.SummaryHealthPercent;
            var enemies = GetNearbyEnemies(pawn, 100f);
            var allies = GetNearbyAllies(pawn, 10f);

            bool hasAnyEnemy = pawn.Map.mapPawns.FreeColonists.Any(p => !p.Downed && !p.Dead);

            // ✅ NOUVEAU : Priorité absolue pour RapidMovement de Goblu
            if (pawn.kindDef.defName == "Expedition33_Goblu")
            {
                var rapidMovement = availableAbilities.FirstOrDefault(a => a.name == "RapidMovement");
                if (rapidMovement != null)
                {
                    Log.Message($"[DEBUG] {pawn.LabelShort} utilise RapidMovement prioritaire");
                    return rapidMovement;
                }
            }

            if (pawn.kindDef.defName == "Expedition33_Paintress")
            {
                var nevronPainting = availableAbilities.FirstOrDefault(a => a.name == "NevronPainting");
                if (nevronPainting != null && Rand.Chance(1.0f)) // 30% de chance quand disponible
                {
                    // Log.Message($"[DEBUG] {pawn.LabelShort} utilise NevronPainting prioritaire");
                    return nevronPainting;
                }
            }

            if (pawn.kindDef.defName == "Expedition33_SakapatateUltime")
            {
                var heavyArmor = availableAbilities.FirstOrDefault(a => a.name == "HeavyArmor");
                if (heavyArmor != null && hp < 0.9f) // Utiliser dès que la santé baisse
                {
                    Log.Message($"[DEBUG] {pawn.LabelShort} utilise HeavyArmor prioritaire (HP: {hp:P1})");
                    return heavyArmor;
                }
            }


           



            if (hasAnyEnemy)
            {
                var rangedAbilities = availableAbilities.Where(a => !IsMeleeAbility(a) && a.name != "DivinePurification" && a.name != "PrimalHeal").ToList();
                var supportAbilities = availableAbilities.Where(a => a.selfCast || a.name == "DivinePurification" || a.name == "PrimalHeal").ToList();

                if (rangedAbilities.Any())
                {
                    var bestRanged = rangedAbilities
                    .Where(a => CanUseAbilityWithinRange(pawn, a))
                    .OrderByDescending(a => GetAbilityPriority(a, pawn, hp, enemies, allies))
                    .FirstOrDefault();
                    if (bestRanged != null && CanUseAbilitySafely(pawn, bestRanged, enemies))
                    {
                        Log.Message($"[DEBUG] {pawn.LabelShort} sélectionne capacité à distance : {bestRanged.name}");
                        return bestRanged;
                    }
                }

                if (hp < 0.6f && supportAbilities.Any())
                {
                    var bestSupport = supportAbilities
                        .OrderByDescending(a => GetAbilityPriority(a, pawn, hp, enemies, allies))
                        .FirstOrDefault();
                    if (bestSupport != null)
                    {
                        Log.Message($"[DEBUG] {pawn.LabelShort} sélectionne capacité de support : {bestSupport.name}");
                        return bestSupport;
                    }
                }
            }


            foreach (var ability in availableAbilities.OrderByDescending(a => GetAbilityPriority(a, pawn, hp, enemies, allies)))
            {
                if (CanUseAbilitySafely(pawn, ability, enemies))
                    return ability;
            }

            return null;
        }



        // ✅ NOUVELLE MÉTHODE
        private static bool CanUseAbilityWithinRange(Pawn pawn, BossAbility ability)
        {
            if (ability.name == "RangeAttack")
            {
                // RangeAttack seulement si un ennemi est à portée (15 cases)
                var nearbyEnemies = pawn.Map.mapPawns.FreeColonists
                    .Where(p => !p.Downed && !p.Dead)
                    .Where(p => pawn.Position.DistanceTo(p.Position) <= ability.range)
                    .ToList();

                return nearbyEnemies.Any();
            }

            return true; // Autres capacités non affectées
        }


        public static Thing FindTarget(Pawn pawn, BossAbility ability)
        {
            
              // Enhanced safety checks
            if (pawn?.Map?.mapPawns?.FreeColonists == null || ability == null)
            {
                Log.Warning($"[Boss AI] FindTarget: Critical null reference - pawn:{pawn != null}, map:{pawn?.Map != null}, ability:{ability != null}");
                return null;
            }

            if (ability.selfCast) return pawn;

            // Additional safety for position access
            if (!pawn.Spawned || pawn.Position == IntVec3.Invalid)
            {
                Log.Warning($"[Boss AI] FindTarget: Invalid pawn position for {pawn.LabelShort}");
                return null;
            }

            switch (ability.name)
            {
                case "RangeAttack":
                    var allEnemies = pawn.Map.mapPawns.FreeColonists
                        .Where(p => !p.Downed && !p.Dead)
                        .OrderBy(p => pawn.Position.DistanceTo(p.Position))
                        .ToList();

                    var closeTarget = allEnemies.FirstOrDefault(e => pawn.Position.DistanceTo(e.Position) <= ability.range);
                    if (closeTarget != null)
                        return closeTarget;

                    return allEnemies.FirstOrDefault();

                case "IndigoBlast":
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed &&
                            pawn.Position.DistanceTo(p.Position) > 5f
                    );

                case "SacredLight":
                    var injuredAlly = GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction == pawn.Faction &&
                            p.health.summaryHealth.SummaryHealthPercent < 0.9f
                    );
                    if (injuredAlly != null) return injuredAlly;
                    goto default;

                case "LethalStrike":
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.Touch,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );

                case "ClairStrike":
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );

                // ✅ NOUVELLES CAPACITÉS AJOUTÉES

                case "CannonShot":
                    // Cible la plus éloignée dans la portée (artillerie longue distance)
                    return pawn.Map.mapPawns.FreeColonists
                        .Where(p => !p.Downed && !p.Dead)
                        .Where(p => pawn.Position.DistanceTo(p.Position) <= ability.range)
                        .OrderByDescending(p => pawn.Position.DistanceTo(p.Position))
                        .FirstOrDefault();

                case "LightManipulation":
                case "IlluminationAttacks":
                    // Capacités de manipulation de lumière - cible ennemis moyennement distants
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed &&
                            pawn.Position.DistanceTo(p.Position) >= 3f
                    );

                case "TacticalStrike":
                    // Frappe tactique - cible l'ennemi le plus dangereux (le mieux armé/blindé)
                    var tacticalTargets = pawn.Map.mapPawns.FreeColonists
                        .Where(p => !p.Downed && !p.Dead)
                        .Where(p => pawn.Position.DistanceTo(p.Position) <= ability.range)
                        .ToList();

                    return tacticalTargets
                        .OrderByDescending(p => p.equipment?.Primary?.def?.IsMeleeWeapon == false ? 1 : 0) // Prioriser les tireurs
                        .ThenByDescending(p => p.apparel?.WornApparel?.Count ?? 0) // Puis les bien équipés
                        .FirstOrDefault();

                case "SonicWave":
                    // Onde sonique - efficace contre les groupes
                    var sonicEnemies = GetNearbyEnemies(pawn, ability.range);
                    if (sonicEnemies.Count >= 2)
                    {
                        var sonicCenter = sonicEnemies.OrderByDescending(e =>
                            sonicEnemies.Count(other => e.Position.DistanceTo(other.Position) <= 4f)
                        ).FirstOrDefault();
                        return sonicCenter;
                    }
                    goto default;

                case "MindControl":
                    // Contrôle mental - cible l'ennemi le plus fort
                    return pawn.Map.mapPawns.FreeColonists
                        .Where(p => !p.Downed && !p.Dead)
                        .Where(p => pawn.Position.DistanceTo(p.Position) <= ability.range)
                        .OrderByDescending(p => p.health.summaryHealth.SummaryHealthPercent)
                        .ThenByDescending(p => p.skills?.GetSkill(SkillDefOf.Shooting)?.Level ?? 0)
                        .FirstOrDefault();

                case "CosmicBrush":
                    // Pinceau cosmique - cible longue portée, évite le corps à corps
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed &&
                            pawn.Position.DistanceTo(p.Position) > 8f
                    );

                case "NumberInscription":
                    // Inscription de nombres - cible aléatoire pour effet mystique
                    var inscriptionTargets = pawn.Map.mapPawns.FreeColonists
                        .Where(p => !p.Downed && !p.Dead)
                        .Where(p => pawn.Position.DistanceTo(p.Position) <= ability.range)
                        .ToList();

                    return inscriptionTargets.RandomElementWithFallback();

                case "InvisibleBarriers":
                    // Barrières invisibles - cible l'ennemi le plus proche pour bloquer son approche
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );

                case "FractureParfaite":
                case "VersoFractureParfaite":
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction != pawn.Faction && !p.Downed && !p.Dead
                    );

                case "ObscurBlast":
                case "AreaAttacks":
                    var enemies = GetNearbyEnemies(pawn, ability.range);
                    if (enemies.Count >= 2)
                    {
                        var centerEnemy = enemies.OrderByDescending(e =>
                            enemies.Count(other => e.Position.DistanceTo(other.Position) <= 3f)
                        ).FirstOrDefault();
                        return centerEnemy;
                    }
                    goto default;

                default:
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );
            }
        }



        public static List<Pawn> GetNearbyEnemies(Pawn pawn, float radius)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(pawn.Position) <= radius &&
                           p.Faction != pawn.Faction &&
                           p.Faction?.IsPlayer == true &&
                           !p.Downed)
                .ToList();
        }

        public static List<Pawn> GetNearbyAllies(Pawn pawn, float radius)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(pawn.Position) <= radius &&
                           p.Faction == pawn.Faction &&
                           p != pawn &&
                           !p.Downed)
                .ToList();
        }

        public static bool IsMeleeAbility(BossAbility ability)
        {
            return ability.name switch
            {
                "LethalStrike" => true,
                "ClairStrike" => true,
                _ => false
            };
        }

        public static float GetAbilityPriority(BossAbility ability, Pawn pawn, float hp, List<Pawn> enemies, List<Pawn> allies)
        {
            return ability.name switch
            {
                // Capacités de soins - priorité haute si santé basse
                "PrimalHeal" => hp < 0.3f ? 15f : 1f,
                "DivinePurification" => hp < 0.5f ? 12f : 2f,
                
                // Capacités offensives puissantes
                "IndigoBlast" => enemies.Count >= 2 ? 10f : 6f,
                "ObscurBlast" => enemies.Count >= 2 ? 9f : 5f,
                "LethalStrike" => enemies.Any(e => pawn.Position.DistanceTo(e.Position) <= 3f) ? 8f : 3f,
                
                // Capacités à distance
                "RangeAttack" => 7f,
                "ClairStrike" => 6f,
                "SacredLight" => allies.Any(a => a.health.summaryHealth.SummaryHealthPercent < 0.7f) ? 8f : 3f,
                
                // Capacités utilitaires
                "RapidMovement" => enemies.Any(e => pawn.Position.DistanceTo(e.Position) <= 5f) ? 4f : 2f,
                "DualityShift" => 5f,
                "FearAura" => enemies.Count >= 3 ? 7f : 4f,
                
                // Capacités défensives
                "HeavyArmor" => hp < 0.6f ? 10f : 4f,
                "EnergyShields" => enemies.Count >= 2 ? 7f : 3f,
                
                // Capacités Verso
                "VersoImmortalityFragment" => hp < 0.2f ? 20f : 1f,
                "VersoFractureParfaite" => 11f,

                "NevronPainting" => enemies.Any() ? 8f : 5f,
                
                // Défaut
                _ => 5f
            };
        }

        public static bool CanUseAbilitySafely(Pawn pawn, BossAbility ability, List<Pawn> enemies)
        {
            if (ability.selfCast) return true;

            var nearestEnemy = enemies.OrderBy(e => pawn.Position.DistanceTo(e.Position)).FirstOrDefault();
            if (nearestEnemy == null) return false;

            float distanceToTarget = pawn.Position.DistanceTo(nearestEnemy.Position);

            if (IsMeleeAbility(ability))
            {
                return distanceToTarget <= ability.range;
            }
            else
            {
                // ✅ CORRECTION : Permettre les capacités à distance même si proche
                return distanceToTarget <= ability.range; // Supprimer la condition >= 5f
            }
        }

        


        public static void RecordAbilityUsage(Pawn pawn, BossAbility ability)
        {
            string key = $"{pawn.ThingID}_{ability.name}";

            bool isNewlySpawned = pawn.ageTracker.AgeBiologicalTicks < 600;

            if (isNewlySpawned)
            {
                lastAbilityUsage[key] = Find.TickManager.TicksGame - GetAbilityCooldownTicks(ability.name);
                Log.Message($"[DEBUG] {pawn.LabelShort} - premier usage de {ability.name} sans cooldown");
            }
            else
            {
                lastAbilityUsage[key] = Find.TickManager.TicksGame;
            }
        }

        // ✅ MÉTHODES D'INSTANCE PRIVÉES
        public static bool IsBoss(Pawn pawn)
        {
            // ✅ EXCLUSION EXPLICITE de Verso
            if (pawn?.kindDef?.defName == "Expedition_Verso" ||
                pawn?.def?.defName == "Expedition_VersoColon" ||
                pawn.LabelShort == "Verso")
                return false;

            // Votre logique boss existante
            var extension = pawn?.kindDef?.GetModExtension<BossExtension>();
            return extension != null && extension.isLegendaryEntity;
        }



        private bool ShouldInterruptCurrentJob(Pawn pawn)
        {
            bool isNewlySpawned = pawn.ageTracker.AgeBiologicalTicks < 900;
            if (!isNewlySpawned) return false;

            var currentJob = pawn.jobs?.curJob;
            if (currentJob == null) return false;

            bool isBasicJob = currentJob.def == JobDefOf.AttackMelee ||
                             currentJob.def == JobDefOf.Goto ||
                             currentJob.def == JobDefOf.AttackStatic ||
                             currentJob.def == JobDefOf.Wait ||
                             currentJob.def == JobDefOf.Wait_Combat;

            if (isBasicJob)
            {
                Log.Message($"[DEBUG] {pawn.LabelShort} - job basique {currentJob.def.defName} interrompu IMMÉDIATEMENT");
                return true;
            }

            return false;
        }

        private void EnsureImmediateAggression(Pawn pawn)
        {
            if (pawn.mindState != null)
            {
                pawn.mindState.anyCloseHostilesRecently = true;
                pawn.mindState.canFleeIndividual = false;

                var allColonists = pawn.Map.mapPawns.FreeColonists
                    .Where(p => !p.Downed && !p.Dead)
                    .OrderBy(p => pawn.Position.DistanceTo(p.Position))
                    .ToList();

                if (allColonists.Any())
                {
                    var nearestColonist = allColonists.First();
                    
                    if (pawn.mindState.enemyTarget == null || 
                        (pawn.mindState.enemyTarget as Pawn)?.Dead == true || 
                        (pawn.mindState.enemyTarget as Pawn)?.Downed == true)
                    {
                        pawn.mindState.enemyTarget = nearestColonist;
                        Log.Message($"[DEBUG] {pawn.LabelShort} cible forcée sur {nearestColonist.LabelShort}");
                    }
                }
            }
        }

        private ThinkResult ForceBasicCombat(Pawn pawn)
        {
            var target = FindBestTarget(pawn);

            if (target != null && target is Pawn targetPawn && !targetPawn.Dead && !targetPawn.Downed)
            {
                float distance = pawn.Position.DistanceTo(targetPawn.Position);

                // Utiliser le job de déplacement intelligent au lieu d'AttackMelee
                Job smartMovement = JobMaker.MakeJob(
                    DefDatabase<JobDef>.GetNamed("Expedition33_BossMovement"),
                    targetPawn
                );

                return new ThinkResult(smartMovement, this);
            }

            return ThinkResult.NoJob;
        }
        private static Pawn FindBestTarget(Pawn boss)
        {
            var allTargets = boss.Map.mapPawns.FreeColonists
                .Where(p => !p.Downed && !p.Dead)
                .ToList();

            if (!allTargets.Any()) return null;

            // Prioriser selon la distance et la santé
            return allTargets
                .OrderBy(p => boss.Position.DistanceTo(p.Position))
                .ThenByDescending(p => p.health.summaryHealth.SummaryHealthPercent)
                .FirstOrDefault();
        }


        public static void ForceTargetReevaluation(Pawn pawn)
        {
            if (pawn?.jobs?.curJob == null) return;

            var currentJob = pawn.jobs.curJob;

            // Si c'est un job de déplacement, le rediriger
            if (currentJob.def.defName == "Expedition33_BossMovement" ||
                currentJob.def == JobDefOf.Goto)
            {
                var currentTarget = currentJob.targetA.Thing as Pawn;
                var newTarget = FindBestTarget(pawn);

                // Seulement changer si la nouvelle cible est différente ET plus proche
                if (newTarget != null && newTarget != currentTarget)
                {
                    float currentDistance = currentTarget != null ?
                        pawn.Position.DistanceTo(currentTarget.Position) : float.MaxValue;
                    float newDistance = pawn.Position.DistanceTo(newTarget.Position);

                    if (newDistance < currentDistance - 3f) // Seuil de 3 cases pour éviter le flickering
                    {
                        Log.Message($"[Boss AI] {pawn.LabelShort} change de cible: {currentTarget?.LabelShort} → {newTarget.LabelShort}");
                        currentJob.SetTarget(TargetIndex.A, newTarget);
                        pawn.pather.StartPath(newTarget.Position, PathEndMode.Touch);
                    }
                }
            }
        }

        
[DebugAction("Expedition33", "Debug Verso", allowedGameStates = AllowedGameStates.Playing)]
private static void DebugVerso()
{
    var verso = Find.CurrentMap.mapPawns.AllPawnsSpawned
        .FirstOrDefault(p => p.kindDef?.defName == "Expedition_Verso");
    
    if (verso == null)
    {
        Log.Error("Verso non trouvé sur la carte");
        return;
    }
    
    Log.Message("=== DIAGNOSTIC VERSO ===");
    Log.Message($"Nom: {verso.LabelShort}");
    Log.Message($"Faction: {verso.Faction?.Name}");
    Log.Message($"IsColonistPlayerControlled: {verso.IsColonistPlayerControlled}");
    Log.Message($"Drafted: {verso.Drafted}");
    Log.Message($"IsBoss: {IsBoss(verso)}");
    Log.Message($"ThinkTreeMain: {verso.RaceProps.thinkTreeMain?.defName}");
    Log.Message($"CurrentJob: {verso.jobs?.curJob?.def?.defName}");
    Log.Message($"CanMove: {verso.health.capacities.CapableOf(PawnCapacityDefOf.Moving)}");
    
    // Test de composants
    foreach (var comp in verso.AllComps)
    {
        Log.Message($"Composant: {comp.GetType().Name}");
    }
}



    }

    
}
