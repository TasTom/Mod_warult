using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using System;

namespace Mod_warult
{
    public class ThinkNode_AbilityUser : ThinkNode_Priority
    {
        public enum BossPhase
        {
            Phase1_Normal,      // 100% - 70% santé
            Phase2_Aggressive,  // 70% - 40% santé  
            Phase3_Desperate    // 40% - 0% santé
        }


        // Nouveau dictionnaire pour l'historique des capacités
        private static readonly Dictionary<int, List<(string ability, int tick)>> abilityHistory = new();

        private static readonly Dictionary<int, Dictionary<string, int>> abilityLastUsedTick = new();

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            if (IsBoss(pawn) && pawn?.Name != null && !(pawn.Name is NameTriple))
            {
                try
                {
                    BossNameManager.InitializeBossName(pawn);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"[Expedition33] Erreur lors de l'initialisation du nom de boss : {ex.Message}");
                }
            }


            if (!IsBoss(pawn)) return ThinkResult.NoJob;

            // Récupérer les capacités disponibles (non en cooldown)
            var availableAbilities = GetAvailableAbilities(pawn);
            if (!availableAbilities.Any()) return ThinkResult.NoJob;


            // Sélection intelligente de la capacité
            var ability = SelectBestAbility(pawn, availableAbilities);
            if (ability == null) return ThinkResult.NoJob;

            // Trouver une cible appropriée
            Thing target = FindTarget(pawn, ability);
            if (target == null) return ThinkResult.NoJob;

            // Créer le job avec le système existant
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Expedition33_ExecuteAbility"));
            job.SetTarget(TargetIndex.A, target);
            job.verbToUse = new Verb_ExecuteAbility { ability = ability, caster = pawn };

            // Enregistrer l'utilisation pour le cooldown
            RecordAbilityUsage(pawn, ability);

