using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;
using System;

namespace Mod_warult
{
    public static class AbilityExecuter
    {
        // === CAPACITÉS DE L'ÉVÊQUE ===

        public static void ExecuteIndigoBlast(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;
            float distanceToCaster = caster.Position.DistanceTo(targetCell);
            
            if (distanceToCaster <= 6.5f)
            {
                Log.Warning("Expedition33_IndigoBlastTooClose".Translate(caster.LabelShort));
                return;
            }

            GenExplosion.DoExplosion(
                center: targetCell,
                map: caster.Map,
                radius: 6f,
                damType: DamageDefOf.Bomb,
                instigator: caster,
                damAmount: 40,
                armorPenetration: 0.8f,
                explosionSound: SoundDefOf.VoidNode_Explode,
                ignoredThings: new List<Thing> { caster },
                chanceToStartFire: 0f,
                damageFalloff: true
            );

            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowDustPuffThick(targetCell.ToVector3Shifted(),
                    caster.Map, 4f, new Color(0.3f, 0f, 0.8f));
            }


        }

        public static void ExecuteDivinePurification(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_DivineRegeneration", HediffDefOf.NeuralSupercharge);

            for (int i = 0; i < 20; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.white);
            }

            Messages.Message($"✨ {caster.LabelShort} purifies itself !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteSacredLight(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 2f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null)
                    {
                        if (pawn.Faction == caster.Faction)
                        {
                            SafeAddHediff(pawn, "Expedition33_SacredHealing", HediffDefOf.WorkFocus);
                        }
                        else
                        {
                            SafeAddHediff(pawn, "Expedition33_SacredBlindness", HediffDefOf.Blindness);
                        }
                    }

                    FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), caster.Map, 1.5f);
                }
            }

            // Messages.Message($"🌟 {caster.LabelShort} projette une lumière sacrée !",
            //     MessageTypeDefOf.ThreatBig);
        }

        // === CAPACITÉS DES SAKAPATATES ===

        public static void ExecuteRapidMovement(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_RapidMovementBuff", HediffDefOf.MetalhorrorSpeedBoost);

            // NOUVEAU : Dégâts aux ennemis proches lors de l'activation
            var nearbyEnemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction &&
                           p.Position.DistanceTo(caster.Position) <= 3f &&
                           !p.Downed)
                .ToList();

            foreach (var enemy in nearbyEnemies)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 25, 1.0f, -1, caster);
                enemy.TakeDamage(dinfo);
            }

            // Effets visuels améliorés
            for (int i = 0; i < 20; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 4f, Color.cyan);
            }

            // Messages.Message($"⚡ {caster.LabelShort} explose de vitesse et repousse les ennemis !",
            //     MessageTypeDefOf.ThreatBig);
        }


        public static void ExecuteHeavyArmor(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_HeavyArmorBuff", HediffDefOf.GhoulBarbs);

            // NOUVEAU : Dégâts de riposte aux attaquants proches
            var nearbyEnemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction &&
                           p.Position.DistanceTo(caster.Position) <= 2f &&
                           !p.Downed)
                .ToList();

            foreach (var enemy in nearbyEnemies)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, 15, 1.5f, -1, caster);
                enemy.TakeDamage(dinfo);

                FilthMaker.TryMakeFilth(enemy.Position, caster.Map, ThingDefOf.Filth_Blood, 1);
            }

            // Effets visuels métalliques
            for (int i = 0; i < 12; i++)
            {
                FleckMaker.ThrowLightningGlow(caster.Position.ToVector3Shifted(), caster.Map, 2f);
            }

            Messages.Message($"🛡️ {caster.LabelShort} activates his armor and damages nearby attackers !",
                MessageTypeDefOf.ThreatBig);
        }


        public static void ExecuteAreaAttacks(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 5f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 25, 1.2f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }
                    FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), caster.Map, 1f);
                }
            }

            // Messages.Message($"💥 {caster.LabelShort} effectue une attaque de zone !",
            //     MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteSlowRegeneration(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_SlowRegenerationBuff", HediffDefOf.RapidRegeneration);

            for (int i = 0; i < 12; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 2f, Color.green);
            }

            // Messages.Message($"✨ {caster.LabelShort} commence une régénération lente !",
            //     MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteEnergyShields(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_EnergyShieldsBuff", HediffDefOf.GhoulPlating);

            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowLightningGlow(caster.Position.ToVector3Shifted(), caster.Map, 2f);
            }

            // Messages.Message($"🔰 {caster.LabelShort} active ses boucliers énergétiques !",
            //     MessageTypeDefOf.NeutralEvent);
        }

        // === CAPACITÉS DU MAÎTRE DES LAMPES ===

        public static void ExecuteLightManipulation(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            // Créer des zones de lumière et d'ombre alternées
            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 5f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    // Alternance entre zones sombres et lumineuses
                    bool isDarkZone = (cell.x + cell.z) % 2 == 0;

                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        if (isDarkZone)
                        {
                            SafeAddHediff(pawn, "Expedition33_DarknessEffect", HediffDefOf.Blindness);
                        }
                        else
                        {
                            SafeAddHediff(pawn, "Expedition33_LightEffect", HediffDefOf.LightExposure);
                        }
                    }

                    Color effectColor = isDarkZone ? Color.black : Color.white;
                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 2f, effectColor);
                }
            }

            // Messages.Message($"🔆 {caster.LabelShort} manipule la lumière !",
            //     MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteIlluminationAttacks(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 40, 2.0f, -1, caster);
                pawnTarget.TakeDamage(dinfo);

                SafeAddHediff(pawnTarget, "Expedition33_IlluminationBurn", HediffDefOf.Blindness);

                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 3f);

                // Messages.Message($"☀️ {caster.LabelShort} aveugle {pawnTarget.LabelShort} avec un faisceau lumineux !",
                //     MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteLampMastery(Pawn caster)
        {
            // Créer des "lampes spectrales" autour du caster
            var lampPositions = GenRadial.RadialCellsAround(caster.Position, 4f, true)
                .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map))
                .Take(6);

            foreach (var pos in lampPositions)
            {
                // Effet visuel de lampe
                FleckMaker.ThrowLightningGlow(pos.ToVector3Shifted(), caster.Map, 2f);

                // Dégâts aux ennemis proches de chaque "lampe"
                foreach (var enemy in GenRadial.RadialCellsAround(pos, 2f, true))
                {
                    var pawn = enemy.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 15, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }

            Messages.Message($"🏮 {caster.LabelShort} summons spectral lamps !",
                MessageTypeDefOf.ThreatBig);
        }

        // === CAPACITÉS DU DUALISTE ===

        public static void ExecuteClairStrike(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget)
            {
                var currentHour = GenLocalDate.HourOfDay(caster.Map);
                bool isDaytime = currentHour >= 6 && currentHour <= 18;

                if (isDaytime)
                {
                    // Mode CLAIR - Dégâts de lumière pure
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 60, 3.0f, -1, caster);
                    pawnTarget.TakeDamage(dinfo);

                    SafeAddHediff(pawnTarget, "Expedition33_LightBurn", HediffDefOf.Blindness);

                    // Effets visuels éblouissants
                    for (int i = 0; i < 15; i++)
                    {
                        FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 4f);
                    }

                    Messages.Message($"☀️ {caster.LabelShort} hit {pawnTarget.LabelShort} with the power of the sun !",
                        MessageTypeDefOf.ThreatBig);
                }
                else
                {
                    // Mode OBSCUR - Dégâts de ténèbres
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 45, 2.5f, -1, caster);
                    pawnTarget.TakeDamage(dinfo);

                    SafeAddHediff(pawnTarget, "Expedition33_DarknessCurse", HediffDefOf.DarkPsychicShock);

                    // Effets visuels sombres
                    for (int i = 0; i < 15; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(pawnTarget.Position.ToVector3Shifted(), caster.Map, 3f, Color.black);
                    }

                    // Messages.Message($"🌑 {caster.LabelShort} maudit {pawnTarget.LabelShort} avec les ténèbres !",
                    //     MessageTypeDefOf.ThreatBig);
                }
            }
        }





        public static void ExecuteObscurBlast(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            GenExplosion.DoExplosion(
                center: targetCell,
                map: caster.Map,
                radius: 4f,
                damType: DamageDefOf.EMP,
                instigator: caster,
                damAmount: 35,
                armorPenetration: 0.6f,
                explosionSound: SoundDefOf.EnergyShield_Reset,
                chanceToStartFire: 0f
            );

            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 4f, true))
            {
                var pawn = cell.GetFirstPawn(caster.Map);

                // ✅ CORRECTION : Cibler seulement les colons humains
                if (pawn != null &&
                    pawn.Faction?.IsPlayer == true && // Faction du joueur
                    pawn.RaceProps.Humanlike && // Humanoïde seulement
                    !pawn.Downed && !pawn.Dead) // Vivant et conscient
                {
                    SafeAddHediff(pawn, "Expedition33_ObscurEffect", HediffDefOf.DarkPsychicShock);
                }
            }

            for (int i = 0; i < 12; i++)
            {
                FleckMaker.ThrowDustPuffThick(targetCell.ToVector3Shifted(),
                    caster.Map, 3f, Color.black);
            }
        }



        public static void ExecuteDualityShift(Pawn caster)
        {
            try
            {
                if (caster?.Map == null)
                {
                    Log.Error("[DualityShift] Caster ou map nulle");
                    return;
                }

                // Déterminer l'état actuel
                var currentHour = GenLocalDate.HourOfDay(caster.Map);
                bool isDaytime = currentHour >= 6 && currentHour <= 18;

                // Explosion de transformation
                GenExplosion.DoExplosion(
                    center: caster.Position,
                    map: caster.Map,
                    radius: 10f,
                    damType: DamageDefOf.EMP,
                    instigator: caster,
                    damAmount: 40,
                    armorPenetration: 1.0f,
                    explosionSound: SoundDefOf.EnergyShield_Reset,
                    ignoredThings: new List<Thing> { caster }
                );

                // Effet d'inversion jour/nuit temporaire
                if (isDaytime)
                {
                    CreateDarknessZones(caster);
                    Messages.Message($"🌑 {caster.LabelShort} plunges the area into darkness !",
                        MessageTypeDefOf.ThreatBig);
                }
                else
                {
                    CreateLightZones(caster);
                    Messages.Message($"☀️ {caster.LabelShort} illuminates the area with a blinding light !",
                        MessageTypeDefOf.ThreatBig);
                }

                // ✅ CORRECTION : Transformation du boss avec le bon hediff
                string targetHediff = isDaytime ? "Expedition33_DarkMode" : "Expedition33_LightMode";
                SafeAddHediff(caster, "Expedition33_DarknessDebuff", HediffDef.Named(targetHediff));

                // Buff personnel du boss
                SafeAddHediff(caster, "Expedition33_DarknessDebuff", HediffDef.Named("Expedition33_DualityShiftBuff"));

                // Téléportation tactique
                TeleportToOptimalPosition(caster);

                // Effets visuels spectaculaires
                for (int i = 0; i < 40; i++)
                {
                    Color shiftColor = i % 2 == 0 ? Color.white : Color.black;
                    FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                        caster.Map, 5f, shiftColor);
                }

                Log.Message($"[DualityShift] {caster.LabelShort} activated Duality Shift ({(isDaytime ? "Dark" : "Light")} mode)");
            }
            catch (Exception ex)
            {
                Log.Error($"[DualityShift] Erreur: {ex}");
            }
        }



        private static void CreateDarknessZones(Pawn caster)
        {
            var darkZones = GenRadial.RadialCellsAround(caster.Position, 30f, true);

            foreach (var cell in darkZones)
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        SafeAddHediff(pawn, "Expedition33_DarknessDebuff", HediffDefOf.Blindness);

                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 20, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }

                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 3f, Color.black);
                }
            }
        }


        private static void CreateLightZones(Pawn caster)
        {
            var lightZones = GenRadial.RadialCellsAround(caster.Position, 30f, true);

            foreach (var cell in lightZones)
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        SafeAddHediff(pawn, "Expedition33_LightBurn", HediffDefOf.LightExposure);

                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 25, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }

                    FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), caster.Map, 3f);
                }
            }
        }


        private static void TeleportToOptimalPosition(Pawn caster)
        {
            try
            {
                if (caster?.Map == null)
                {
                    Log.Warning("[Teleport] Caster ou map nulle");
                    return;
                }

                // ✅ Tous les pawns vivants de la faction du joueur
                var targets = caster.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p.Faction == Faction.OfPlayer &&
                               !p.Downed &&
                               p.Spawned &&
                               p != caster && // Ne pas se téléporter sur soi-même
                               (p.RaceProps.Humanlike || p.RaceProps.Animal)) // Humains ou animaux
                    .ToList();

                if (!targets.Any())
                {
                    Log.Message($"[Teleport] Aucune cible trouvée pour {caster.LabelShort}");
                    return;
                }

                var target = targets.RandomElement();

                // ✅ Chercher une position valide autour de la cible
                var teleportPos = GenRadial.RadialCellsAround(target.Position, 3f, true)
                    .Where(c => c.InBounds(caster.Map) &&
                               c.Walkable(caster.Map) &&
                               c.Standable(caster.Map) &&
                               !c.Roofed(caster.Map)) // Éviter les toits
                    .OrderBy(c => c.DistanceTo(target.Position))
                    .FirstOrDefault();

                if (teleportPos.IsValid)
                {
                    caster.Position = teleportPos;
                    FleckMaker.ThrowLightningGlow(teleportPos.ToVector3Shifted(), caster.Map, 4f);
                    Log.Message($"[Teleport] {caster.LabelShort} teleported near {target.LabelShort}");
                }
                else
                {
                    Log.Warning($"[Teleport] Aucune position valide trouvée près de {target.LabelShort}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[Teleport] Erreur: {ex}");
            }
        }








        public static void ExecuteJudgmentDay(Pawn caster)
        {
            var allEnemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction && !p.Downed)
                .ToList();

            foreach (var enemy in allEnemies)
            {
                int judgmentDamage = Rand.Range(50, 100);
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, judgmentDamage, 4.0f, -1, caster);
                enemy.TakeDamage(dinfo);

                for (int i = 0; i < 20; i++)
                {
                    FleckMaker.ThrowLightningGlow(enemy.Position.ToVector3Shifted(), caster.Map, 6f);
                }
            }

            Messages.Message($"⚖️ The bishop {caster.LabelShort} pronounces the LAST JUDGMENT !",
                MessageTypeDefOf.ThreatBig);
        }


        // === CAPACITÉS DE FRANÇOIS ===

        public static void ExecuteLethalStrike(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.SurgicalCut, 65, 3.5f, -1, caster);
                pawnTarget.TakeDamage(dinfo);

                FilthMaker.TryMakeFilth(pawnTarget.Position, caster.Map, ThingDefOf.Filth_Blood, 3);

                FleckMaker.ThrowDustPuffThick(pawnTarget.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.red);

                Messages.Message($"☠️ {caster.LabelShort} deals a mortal blow to{pawnTarget.LabelShort} !",
                    MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteFearAura(Pawn caster)
        {
            var enemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction &&
                        p.Position.DistanceTo(caster.Position) <= 10f &&
                        !p.Downed)
                .ToList();

            foreach (var enemy in enemies)
            {
                SafeAddHediff(enemy, "Expedition33_FearAuraHediff", HediffDefOf.PsychicSuppression);
                
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 30, 1.0f, -1, caster);
                enemy.TakeDamage(dinfo);

                if (Rand.Chance(0.4f))
                {
                    enemy.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }

                FleckMaker.ThrowDustPuffThick(enemy.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.red);
            }

            Messages.Message("Expedition33_FearAuraActivated".Translate(caster.LabelShort), 
                MessageTypeDefOf.ThreatBig);
        }

        // === CAPACITÉS DU COMMANDANT RENOIR ===

        public static void ExecuteCommanderAura(Pawn caster)
        {
            var allies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction == caster.Faction &&
                           p.Position.DistanceTo(caster.Position) <= 15f &&
                           !p.Downed && p != caster)
                .ToList();

            foreach (var ally in allies)
            {
                SafeAddHediff(ally, "Expedition33_CommanderBoost", HediffDefOf.NeuralSupercharge);

                FleckMaker.ThrowDustPuffThick(ally.Position.ToVector3Shifted(),
                    caster.Map, 2f, Color.yellow);
            }

            // Messages.Message($"⚔️ {caster.LabelShort} booste ses alliés !",
            //     MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteTacticalStrike(Pawn caster, Thing target)
        {
            var nearbyAllies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction == caster.Faction &&
                           p.Position.DistanceTo(caster.Position) <= 10f &&
                           !p.Downed && p != caster)
                .Count();

            int bonusDamage = nearbyAllies * 5;
            int totalDamage = 30 + bonusDamage;

            if (target is Pawn pawnTarget)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, totalDamage, 2.0f, -1, caster);
                pawnTarget.TakeDamage(dinfo);

                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 2f);

                Messages.Message($"🎯 {caster.LabelShort} coordinates a tactical strike({totalDamage} damage) !",
                    MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteExpeditionMemory(Pawn caster)
        {
            if (caster == null || caster.Map == null)
                return;

            Map map = caster.Map;

            // Récupère la faction cible hostile (ex : "Nevrons")
            Faction hostileFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Nevrons");
            if (hostileFaction == null)
            {
                // Log.Error("[ExecuteExpeditionMemory] Faction 'Nevrons' introuvable !");
                return;
            }

            // Détermine combien de Nevrons à spawn
            int count = Rand.RangeInclusive(2, 5);

            // Récupère tous les colons joueurs valides
            var colonists = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != null && p.Faction.IsPlayer && !p.Dead && p.Spawned)
                .ToList();

            if (colonists.Count == 0)
            {
                // Messages.Message("[Expedition33] Aucun colon trouvé pour l'invocation !", caster, MessageTypeDefOf.NeutralEvent);
                return;
            }

            var spawnPositions = new List<IntVec3>();

            // Pour chaque Nevron à spawn, choisis un colon aléatoire comme centre
            for (int i = 0; i < count; i++)
            {
                Pawn colonTarget = colonists.RandomElement();

                // Cherche une case à 15 cases de distance autour du colon, libre et marchable
                var cells = GenRadial.RadialCellsAround(colonTarget.Position, 15f, true)
                    .Where(c => c.InBounds(map) && c.Walkable(map) && c.GetFirstPawn(map) == null)
                    .ToList();

                if (cells.Any())
                    spawnPositions.Add(cells.RandomElement());
            }

            if (spawnPositions.Count == 0)
            {
                // Messages.Message("Aucune case valide à 15 cases d'un colon pour spawn un Nevron.", caster, MessageTypeDefOf.NeutralEvent);
                return;
            }

            int actuallySpawned = 0;
            foreach (var pos in spawnPositions)
            {
                FleckMaker.ThrowDustPuffThick(pos.ToVector3Shifted(), map, 2.5f, Color.gray);

                var request = new PawnGenerationRequest(
                    PawnKindDef.Named("Nevron_Basic"),
                    hostileFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true);

                var nevron = PawnGenerator.GeneratePawn(request);
                GenSpawn.Spawn(nevron, pos, map);
                FleckMaker.ThrowSmoke(pos.ToVector3Shifted(), map, 1.8f);

                // if (nevron.mindState != null && nevron.RaceProps.Animal)
                // {
                //     nevron.mindState.mentalStateHandler.TryStartMentalState(
                //         MentalStateDefOf.ManhunterPermanent,
                //         reason: "Rage invoquée",
                //         forceWake: true,
                //         causedByMood: false);
                // }

                actuallySpawned++;
            }

            // Messages.Message(
            //     $"👻 {caster.LabelShort} invoque {actuallySpawned} Nevron(s) enragé(s) à 15 cases des colons !",
            //     caster,
            //     MessageTypeDefOf.ThreatBig
            // );
        }


        public static void ExecuteNevronPainting(Pawn caster)
        {
            if (caster == null || caster.Map == null)
                return;

            Map map = caster.Map;

            // Récupère la faction cible hostile (ex : "Nevrons")
            Faction hostileFaction = Find.FactionManager.AllFactions.FirstOrDefault(f => f.def.defName == "Nevrons");
            if (hostileFaction == null)
            {
                // Log.Error("[ExecuteExpeditionMemory] Faction 'Nevrons' introuvable !");
                return;
            }

            // Détermine combien de Nevrons à spawn
            int count = Rand.RangeInclusive(2, 3);

            // Récupère tous les colons joueurs valides
            var colonists = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != null && p.Faction.IsPlayer && !p.Dead && p.Spawned)
                .ToList();

            if (colonists.Count == 0)
            {
                // Messages.Message("[Expedition33] Aucun colon trouvé pour l'invocation !", caster, MessageTypeDefOf.NeutralEvent);
                return;
            }

            var spawnPositions = new List<IntVec3>();

            // Pour chaque Nevron à spawn, choisis un colon aléatoire comme centre
            for (int i = 0; i < count; i++)
            {
                Pawn colonTarget = colonists.RandomElement();

                // Cherche une case à 15 cases de distance autour du colon, libre et marchable
                var cells = GenRadial.RadialCellsAround(colonTarget.Position, 15f, true)
                    .Where(c => c.InBounds(map) && c.Walkable(map) && c.GetFirstPawn(map) == null)
                    .ToList();

                if (cells.Any())
                    spawnPositions.Add(cells.RandomElement());
            }

            if (spawnPositions.Count == 0)
            {
                // Messages.Message("Aucune case valide à 15 cases d'un colon pour spawn un Nevron.", caster, MessageTypeDefOf.NeutralEvent);
                return;
            }

            int actuallySpawned = 0;
            foreach (var pos in spawnPositions)
            {
                FleckMaker.ThrowDustPuffThick(pos.ToVector3Shifted(), map, 2.5f, Color.gray);

                var request = new PawnGenerationRequest(
                    PawnKindDef.Named("Nevron_Basic"),
                    hostileFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true);

                var request2 = new PawnGenerationRequest(
                    PawnKindDef.Named("Amphorien"),
                    hostileFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true);

                var request3 = new PawnGenerationRequest(
                    PawnKindDef.Named("Pitank"),
                    hostileFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true);

                var request4 = new PawnGenerationRequest(
                    PawnKindDef.Named("NevronDechut"),
                    hostileFaction,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: true);


                var nevron = PawnGenerator.GeneratePawn(request);
                 var nevron2 = PawnGenerator.GeneratePawn(request2);
                  var nevron3 = PawnGenerator.GeneratePawn(request3);
                   var nevron4 = PawnGenerator.GeneratePawn(request4);
                GenSpawn.Spawn(nevron, pos, map);
                GenSpawn.Spawn(nevron2, pos, map);
                GenSpawn.Spawn(nevron3, pos, map);
                GenSpawn.Spawn(nevron4, pos, map);
                FleckMaker.ThrowSmoke(pos.ToVector3Shifted(), map, 1.8f);

                actuallySpawned++;
            }
          
        }





        // === CAPACITÉS DES AXONS ===

        public static void ExecuteSonicWave(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;
            Vector3 direction = (targetCell - caster.Position).ToVector3().normalized;

            // Onde sonique en ligne droite
            for (int i = 1; i <= 25; i++)
            {
                IntVec3 waveCell = caster.Position + IntVec3.FromVector3(direction * i);
                if (!waveCell.InBounds(caster.Map)) break;

                var pawn = waveCell.GetFirstPawn(caster.Map);
                if (pawn != null && pawn.Faction != caster.Faction)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 50, 1.5f, -1, caster);
                    pawn.TakeDamage(dinfo);

                    SafeAddHediff(pawn, "Expedition33_SonicStun", HediffDefOf.PsychicShock);
                }

                FleckMaker.ThrowDustPuffThick(waveCell.ToVector3Shifted(), caster.Map, 1f, Color.cyan);
            }

            // Messages.Message($"🌊 {caster.LabelShort} émet une onde sonique dévastatrice !",
            //     MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteMindControl(Pawn caster, Thing target)
        {
            float radius = 35f;
            Map map = caster.Map;

            var targets = GenRadial.RadialCellsAround(caster.Position, radius, true)
                .Where(c => c.InBounds(map))
                .Select(c => c.GetFirstPawn(map))
                .Where(p => p != null && p.Faction != caster.Faction && p.RaceProps?.Humanlike == true)
                .Distinct();

            int affected = 0;
            foreach (var pawnTarget in targets)
            {
                SafeAddHediff(pawnTarget, "Expedition33_MindControlled", HediffDefOf.PsychicTrance);

                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), map, 3.5f);
                FleckMaker.ThrowMicroSparks(pawnTarget.Position.ToVector3Shifted(), map);

                // Facultatif : petit stun
                pawnTarget.stances.stunner.StunFor(60, target);

                affected++;
            }

            FleckMaker.Static(caster.Position, map, FleckDefOf.PsycastAreaEffect, 6f);

            Messages.Message(
                $"🧠 {caster.LabelShort} takes mental control of{affected} surrounding target(s) !",
                caster, MessageTypeDefOf.ThreatBig
            );
        }



        public static void ExecuteFaceShift(Pawn caster)
        {
            Map map = caster.Map;
            SafeAddHediff(caster, "Expedition33_FaceShiftBuff", HediffDefOf.PsychicTrance);

            for (int i = 0; i < 25; i++)
            {
                Vector3 spawn = caster.DrawPos + Rand.InsideUnitCircleVec3 * 3f;
                FleckMaker.ThrowDustPuffThick(spawn, map, 2.4f, Color.magenta);
                FleckMaker.ThrowSmoke(spawn, map, 1.5f);
                FleckMaker.ThrowMicroSparks(spawn, map);
                FleckMaker.ThrowDustPuffThick(spawn, map, 2f, Color.magenta);
            }

            // Affecte ennemis proches (désorientation temporaire)
            foreach (var pawn in GenRadial.RadialCellsAround(caster.Position, 4f, true)
                .Where(c => c.InBounds(map))
                .Select(c => c.GetFirstPawn(map))
                .Where(p => p != null && p.Faction != caster.Faction))
            {
                SafeAddHediff(pawn, "Expedition33_IdentityConfusion", HediffDefOf.PsychicShock);
                FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), map, 1.5f);
            }

            Messages.Message($"🎭 {caster.LabelShort} changes face and sows confusion !",
                caster, MessageTypeDefOf.ThreatBig);
        }



        public static void ExecuteRealityDistortion(Pawn caster)
        {
            Map map = caster.Map;
            float radius = 40f;

            int victims = 0;
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, radius, true))
            {
                if (!cell.InBounds(map)) continue;

                // Effets visuels de chaos aléatoire
                Color chaosColor = new Color(Rand.Value, Rand.Value, Rand.Value);
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, Rand.Range(1.2f, 2f), chaosColor);
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 1.5f);

                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null && pawn.Faction != caster.Faction)
                {
                    SafeAddHediff(pawn, "Expedition33_RealityDistorted", HediffDefOf.PsychicShock);

                    // Dégâts mentaux symboliques
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 8, 1, -1, caster);
                    pawn.TakeDamage(dinfo);

                    // Probabilité d'état mental
                    if (Rand.Chance(0.2f))
                    {
                        pawn.mindState?.mentalStateHandler?.TryStartMentalState(MentalStateDefOf.PanicFlee, "Perturbation de la réalité", false, false);
                    }

                    victims++;
                }
            }

            FleckMaker.Static(caster.Position, map, FleckDefOf.PsycastAreaEffect, 7f);

            Messages.Message(
                $"🌀 {caster.LabelShort} disrupts the reality of {victims} victim(s) in a distorted radius !",
                caster, MessageTypeDefOf.ThreatBig);
        }



        // === CAPACITÉS DE LA Paintress ===

        public static void ExecuteGommageRitual(Pawn caster)
        {
            int targetAge = Rand.Range(18, 80);

            var targets = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction &&
                           p.ageTracker.AgeBiologicalYears == targetAge)
                .ToList();

            foreach (var target in targets)
            {
                SafeAddHediff(target, "Expedition33_MonolithCurse", HediffDefOf.ToxicBuildup);

                FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.red);
            }

            Messages.Message($"📝 {caster.LabelShort} write age {targetAge} - warning !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteCosmicBrush(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            // Créer une zone de danger qui se déplace
            var dangerCells = GenRadial.RadialCellsAround(targetCell, 4f, true);

            foreach (var cell in dangerCells)
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 30, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }

                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 2f, Color.magenta);
                }
            }

            Messages.Message($"🎨 {caster.LabelShort} painted a cosmic danger zone !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteNumberInscription(Pawn caster, Thing target)
        {
            int randomNumber = Rand.Range(1, 100);
            IntVec3 targetCell = target.Position;

            // Effets selon le nombre inscrit
            if (randomNumber <= 25)
            {
                // Nombre bas : dégâts faibles mais zone large
                foreach (var cell in GenRadial.RadialCellsAround(targetCell, 6f, true))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 15, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }
            else if (randomNumber >= 75)
            {
                // Nombre élevé : dégâts élevés mais zone réduite
                foreach (var cell in GenRadial.RadialCellsAround(targetCell, 2f, true))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Bomb, 60, 2.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }

            FleckMaker.ThrowLightningGlow(targetCell.ToVector3Shifted(), caster.Map, 3f);

            Messages.Message($"🔢 {caster.LabelShort} write the number {randomNumber} !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteMonolithPower(Pawn caster)
        {
            // Attaque dévastatrice finale
            var allEnemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction && !p.Downed)
                .ToList();

            foreach (var enemy in allEnemies)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Bomb, 100, 3.0f, -1, caster);
                enemy.TakeDamage(dinfo);

                FleckMaker.ThrowLightningGlow(enemy.Position.ToVector3Shifted(), caster.Map, 4f);
            }

            // Effets visuels massifs
            for (int i = 0; i < 50; i++)
            {
                IntVec3 randomCell = caster.Map.AllCells.RandomElement();
                FleckMaker.ThrowDustPuffThick(randomCell.ToVector3Shifted(), caster.Map, 5f, Color.black);
            }

            Messages.Message($"⚫ {caster.LabelShort} Channel the power of the Monolith! TOTAL DESTRUCTION!",
                MessageTypeDefOf.ThreatBig);
        }

        // === CAPACITÉS SPÉCIALES ===

        public static void ExecuteImmortalityFragment(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_ImmortalityBuff", HediffDefOf.RapidRegeneration);

            for (int i = 0; i < 25; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.yellow);
            }

            // Messages.Message($"♾️ {caster.LabelShort} utilise son immortalité !",
            //     MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteFractureParfaite(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget)
            {
                // Ignore complètement l'armure
                DamageInfo dinfo = new DamageInfo(DamageDefOf.SurgicalCut, 80, 10.0f, -1, caster);
                pawnTarget.TakeDamage(dinfo);

                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 3f);

                Messages.Message($"💎 {caster.LabelShort} brise parfaitement les défenses de {pawnTarget.LabelShort} !",
                    MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteCorruptedRage(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_CorruptedRageBuff", HediffDefOf.BloodRage);

            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 2f, Color.red);
            }

            // Messages.Message($"😡 {caster.LabelShort} entre dans une rage corrompue !",
            //     MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteSilenceAura(Pawn caster)
        {
            var affectedPawns = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Position.DistanceTo(caster.Position) <= 8f &&
                           p.Faction != caster.Faction)
                .ToList();

            foreach (var pawn in affectedPawns)
            {
                SafeAddHediff(pawn, "Expedition33_SilenceEffect", HediffDefOf.PsychicSuppression);

                // NOUVEAU : Dégâts d'étouffement
                DamageInfo dinfo = new DamageInfo(DamageDefOf.ToxGas, 30, 1.0f, -1, caster);
                pawn.TakeDamage(dinfo);

                FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3Shifted(),
                    caster.Map, 2f, Color.gray);
            }

            // Messages.Message($"🤐 {caster.LabelShort} étouffe ses ennemis dans le silence !",
            //     MessageTypeDefOf.ThreatBig);
        }


        public static void ExecuteInvisibleBarriers(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;

            // NOUVEAU : Créer de vraies barrières temporaires avec des murs
            var barrierCells = GenRadial.RadialCellsAround(targetCell, 3f, true)
                .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map))
                .Take(12);

            foreach (var cell in barrierCells)
            {
                // Dégâts aux pawns sur les cellules de barrière
                var pawn = cell.GetFirstPawn(caster.Map);
                if (pawn != null && pawn.Faction != caster.Faction)
                {
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, 35, 2.0f, -1, caster);
                    pawn.TakeDamage(dinfo);

                    SafeAddHediff(pawn, "Expedition33_BarrierCrush", HediffDefOf.Anesthetic);
                }

                // Effets visuels de barrière
                FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), caster.Map, 1.5f);
            }

            // Messages.Message($"🚧 {caster.LabelShort} écrase ses ennemis avec des barrières invisibles !",
            //     MessageTypeDefOf.ThreatBig);
        }

        // === MÉTHODE UTILITAIRE ===

        private static void SafeAddHediff(Pawn pawn, string hediffDefName, HediffDef fallbackHediff = null)
        {
            var hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail(hediffDefName);
            if (hediffDef != null)
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }
            else if (fallbackHediff != null)
            {
                var hediff = HediffMaker.MakeHediff(fallbackHediff, pawn);
                pawn.health.AddHediff(hediff);
                Log.Warning($"[Expedition33] Utilisation de {fallbackHediff.defName} à la place de {hediffDefName} manquant");
            }
            else
            {
                Log.Error($"[Expedition33] HediffDef manquant : {hediffDefName}");
            }
        }
        public static void ExecuteRangeAttack(Pawn caster, Thing target)
        {
            if (target == null || caster == null || caster.Map == null || !(target is Pawn))
                return;

            // ✅ AJOUT : Vérification de la ligne de vue
            if (!GenSight.LineOfSight(caster.Position, target.Position, caster.Map, true))
            {
                Log.Message($"[RangeAttack] {caster.LabelShort} can't see {target.LabelShort} - attack cancelled.");
                return;
            }

            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("Projectile_EnergyWaveBullet");
            if (projectileDef == null) return;

            // Spawn le projectile à la position caster, MAIS ne lance pas directement le projectile
            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, caster.Position, caster.Map);

            // Lance le projectile correctement AVEC le caster comme launcher
            projectile.Launch(
                caster,              // lanceur
                caster.DrawPos,      // position de départ
                target,              // cible utilisée (LocalTargetInfo)
                target,              // cible intentionnelle (LocalTargetInfo)
                ProjectileHitFlags.IntendedTarget
            );

            FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), caster.Map, 1.2f, Color.white);
        }





        public static void ExecutePrimalHeal(Pawn caster)
        {
            // Applique le Hediff "Expedition33_SlowRegeneration"
            HediffDef hediff = HediffDef.Named("Expedition33_SlowRegeneration");
            if (caster.health.hediffSet.HasHediff(hediff))
            {
                // Rafraîchi la durée si le hediff existe déjà
                Hediff h = caster.health.hediffSet.GetFirstHediffOfDef(hediff);
                var comp = h.TryGetComp<HediffComp_Disappears>();
                if (comp != null) comp.ticksToDisappear = 3600;
            }
            else
            {
                Hediff hediffNew = HediffMaker.MakeHediff(hediff, caster);
                caster.health.AddHediff(hediffNew);
            }

            FleckMaker.ThrowMicroSparks(caster.DrawPos, caster.Map);
            // Messages.Message($"{caster.LabelShort} entre en régénération primitive !", caster, MessageTypeDefOf.PositiveEvent);
        }


        public static void ExecuteCannonShot(Pawn caster, Thing target)
{
    if (target == null || caster == null || caster.Map == null)
        return;

    // Vérifie que la cible est valide
    if (!(target is Pawn || target is Building))
        return;

    // Charge le ThingDef du projectile de canon
    ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("Projectile_SakapateCannon");
    if (projectileDef == null)
    {
        Log.Error("[ExecuteCannonShot] Projectile_SakapateCannon introuvable !");
        return;
    }

    // Crée et spawn le projectile sur la position du lanceur
    Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, caster.Position, caster.Map);
    
    // Lance le projectile vers la cible avec les paramètres classiques RimWorld
    projectile.Launch(
        caster,           // lanceur
        caster.DrawPos,   // position de départ (3D)
        target,           // cible utilisée (LocalTargetInfo)
        target,           // cible intentionnelle (LocalTargetInfo)
        ProjectileHitFlags.IntendedTarget
    );

    // Effet visuel optionnel au départ
    FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), caster.Map, 2.5f, Color.grey);

    // Message info facultatif
    // Messages.Message($"{caster.LabelShort} tire un puissant obus de canon !", caster, MessageTypeDefOf.ThreatBig);
}








    }
    
    public class Projectile_RangeAttackBullet : Projectile
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            base.Impact(hitThing, blockedByShield);

            if (hitThing is Pawn pawn && !pawn.Dead && !pawn.Downed)
            {
                // Appliquer ton dégât ici, avec la source comme lanceur (launcher)
                int damageAmount = 12; // Ou prends depuis des paramètres/modifie à ta guise
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, damageAmount, 1, -1, launcher);

                pawn.TakeDamage(dinfo);

                // Effet visuel à l’impact
                FleckMaker.ThrowDustPuffThick(pawn.Position.ToVector3Shifted(), pawn.Map, 1.2f, Color.white);

                // Optionnel : message
                // Messages.Message($"{launcher.LabelShort} frappe {pawn.LabelShort} à distance !", MessageTypeDefOf.ThreatBig);
            }
        }
    }
}
