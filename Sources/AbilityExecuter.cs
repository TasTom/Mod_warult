using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

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
                Log.Warning($"[Expedition33] {caster.LabelShort} annule IndigoBlast - trop proche");
                return;
            }
            
            GenExplosion.DoExplosion(
                center: targetCell,
                map: caster.Map,
                radius: 6f,
                damType: DamageDefOf.Bomb,
                instigator: caster,
                damAmount: 80,
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
            
            Messages.Message($"💥 {caster.LabelShort} déchaîne une explosion !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteDivinePurification(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_DivineRegeneration", HediffDefOf.NeuralSupercharge);
            
            for (int i = 0; i < 20; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), 
                    caster.Map, 3f, Color.white);
            }
            
            Messages.Message($"✨ {caster.LabelShort} se purifie !",
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
            
            Messages.Message($"🌟 {caster.LabelShort} projette une lumière sacrée !",
                MessageTypeDefOf.ThreatBig);
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
    
    Messages.Message($"⚡ {caster.LabelShort} explose de vitesse et repousse les ennemis !",
        MessageTypeDefOf.ThreatBig);
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
    
    Messages.Message($"🛡️ {caster.LabelShort} active son armure et blesse les attaquants proches !",
        MessageTypeDefOf.ThreatBig);
}

        public static void ExecuteAreaAttacks(Pawn caster, Thing target)
        {
            IntVec3 targetCell = target.Position;
            
            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 3f, true))
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
            
            Messages.Message($"💥 {caster.LabelShort} effectue une attaque de zone !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteSlowRegeneration(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_SlowRegenerationBuff", HediffDefOf.RapidRegeneration);
            
            for (int i = 0; i < 12; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), 
                    caster.Map, 2f, Color.green);
            }
            
            Messages.Message($"✨ {caster.LabelShort} commence une régénération lente !",
                MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteEnergyShields(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_EnergyShieldsBuff", HediffDefOf.GhoulPlating);
            
            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowLightningGlow(caster.Position.ToVector3Shifted(), caster.Map, 2f);
            }
            
            Messages.Message($"🔰 {caster.LabelShort} active ses boucliers énergétiques !",
                MessageTypeDefOf.NeutralEvent);
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
            
            Messages.Message($"🔆 {caster.LabelShort} manipule la lumière !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteIlluminationAttacks(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Burn, 40, 2.0f, -1, caster);
                pawnTarget.TakeDamage(dinfo);
                
                SafeAddHediff(pawnTarget, "Expedition33_IlluminationBurn", HediffDefOf.Blindness);
                
                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 3f);
                
                Messages.Message($"☀️ {caster.LabelShort} aveugle {pawnTarget.LabelShort} avec un faisceau lumineux !",
                    MessageTypeDefOf.ThreatBig);
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
            
            Messages.Message($"🏮 {caster.LabelShort} convoque des lampes spectrales !",
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

                    Messages.Message($"☀️ {caster.LabelShort} frappe {pawnTarget.LabelShort} avec la puissance du soleil !",
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

                    Messages.Message($"🌑 {caster.LabelShort} maudit {pawnTarget.LabelShort} avec les ténèbres !",
                        MessageTypeDefOf.ThreatBig);
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
                if (pawn != null && pawn.Faction != caster.Faction)
                {
                    SafeAddHediff(pawn, "Expedition33_ObscurEffect", HediffDefOf.DarkPsychicShock);
                }
            }

            for (int i = 0; i < 12; i++)
            {
                FleckMaker.ThrowDustPuffThick(targetCell.ToVector3Shifted(),
                    caster.Map, 3f, Color.black);
            }

            Messages.Message($"🌑 {caster.LabelShort} lance une explosion obscure !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteDualityShift(Pawn caster)
        {
            // Déterminer l'état actuel
            var currentHour = GenLocalDate.HourOfDay(caster.Map);
            bool isDaytime = currentHour >= 6 && currentHour <= 18;

            // Explosion de transformation
            GenExplosion.DoExplosion(
                center: caster.Position,
                map: caster.Map,
                radius: 4f,
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
                Messages.Message($"🌑 {caster.LabelShort} plonge la zone dans les ténèbres !",
                    MessageTypeDefOf.ThreatBig);
            }
            else
            {
                CreateLightZones(caster);
                Messages.Message($"☀️ {caster.LabelShort} illumine la zone d'une lumière aveuglante !",
                    MessageTypeDefOf.ThreatBig);
            }

            // Transformation du boss
            SafeAddHediff(caster, "Expedition33_DualityShiftBuff",
                isDaytime ? HediffDefOf.DarkPsychicShock : HediffDefOf.LightExposure);

            // Téléportation tactique
            TeleportToOptimalPosition(caster);

            // Effets visuels spectaculaires
            for (int i = 0; i < 40; i++)
            {
                Color shiftColor = i % 2 == 0 ? Color.white : Color.black;
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(),
                    caster.Map, 5f, shiftColor);
            }
        }

        private static void CreateDarknessZones(Pawn caster)
        {
            var darkZones = GenRadial.RadialCellsAround(caster.Position, 12f, true);

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
            var lightZones = GenRadial.RadialCellsAround(caster.Position, 12f, true);

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
            var enemies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction && !p.Downed)
                .ToList();

            if (enemies.Any())
            {
                var target = enemies.RandomElement();
                var teleportPos = GenRadial.RadialCellsAround(target.Position, 3f, true)
                    .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map))
                    .FirstOrDefault();

                if (teleportPos.IsValid)
                {
                    caster.Position = teleportPos;
                    FleckMaker.ThrowLightningGlow(teleportPos.ToVector3Shifted(), caster.Map, 4f);
                }
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

            Messages.Message($"⚖️ L'Évêque {caster.LabelShort} prononce le JUGEMENT DERNIER !",
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
                    caster.Map, 2f, Color.red);

                Messages.Message($"☠️ {caster.LabelShort} porte un coup létal à {pawnTarget.LabelShort} !",
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

                // NOUVEAU : Dégâts psychiques directs
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 30, 1.0f, -1, caster);
                enemy.TakeDamage(dinfo);

                // NOUVEAU : Chance de fuite
                if (Rand.Chance(0.4f))
                {
                    enemy.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
                }

                FleckMaker.ThrowDustPuffThick(enemy.Position.ToVector3Shifted(),
                    caster.Map, 3f, Color.red);
            }

            Messages.Message($"😱 {caster.LabelShort} terrorise ses ennemis - certains fuient de terreur !",
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
            
            Messages.Message($"⚔️ {caster.LabelShort} booste ses alliés !",
                MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteTacticalStrike(Pawn caster, Thing target)
        {
            var nearbyAllies = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction == caster.Faction && 
                           p.Position.DistanceTo(caster.Position) <= 10f && 
                           !p.Downed && p != caster)
                .Count();
            
            int bonusDamage = nearbyAllies * 10;
            int totalDamage = 30 + bonusDamage;
            
            if (target is Pawn pawnTarget)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, totalDamage, 2.0f, -1, caster);
                pawnTarget.TakeDamage(dinfo);
                
                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 2f);
                
                Messages.Message($"🎯 {caster.LabelShort} coordonne une frappe tactique ({totalDamage} dégâts) !",
                    MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteExpeditionMemory(Pawn caster)
        {
            // Simuler l'invocation de membres corrompus
            var spawnPositions = GenRadial.RadialCellsAround(caster.Position, 5f, true)
                .Where(c => c.InBounds(caster.Map) && c.Walkable(caster.Map))
                .Take(3);
            
            foreach (var pos in spawnPositions)
            {
                // Effet visuel d'invocation
                FleckMaker.ThrowDustPuffThick(pos.ToVector3Shifted(), caster.Map, 3f, Color.gray);
                
                // Dégâts dans la zone d'invocation
                foreach (var enemy in GenRadial.RadialCellsAround(pos, 2f, true))
                {
                    var pawn = enemy.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        DamageInfo dinfo = new DamageInfo(DamageDefOf.Psychic, 20, 1.0f, -1, caster);
                        pawn.TakeDamage(dinfo);
                    }
                }
            }
            
            Messages.Message($"👻 {caster.LabelShort} invoque les mémoires de l'Expédition !",
                MessageTypeDefOf.ThreatBig);
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
            
            Messages.Message($"🌊 {caster.LabelShort} émet une onde sonique dévastatrice !",
                MessageTypeDefOf.ThreatBig);
        }

        public static void ExecuteMindControl(Pawn caster, Thing target)
        {
            if (target is Pawn pawnTarget && pawnTarget.Faction != caster.Faction)
            {
                SafeAddHediff(pawnTarget, "Expedition33_MindControlled", HediffDefOf.PsychicTrance);
                
                FleckMaker.ThrowLightningGlow(pawnTarget.Position.ToVector3Shifted(), caster.Map, 3f);
                
                Messages.Message($"🧠 {caster.LabelShort} prend le contrôle de {pawnTarget.LabelShort} !",
                    MessageTypeDefOf.ThreatBig);
            }
        }

        public static void ExecuteFaceShift(Pawn caster)
        {
            SafeAddHediff(caster, "Expedition33_FaceShiftBuff", HediffDefOf.PsychicTrance);
            
            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), 
                    caster.Map, 2f, Color.magenta);
            }
            
            Messages.Message($"🎭 {caster.LabelShort} change de visage !",
                MessageTypeDefOf.NeutralEvent);
        }

        public static void ExecuteRealityDistortion(Pawn caster)
        {
            var affectedCells = GenRadial.RadialCellsAround(caster.Position, 8f, true);
            
            foreach (var cell in affectedCells)
            {
                if (cell.InBounds(caster.Map))
                {
                    var pawn = cell.GetFirstPawn(caster.Map);
                    if (pawn != null && pawn.Faction != caster.Faction)
                    {
                        SafeAddHediff(pawn, "Expedition33_RealityDistorted", HediffDefOf.PsychicShock);
                    }
                    
                    // Effets visuels de distorsion
                    Color distortionColor = new Color(Rand.Value, Rand.Value, Rand.Value);
                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 1f, distortionColor);
                }
            }
            
            Messages.Message($"🌀 {caster.LabelShort} déforme la réalité !",
                MessageTypeDefOf.ThreatBig);
        }

        // === CAPACITÉS DE LA PEINTRESSE ===
        
        public static void ExecuteGommageRitual(Pawn caster)
        {
            int targetAge = Rand.Range(18, 80);
            
            var targets = caster.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != caster.Faction && 
                           p.ageTracker.AgeBiologicalYears == targetAge)
                .ToList();
            
            foreach (var target in targets)
            {
                SafeAddHediff(target, "Expedition33_GommageEffect", HediffDefOf.ToxicBuildup);
                
                FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), 
                    caster.Map, 3f, Color.red);
            }
            
            Messages.Message($"📝 {caster.LabelShort} inscrit l'âge {targetAge} - Danger mortel !",
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
            
            Messages.Message($"🎨 {caster.LabelShort} peint une zone de danger cosmique !",
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
            
            Messages.Message($"🔢 {caster.LabelShort} inscrit le nombre {randomNumber} !",
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
            
            Messages.Message($"⚫ {caster.LabelShort} canalise le pouvoir du Monolithe ! DESTRUCTION TOTALE !",
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
            
            Messages.Message($"♾️ {caster.LabelShort} utilise son immortalité !",
                MessageTypeDefOf.NeutralEvent);
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
            
            Messages.Message($"😡 {caster.LabelShort} entre dans une rage corrompue !",
                MessageTypeDefOf.ThreatBig);
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
    
    Messages.Message($"🤐 {caster.LabelShort} étouffe ses ennemis dans le silence !",
        MessageTypeDefOf.ThreatBig);
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
    
    Messages.Message($"🚧 {caster.LabelShort} écrase ses ennemis avec des barrières invisibles !",
        MessageTypeDefOf.ThreatBig);
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
    }
}