            return new ThinkResult(job, this);
        }

        private List<BossAbility> GetAvailableAbilities(Pawn pawn)
        {
            var allAbilities = GetAbilitiesForBoss(pawn);
            var available = new List<BossAbility>();

            foreach (var ability in allAbilities)
            {
                if (IsAbilityAvailable(pawn, ability))
                {
                    available.Add(ability);
                }
            }

            return available;
        }

        private bool IsAbilityAvailable(Pawn pawn, BossAbility ability)
        {
            if (!abilityLastUsedTick.ContainsKey(pawn.thingIDNumber))
                return true;

            var pawnCooldowns = abilityLastUsedTick[pawn.thingIDNumber];
            if (!pawnCooldowns.ContainsKey(ability.name))
                return true;

            int lastUsed = pawnCooldowns[ability.name];
            int cooldownTicks = GetAbilityCooldownTicks(ability);

            return GenTicks.TicksGame - lastUsed >= cooldownTicks;
        }

        private int GetAbilityCooldownTicks(BossAbility ability)
        {
            var abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail($"Expedition33_{ability.name}");
            if (abilityDef?.cooldownTicksRange != null)
            {
                return abilityDef.cooldownTicksRange.min;
            }

            return ability.name switch
            {
                "SacredLight" => 1800,           // 30 secondes
                "DivinePurification" => 3600,    // 60 secondes
                "IndigoBlast" => 4800,           // 80 secondes
                "RapidMovement" => 1200,         // 20 secondes
                "HeavyArmor" => 2400,            // 40 secondes
                "SlowRegeneration" => 3600,      // 60 secondes
                "EnergyShields" => 4800,         // 80 secondes
                "AreaAttacks" => 3600,           // 60 secondes
                "LightManipulation" => 2400,     // 40 secondes
                "IlluminationAttacks" => 1800,  // 30 secondes
                "LampMastery" => 6000,           // 100 secondes
                "ClairStrike" => 1200,           // 20 secondes
                "ObscurBlast" => 1800,           // 30 secondes
                "DualityShift" => 2400,          // 40 secondes
                "LethalStrike" => 7200,          // 120 secondes
                "FearAura" => 3600,              // 60 secondes
                "CommanderAura" => 2400,         // 40 secondes
                "TacticalStrike" => 1800,        // 30 secondes
                "ExpeditionMemory" => 6000,      // 100 secondes
                "SonicWave" => 3000,             // 50 secondes
                "MindControl" => 9000,           // 150 secondes
                "FaceShift" => 4800,             // 80 secondes
                "RealityDistortion" => 7200,     // 120 secondes
                "GommageRitual" => 12000,        // 200 secondes
                "CosmicBrush" => 6000,           // 100 secondes
                "NumberInscription" => 4800,     // 80 secondes
                "MonolithPower" => 18000,        // 300 secondes
                "ImmortalityFragment" => 7200,   // 120 secondes
                "FractureParfaite" => 4800,      // 80 secondes
                "CorruptedRage" => 1800,         // 30 secondes
                "SilenceAura" => 2400,           // 40 secondes
                "InvisibleBarriers" => 3600,     // 60 secondes
                _ => 600 // 10 secondes par défaut
            };
        }

        private void RecordAbilityUsage(Pawn pawn, BossAbility ability)
        {
            if (!abilityLastUsedTick.ContainsKey(pawn.thingIDNumber))
            {
                abilityLastUsedTick[pawn.thingIDNumber] = new Dictionary<string, int>();
            }

            abilityLastUsedTick[pawn.thingIDNumber][ability.name] = GenTicks.TicksGame;

            // Nouveau : Enregistrer dans l'historique
            if (!abilityHistory.ContainsKey(pawn.thingIDNumber))
            {
                abilityHistory[pawn.thingIDNumber] = new List<(string, int)>();
            }

            abilityHistory[pawn.thingIDNumber].Add((ability.name, GenTicks.TicksGame));

            // Nettoyer l'historique ancien (garder seulement les 10 dernières)
            if (abilityHistory[pawn.thingIDNumber].Count > 10)
            {
                abilityHistory[pawn.thingIDNumber].RemoveAt(0);
            }
        }


        private BossAbility SelectBestAbility(Pawn pawn, List<BossAbility> availableAbilities)
        {
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;
            var nearbyEnemies = GetNearbyEnemies(pawn, 15f);
            var nearbyAllies = GetNearbyAllies(pawn, 10f);

            // Logique de priorité basée sur la situation
            foreach (var ability in availableAbilities.OrderByDescending(a => GetAbilityPriority(a, pawn, healthPercent, nearbyEnemies, nearbyAllies)))
            {
                if (CanUseAbilitySafely(pawn, ability, nearbyEnemies))
                {
                    return ability;
                }
            }

            return availableAbilities.FirstOrDefault();
        }

        private int GetAbilityPriority(BossAbility ability, Pawn pawn, float healthPercent, List<Pawn> enemies, List<Pawn> allies)
        {
            // Phase de combat selon la santé
            var currentPhase = GetCurrentPhase(pawn);

            // Analyse contextuelle
            int nearEnemies = enemies.Count(e => pawn.Position.DistanceTo(e.Position) <= 5f);
            int farEnemies = enemies.Count(e => pawn.Position.DistanceTo(e.Position) > 10f);
            int injuredAllies = allies.Count(a => a.health.summaryHealth.SummaryHealthPercent < 0.5f);

            // Randomisation renforcée
            int randomFactor = Rand.Range(-25, 26);

            // Modificateurs de phase et personnalité
            int phaseMod = GetPhaseModifier(pawn, ability, currentPhase);
            int personalityMod = GetBossPersonalityModifier(pawn, ability);

            // Priorité de base selon le type d'ability et contexte tactique
            int basePriority = ability.name switch
            {
                // === ÉVÊQUE ===
                "DivinePurification" when healthPercent < 0.4f => 120,
                "DivinePurification" when healthPercent < 0.7f && nearEnemies == 0 => 90,
                "SacredLight" when injuredAllies >= 2 => 100,
                "SacredLight" when nearEnemies >= 2 && allies.Any() => 85,
                "SacredLight" => 60,
                "IndigoBlast" when farEnemies >= 2 && nearEnemies == 0 => 95,
                "IndigoBlast" when enemies.Count >= 3 && nearEnemies == 0 => 80,

                // === SAKAPATATES ===
                "RapidMovement" when nearEnemies >= 3 => 110,
                "RapidMovement" when healthPercent < 0.6f => 85,
                "HeavyArmor" when nearEnemies >= 2 && healthPercent > 0.5f => 90,
                "EnergyShields" when enemies.Count >= 3 => 95,
                "AreaAttacks" when nearEnemies >= 2 => 100,
                "SlowRegeneration" when healthPercent < 0.5f && nearEnemies == 0 => 105,

                // === DUALISTE (clair-obscur) ===
                "DualityShift" when healthPercent < 0.6f || enemies.Count >= 4 => 100,
                "ClairStrike" when GetTimeOfDay(pawn.Map) == "Day" => 90,
                "ClairStrike" when enemies.Count == 1 => 85,
                "ObscurBlast" when GetTimeOfDay(pawn.Map) == "Night" => 90,
                "ObscurBlast" when nearEnemies >= 2 => 80,

                // === FRANÇOIS ===
                "FearAura" when enemies.Count >= 3 => 95,
                "LethalStrike" when enemies.Any(e => e.health.summaryHealth.SummaryHealthPercent < 0.4f) => 100,

                // === MAÎTRE DES LAMPES ===
                "LightManipulation" when enemies.Count >= 3 => 90,
                "LightManipulation" when GetTimeOfDay(pawn.Map) == "Night" => 85,
                "IlluminationAttacks" when GetTimeOfDay(pawn.Map) == "Day" => 95,
                "IlluminationAttacks" when enemies.Count >= 2 => 80,
                "LampMastery" when healthPercent < 0.5f => 100,
                "LampMastery" when enemies.Count >= 4 => 85,

                // === COMMANDANT RENOIR ===
                "CommanderAura" when allies.Count >= 2 => 95,
                "CommanderAura" when healthPercent > 0.7f => 80,
                "TacticalStrike" when allies.Count >= 1 => 90,
                "TacticalStrike" when enemies.Count >= 2 => 85,
                "ExpeditionMemory" when healthPercent < 0.4f => 110,
                "ExpeditionMemory" when enemies.Count >= 3 => 85,

                // === SIRÈNE (AXON) ===
                "SonicWave" when enemies.Count >= 2 => 100,
                "SonicWave" when farEnemies >= 1 => 85,
                "MindControl" when enemies.Count == 1 => 110,
                "MindControl" when enemies.Any(e => e.health.summaryHealth.SummaryHealthPercent > 0.8f) => 95,

                // === VISAGES (AXON) ===
                "FaceShift" when healthPercent < 0.6f => 100,
                "FaceShift" when enemies.Count >= 3 => 80,
                "RealityDistortion" when enemies.Count >= 2 => 95,
                "RealityDistortion" when healthPercent < 0.5f => 105,

                // === LA PEINTRESSE (BOSS FINAL) ===
                "GommageRitual" when healthPercent < 0.3f => 150, // Capacité ultime
                "GommageRitual" when enemies.Count >= 4 => 120,
                "CosmicBrush" when enemies.Count >= 2 => 100,
                "CosmicBrush" when farEnemies >= 1 => 85,
                "NumberInscription" when enemies.Count >= 3 => 90,
                "NumberInscription" when healthPercent < 0.6f => 80,
                "MonolithPower" when healthPercent < 0.2f => 200, // Attaque finale
                "MonolithPower" when enemies.Count >= 5 => 130,

                // === VERSO ===
                "ImmortalityFragment" when healthPercent < 0.25f => 80,
                "FractureParfaite" when enemies.Any(e => e.health.summaryHealth.SummaryHealthPercent > 0.8f) => 100,
                "FractureParfaite" when enemies.Count >= 2 => 85,

                // === LE MIME ===
                "SilenceAura" when enemies.Count >= 3 => 95,
                "SilenceAura" when nearEnemies >= 2 => 80,
                "InvisibleBarriers" when enemies.Count >= 2 => 90,
                "InvisibleBarriers" when healthPercent < 0.6f => 85,

                // === BOSS CORROMPU ===
                "CorruptedRage" when healthPercent < 0.5f => 100,
                "CorruptedRage" when nearEnemies >= 2 => 85,

                // Par défaut
                _ => 30
            };

            // Pénalité si la même capacité a été utilisée récemment
            int recentPenalty = GetRecentAbilityUsage(pawn, ability.name) * 15;

            // Calcul final
            return Math.Max(10, basePriority + phaseMod + personalityMod + randomFactor - recentPenalty);
        }









        private bool CanUseAbilitySafely(Pawn pawn, BossAbility ability, List<Pawn> enemies)
        {
            switch (ability.name)
            {
                case "IndigoBlast":
                    // Vérifications de sécurité renforcées
                    var dangerZone = enemies.Where(e => pawn.Position.DistanceTo(e.Position) <= 8f);
                    if (dangerZone.Any())
                    {
                        // Permettre si le boss est très blessé (désespoir)
                        if (pawn.health.summaryHealth.SummaryHealthPercent < 0.3f && enemies.Count >= 3)
                        {
                            Log.Message($"[Expedition33] {pawn.LabelShort} utilise IndigoBlast en désespoir de cause !");
                            return true;
                        }
                        return false;
                    }

                    var target = FindTarget(pawn, ability);
                    return target != null && pawn.Position.DistanceTo(target.Position) > 10f;

                case "DivinePurification":
                    // Ne pas se soigner si entouré d'ennemis (sauf urgence)
                    var nearbyThreats = enemies.Where(e => pawn.Position.DistanceTo(e.Position) <= 3f);
                    if (nearbyThreats.Count() >= 2 && pawn.health.summaryHealth.SummaryHealthPercent > 0.2f)
                        return false;
                    return true;

                case "RapidMovement":
                    // Vérifier qu'il y a un endroit sûr où aller
                    var safeCells = GenRadial.RadialCellsAround(pawn.Position, 8f, true)
                        .Where(c => c.InBounds(pawn.Map) && c.Walkable(pawn.Map) &&
                                   !enemies.Any(e => e.Position.DistanceTo(c) <= 4f))
                        .ToList();
                    return safeCells.Any();

                default:
                    return true;
            }
        }





        private static bool IsBoss(Pawn pawn)
        {
            // Vérification standard pour les boss
            if (pawn?.kindDef?.defName?.StartsWith("Expedition33_") == true)
                return true;

            // Vérification spéciale pour les colons Verso
            if (pawn?.kindDef?.defName?.Contains("Verso") == true)
                return true;

            return false;
        }



        private List<BossAbility> GetAbilitiesForBoss(Pawn pawn)
        {
            return pawn.kindDef.defName switch
            {
                // Boss existants
                "Expedition33_Eveque" => new List<BossAbility>
                {
                    new BossAbility("IndigoBlast", 25f, false),
                    new BossAbility("DivinePurification", 0f, true),
                    new BossAbility("SacredLight", 15f, false)
                },

                "Expedition33_SakapatateRobuste" => new List<BossAbility>
                {
                    new BossAbility("RapidMovement", 0f, true),
                    new BossAbility("HeavyArmor", 0f, true),
                    new BossAbility("AreaAttacks", 20f, false)
                },

                "Expedition33_SakapatateUltime" => new List<BossAbility>
                {
                    new BossAbility("SlowRegeneration", 0f, true),
                    new BossAbility("EnergyShields", 0f, true),
                    new BossAbility("AreaAttacks", 12f, false)
                },

                "Expedition33_Dualiste" => new List<BossAbility>
                {
                    new BossAbility("ClairStrike", 20f, false),
                    new BossAbility("ObscurBlast", 15f, false),
                    new BossAbility("DualityShift", 0f, true)
                },

                "Expedition33_Francois" => new List<BossAbility>
                {
                    new BossAbility("LethalStrike", 8f, false),
                    new BossAbility("FearAura", 0f, true)
                },

                // NOUVEAUX BOSS
                "Expedition33_MaitreLampes" => new List<BossAbility>
                {
                    new BossAbility("LightManipulation", 25f, false),
                    new BossAbility("IlluminationAttacks", 20f, false),
                    new BossAbility("LampMastery", 0f, true)
                },

                "Expedition33_CommandantRenoir" => new List<BossAbility>
                {
                    new BossAbility("CommanderAura", 0f, true),
                    new BossAbility("TacticalStrike", 15f, false),
                    new BossAbility("ExpeditionMemory", 0f, true)
                },

                "Expedition33_Sirene" => new List<BossAbility>
                {
                    new BossAbility("SonicWave", 25f, false),
                    new BossAbility("MindControl", 18f, false)
                },

                "Expedition33_Visages" => new List<BossAbility>
                {
                    new BossAbility("FaceShift", 0f, true),
                    new BossAbility("RealityDistortion", 0f, true)
                },

                "Expedition33_Peintresse" => new List<BossAbility>
                {
                    new BossAbility("GommageRitual", 0f, true),
                    new BossAbility("CosmicBrush", 30f, false),
                    new BossAbility("NumberInscription", 20f, false),
                    new BossAbility("MonolithPower", 0f, true)
                },

                "Expedition33_Verso" => new List<BossAbility>
                {
                    new BossAbility("ImmortalityFragment", 0f, true),
                    new BossAbility("FractureParfaite", 12f, false)
                },

                        // Support pour colon Verso (nom peut varier)
                var name when name.Contains("Verso") => new List<BossAbility>
                {
                    new BossAbility("ImmortalityFragment", 0f, true),
                    new BossAbility("FractureParfaite", 12f, false)
                },
                

                "Expedition33_Mime" => new List<BossAbility>
                {
                    new BossAbility("SilenceAura", 0f, true),
                    new BossAbility("InvisibleBarriers", 15f, false)
                },

                "Expedition33_Corrompu" => new List<BossAbility>
                {
                    new BossAbility("CorruptedRage", 0f, true)
                },

                _ => new List<BossAbility>()
            };
        }


        private List<Pawn> GetNearbyEnemies(Pawn pawn, float radius)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(pawn.Position) <= radius &&
                           p.Faction != pawn.Faction &&
                           p.Faction?.IsPlayer == true &&
                           !p.Downed)
                .ToList();
        }

        private List<Pawn> GetNearbyAllies(Pawn pawn, float radius)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(pawn.Position) <= radius &&
                           p.Faction == pawn.Faction &&
                           p != pawn &&
                           !p.Downed)
                .ToList();
        }

        private Thing FindTarget(Pawn pawn, BossAbility ability)
        {
            if (ability.selfCast) return pawn;

            switch (ability.name)
            {
                case "IndigoBlast":
                    // Logique existante
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
                    // Logique existante
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
                    // Cibler l'ennemi le plus blessé à portée de mêlée
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.Touch,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );

                case "ClairStrike":
                    // Cibler l'ennemi le plus proche
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed
                    );
                 case "FractureParfaite":
                 if (pawn.Faction == Faction.OfPlayer)
                    return null;
                    return GenClosest.ClosestThingReachable(
                        pawn.Position, pawn.Map,
                        ThingRequest.ForGroup(ThingRequestGroup.Pawn),
                        PathEndMode.OnCell,
                        TraverseParms.For(pawn),
                        ability.range,
                        t => t is Pawn p && p.Faction?.IsPlayer == true && !p.Downed && p != pawn // Exclure soi-même
                    );
                case "ObscurBlast":
                case "AreaAttacks":
                    // Cibler le centre du groupe d'ennemis le plus dense
                    var enemies = GetNearbyEnemies(pawn, ability.range);
                    if (enemies.Count >= 2)
                    {
                        // Trouver le centre du groupe
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
        private BossPhase GetCurrentPhase(Pawn pawn)
        {
            float healthPercent = pawn.health.summaryHealth.SummaryHealthPercent;

            return healthPercent switch
            {
                >= 0.7f => BossPhase.Phase1_Normal,
                >= 0.4f => BossPhase.Phase2_Aggressive,
                _ => BossPhase.Phase3_Desperate
            };
        }


        private int GetPhaseModifier(Pawn pawn, BossAbility ability, BossPhase phase)
        {
            return (pawn.kindDef.defName, ability.name, phase) switch
            {
                // ÉVÊQUE
                ("Expedition33_Eveque", "SacredLight", BossPhase.Phase1_Normal) => 30,
                ("Expedition33_Eveque", "IndigoBlast", BossPhase.Phase2_Aggressive) => 40,
                ("Expedition33_Eveque", "DivinePurification", BossPhase.Phase3_Desperate) => 60,

                // DUALISTE
                ("Expedition33_Dualiste", "ClairStrike", BossPhase.Phase1_Normal) => 35,
                ("Expedition33_Dualiste", "ObscurBlast", BossPhase.Phase2_Aggressive) => 45,
                ("Expedition33_Dualiste", "DualityShift", BossPhase.Phase3_Desperate) => 70,

                // FRANÇOIS
                ("Expedition33_Francois", "FearAura", BossPhase.Phase1_Normal) => 25,
                ("Expedition33_Francois", "LethalStrike", BossPhase.Phase2_Aggressive) => 50,
                ("Expedition33_Francois", "LethalStrike", BossPhase.Phase3_Desperate) => 80,

                // MAÎTRE DES LAMPES
                ("Expedition33_MaitreLampes", "LightManipulation", BossPhase.Phase1_Normal) => 30,
                ("Expedition33_MaitreLampes", "IlluminationAttacks", BossPhase.Phase2_Aggressive) => 40,
                ("Expedition33_MaitreLampes", "LampMastery", BossPhase.Phase3_Desperate) => 70,

                // COMMANDANT RENOIR
                ("Expedition33_CommandantRenoir", "CommanderAura", BossPhase.Phase1_Normal) => 35,
                ("Expedition33_CommandantRenoir", "TacticalStrike", BossPhase.Phase2_Aggressive) => 45,
                ("Expedition33_CommandantRenoir", "ExpeditionMemory", BossPhase.Phase3_Desperate) => 80,

                // LA PEINTRESSE (Boss final avec phases extrêmes)
                ("Expedition33_Peintresse", "CosmicBrush", BossPhase.Phase1_Normal) => 40,
                ("Expedition33_Peintresse", "NumberInscription", BossPhase.Phase2_Aggressive) => 50,
                ("Expedition33_Peintresse", "GommageRitual", BossPhase.Phase3_Desperate) => 100,
                ("Expedition33_Peintresse", "MonolithPower", BossPhase.Phase3_Desperate) => 150,

                // VERSO
                ("Expedition33_Verso", "FractureParfaite", BossPhase.Phase2_Aggressive) => 40,
                ("Expedition33_Verso", "ImmortalityFragment", BossPhase.Phase3_Desperate) => 60,

                _ => 0
            };
        }




        private int GetBossPersonalityModifier(Pawn pawn, BossAbility ability)
        {
            return pawn.kindDef.defName switch
            {
                "Expedition33_Eveque" => ability.name switch
                {
                    "DivinePurification" => 20,  // Évêque préfère se soigner
                    "SacredLight" => 15,         // Aime aider ses alliés
                    "IndigoBlast" => -10,        // Réticent aux attaques destructrices
                    _ => 0
                },

                "Expedition33_Francois" => ability.name switch
                {
                    "LethalStrike" => 25,        // François est agressif
                    "FearAura" => 20,            // Aime terroriser
                    _ => 0
                },

                "Expedition33_Dualiste" => ability.name switch
                {
                    "DualityShift" => 30,        // Adore changer de forme
                    "ClairStrike" when Rand.Chance(0.5f) => 15,  // Imprévisible
                    "ObscurBlast" when Rand.Chance(0.5f) => 15,
                    _ => 0
                },

                "Expedition33_MaitreLampes" => ability.name switch
                {
                    "LightManipulation" => 25,   // Maître de la lumière
                    "LampMastery" => 20,         // Fier de ses lampes
                    _ => 0
                },

                "Expedition33_CommandantRenoir" => ability.name switch
                {
                    "CommanderAura" => 30,       // Leader naturel
                    "TacticalStrike" => 20,      // Stratège
                    _ => 0
                },

                "Expedition33_Sirene" => ability.name switch
                {
                    "SonicWave" => 25,           // Pouvoir signature
                    "MindControl" => 15,         // Manipulation mentale
                    _ => 0
                },

                "Expedition33_Peintresse" => ability.name switch
                {
                    "GommageRitual" => 40,       // Capacité ultime favorite
                    "MonolithPower" => 35,       // Pouvoir final
                    "CosmicBrush" => 20,         // Artiste cosmique
                    _ => 0
                },

                "Expedition33_Verso" => ability.name switch
                {
                    "ImmortalityFragment" => 30, // Immortel par nature
                    "FractureParfaite" => 25,    // Technique signature
                    _ => 0
                },

                "Expedition33_Mime" => ability.name switch
                {
                    "SilenceAura" => 35,         // Essence du mime
                    "InvisibleBarriers" => 20,   // Illusions invisibles
                    _ => 0
                },

                _ => 0
            };
        }




        private int GetRecentAbilityUsage(Pawn pawn, string abilityName)
        {
            if (!abilityHistory.ContainsKey(pawn.thingIDNumber))
                return 0;

            var pawnHistory = abilityHistory[pawn.thingIDNumber];
            int currentTick = GenTicks.TicksGame;

            // Compter les utilisations récentes
            int recentUsage = pawnHistory.Count(h => h.ability == abilityName && currentTick - h.tick < 1200);

            return recentUsage;
        }

                // private int GetRecentAbilityUsage(Pawn pawn, string abilityName)
        // {
        //     if (!abilityLastUsedTick.ContainsKey(pawn.thingIDNumber))
        //         return 0;

        //     var pawnHistory = abilityLastUsedTick[pawn.thingIDNumber];
        //     if (!pawnHistory.ContainsKey(abilityName))
        //         return 0;

        //     int ticksSinceUse = GenTicks.TicksGame - pawnHistory[abilityName];

        //     // Plus c'est récent, plus la pénalité est forte
        //     if (ticksSinceUse < 300) return 3;      // < 5 secondes
        //     if (ticksSinceUse < 600) return 2;      // < 10 secondes  
        //     if (ticksSinceUse < 1200) return 1;     // < 20 secondes

        //     return 0;
        // }

        private string GetTimeOfDay(Map map)
        {
            var currentHour = GenLocalDate.HourOfDay(map);
            return currentHour >= 6 && currentHour <= 18 ? "Day" : "Night";
        }



    }
}
