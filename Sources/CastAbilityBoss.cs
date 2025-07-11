using RimWorld;
using UnityEngine;
using Verse;
using System.Linq;
using System.Collections.Generic;

namespace Mod_warult
{
    // ═══════════════════════════════════════════════════════════
    // CLASSE DE BASE SIMPLIFIÉE selon l'architecture RoM
    // ═══════════════════════════════════════════════════════════
    
   public abstract class Verb_CastAbility : Verb
    {
        protected override bool TryCastShot()
        {
            // ✅ CORRECTION : Ne plus appeler base.TryCastShot() (abstrait)
            try
            {
                bool success = ExecuteAbilityEffect();
                
                // Logs de debug pour identifier les problèmes
                Log.Message($"[DEBUG] {GetType().Name} - ExecuteAbilityEffect: {success}");
                
                return success;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur dans {GetType().Name}: {e.Message}");
                return false; // ✅ CRITIQUE : Toujours retourner un booléen
            }
        }

        protected virtual bool ExecuteAbilityEffect()
        {
            // À override dans les classes dérivées
            return true;
        }

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            return true; // Autoriser tous les ciblages pour les capacités magiques
        }
    }


    // ═══════════════════════════════════════════════════════════
    // ABILITIES DE L'ÉVÊQUE - VERSION CORRIGÉE
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_SacredLight : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (!currentTarget.IsValid || CasterPawn?.Map == null)
                return false;

            Log.Message($"[DEBUG] SacredLight - Caster: {CasterPawn.LabelShort}, Target: {currentTarget.Cell}");

            try
            {
                CreateSacredLightEffect(currentTarget.Cell, CasterPawn.Map);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur SacredLight: {e.Message}");
                return false;
            }
        }

        private void CreateSacredLightEffect(IntVec3 targetCell, Map map)
        {
            // Zone de lumière sacrée 3x3
            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 2f, true))
            {
                if (cell.InBounds(map))
                {
                    var pawn = cell.GetFirstPawn(map);
                    if (pawn != null)
                    {
                        if (pawn.Faction == CasterPawn.Faction)
                        {
                            // Soins pour les alliés
                            var healHediff = HediffMaker.MakeHediff(
                                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_SacredHealing"), pawn);
                            if (healHediff != null)
                                pawn.health.AddHediff(healHediff);
                        }
                        else
                        {
                            // Aveuglement aux ennemis
                            var blindHediff = HediffMaker.MakeHediff(
                                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_SacredBlindness"), pawn);
                            if (blindHediff != null)
                                pawn.health.AddHediff(blindHediff);
                        }
                    }

                    // Effet visuel doré
                    FleckMaker.ThrowLightningGlow(cell.ToVector3Shifted(), map, 1.5f);
                }
            }

            Messages.Message($"✨ {CasterPawn.LabelShort} projette une lumière sacrée !",
                MessageTypeDefOf.ThreatBig);
        }
    }

    public class Verb_CastAbility_DivinePurification : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            Log.Message($"[DEBUG] DivinePurification - Caster: {CasterPawn.LabelShort}");

            try
            {
                CreatePurificationZone(CasterPawn);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur DivinePurification: {e.Message}");
                return false;
            }
        }

        private void CreatePurificationZone(Pawn caster)
        {
            // Régénération puissante pour le caster
            var regenHediff = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DivineRegeneration"), caster);
            if (regenHediff != null)
                caster.health.AddHediff(regenHediff);

            // Repousse les ennemis proches
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 5f, true))
            {
                var enemy = cell.GetFirstPawn(caster.Map);
                if (enemy != null && enemy.Faction != caster.Faction)
                {
                    // Force de répulsion
                    IntVec3 pushDirection = (enemy.Position - caster.Position);
                    IntVec3 pushTarget = enemy.Position + pushDirection * 3;
                    if (pushTarget.InBounds(caster.Map) && pushTarget.Walkable(caster.Map))
                    {
                        enemy.DeSpawn();
                        GenSpawn.Spawn(enemy, pushTarget, caster.Map);
                    }
                }
            }

            // Effet visuel massif blanc et doré
            for (int i = 0; i < 20; i++)
            {
                FleckMaker.ThrowDustPuffThick(caster.Position.ToVector3Shifted(), 
                    caster.Map, 3f, Color.white);
            }

            Messages.Message($"🌟 {caster.LabelShort} se purifie dans la lumière divine !",
                MessageTypeDefOf.ThreatBig);
        }
    }

    public class Verb_CastAbility_IndigoBlast : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (!currentTarget.IsValid || !currentTarget.Cell.InBounds(CasterPawn.Map))
                return false;

            Log.Message($"[DEBUG] IndigoBlast - Caster: {CasterPawn.LabelShort}, Target: {currentTarget.Cell}");

            try
            {
                CreateIndigoExplosion(currentTarget.Cell, CasterPawn.Map);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur IndigoBlast: {e.Message}");
                return false;
            }
        }

        private void CreateIndigoExplosion(IntVec3 targetCell, Map map)
        {
            // Vérifier la distance de sécurité pour le caster
            float distanceToCaster = CasterPawn.Position.DistanceTo(targetCell);
            if (distanceToCaster <= 6.5f) // Rayon d'explosion + marge de sécurité
            {
                Log.Warning($"[Expedition33] {CasterPawn.LabelShort} annule IndigoBlast - trop proche de l'explosion");
                return;
            }

            // Explosion avec exclusion du caster
            GenExplosion.DoExplosion(
                center: targetCell,
                map: map,
                radius: 6f,
                damType: DamageDefOf.Bomb,
                instigator: CasterPawn,
                damAmount: 80,
                armorPenetration: 0.8f,
                explosionSound: SoundDefOf.VoidNode_Explode,
                chanceToStartFire: 0f,
                damageFalloff: true,
                ignoredThings: new List<Thing> { CasterPawn } // CRITIQUE : Exclure le caster
            );

            // Effets visuels
            for (int i = 0; i < 15; i++)
            {
                FleckMaker.ThrowDustPuffThick(targetCell.ToVector3Shifted(),
                    map, 4f, new Color(0.3f, 0f, 0.8f));
            }

            Messages.Message($"💥 {CasterPawn.LabelShort} déchaîne une explosion d'indigo !",
                MessageTypeDefOf.ThreatBig);
        }

    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DES SAKAPATATES - VERSION CORRIGÉE
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_RapidMovement : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                var speedBoost = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_RapidMovementBuff"), CasterPawn);
                if (speedBoost != null)
                    CasterPawn.health.AddHediff(speedBoost);

                // Effet visuel de vitesse
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.ThrowDustPuffThick(CasterPawn.Position.ToVector3Shifted(),
                        CasterPawn.Map, 1.5f, Color.yellow);
                }

                Messages.Message($"⚡ {CasterPawn.LabelShort} active son mouvement rapide !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur RapidMovement: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_HeavyArmor : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                var armorBuff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_HeavyArmorBuff"), CasterPawn);
                if (armorBuff != null)
                    CasterPawn.health.AddHediff(armorBuff);

                // Effet visuel métallique
                for (int i = 0; i < 15; i++)
                {
                    FleckMaker.ThrowDustPuffThick(CasterPawn.Position.ToVector3Shifted(),
                        CasterPawn.Map, 2f, Color.gray);
                }

                Messages.Message($"🛡️ {CasterPawn.LabelShort} active son blindage renforcé !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur HeavyArmor: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_SlowRegeneration : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                var regenBuff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_SlowRegenerationBuff"), CasterPawn);
                if (regenBuff != null)
                    CasterPawn.health.AddHediff(regenBuff);

                FleckMaker.ThrowLightningGlow(CasterPawn.Position.ToVector3Shifted(), CasterPawn.Map, 2f);

                Messages.Message($"💚 {CasterPawn.LabelShort} active sa régénération !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur SlowRegeneration: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_EnergyShields : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                var shieldBuff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_EnergyShieldsBuff"), CasterPawn);
                if (shieldBuff != null)
                    CasterPawn.health.AddHediff(shieldBuff);

                // Effet visuel électrique spectaculaire
                for (int i = 0; i < 20; i++)
                {
                    Vector3 randomPos = CasterPawn.Position.ToVector3Shifted() + new Vector3(
                        Rand.Range(-3f, 3f), 0f, Rand.Range(-3f, 3f));
                    FleckMaker.ThrowLightningGlow(randomPos, CasterPawn.Map, 1f);
                }

                Messages.Message($"⚡ {CasterPawn.LabelShort} active ses boucliers d'énergie !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur EnergyShields: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_AreaAttacks : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                // Attaque en zone autour du boss
                foreach (var cell in GenRadial.RadialCellsAround(CasterPawn.Position, 6f, true))
                {
                    if (cell.InBounds(CasterPawn.Map))
                    {
                        var enemy = cell.GetFirstPawn(CasterPawn.Map);
                        if (enemy != null && enemy.Faction != CasterPawn.Faction)
                        {
                            var areaDamage = new DamageInfo(DamageDefOf.Blunt, 40f, 1f, -1f, CasterPawn);
                            enemy.TakeDamage(areaDamage);
                        }

                        // Effet visuel d'onde de choc
                        FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), CasterPawn.Map, 1.5f, Color.red);
                    }
                }

                Messages.Message($"💥 {CasterPawn.LabelShort} déchaîne une attaque de zone !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur AreaAttacks: {e.Message}");
                return false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DU DUALISTE - VERSION CORRIGÉE
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_ClairStrike : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            var target = currentTarget.Pawn;
            if (target == null || CasterPawn?.Map == null)
                return false;

            try
            {
                // Dégâts de lumière pure qui ignorent l'armure
                var damage = new DamageInfo(DamageDefOf.Burn, 60f, 1.5f, -1f, CasterPawn);
                target.TakeDamage(damage);

                // Effet d'aveuglement
                var blindHediff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LightBlindness"), target);
                if (blindHediff != null)
                    target.health.AddHediff(blindHediff);

                // Effet visuel lumineux
                FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 2f);

                Messages.Message($"🌟 {target.LabelShort} est frappé par la lumière pure !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur ClairStrike: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_ObscurBlast : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (!currentTarget.IsValid || CasterPawn?.Map == null)
                return false;

            try
            {
                CreateDarknessExplosion(currentTarget.Cell, CasterPawn.Map);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur ObscurBlast: {e.Message}");
                return false;
            }
        }

        private void CreateDarknessExplosion(IntVec3 targetCell, Map map)
        {
            // Explosion de ténèbres avec drain de vie
            foreach (var cell in GenRadial.RadialCellsAround(targetCell, 4f, true))
            {
                if (cell.InBounds(map))
                {
                    var pawn = cell.GetFirstPawn(map);
                    if (pawn != null && pawn.Faction != CasterPawn.Faction)
                    {
                        // Dégâts + drain de vie
                        var damage = new DamageInfo(DamageDefOf.Deterioration, 35f, 0.5f, -1f, CasterPawn);
                        pawn.TakeDamage(damage);

                        // Ralentissement
                        var slowHediff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DarknessSlow"), pawn);
                        if (slowHediff != null)
                            pawn.health.AddHediff(slowHediff);

                        // Soigne le Dualiste
                        if (CasterPawn.health.summaryHealth.SummaryHealthPercent < 1f)
                        {
                            var injuries = CasterPawn.health.hediffSet.hediffs
                                .OfType<Hediff_Injury>().ToList();
                            if (injuries.Any())
                            {
                                var injury = injuries.RandomElement();
                                injury.Heal(10f);
                            }
                        }
                    }

                    // Effet visuel sombre
                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 2f, Color.black);
                }
            }

            Messages.Message($"🌑 {CasterPawn.LabelShort} libère une explosion de ténèbres !",
                MessageTypeDefOf.ThreatBig);
        }
    }

    public class Verb_CastAbility_DualityShift : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                // Changement de polarité - applique le hediff approprié
                var lightMode = CasterPawn.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LightMode"));

                if (lightMode != null)
                {
                    // Passer en mode obscur
                    CasterPawn.health.RemoveHediff(lightMode);
                    var darkHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DarkMode"), CasterPawn);
                    if (darkHediff != null)
                        CasterPawn.health.AddHediff(darkHediff);
                }
                else
                {
                    // Passer en mode lumière
                    var darkMode = CasterPawn.health.hediffSet.GetFirstHediffOfDef(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DarkMode"));
                    if (darkMode != null) CasterPawn.health.RemoveHediff(darkMode);

                    var lightHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LightMode"), CasterPawn);
                    if (lightHediff != null)
                        CasterPawn.health.AddHediff(lightHediff);
                }

                // Effet visuel spectaculaire
                for (int i = 0; i < 25; i++)
                {
                    Color effectColor = Rand.Bool ? Color.white : Color.black;
                    FleckMaker.ThrowDustPuffThick(CasterPawn.Position.ToVector3Shifted(), 
                        CasterPawn.Map, 3f, effectColor);
                }

                Messages.Message($"⚡ {CasterPawn.LabelShort} inverse sa polarité !",
                    MessageTypeDefOf.ThreatBig);

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur DualityShift: {e.Message}");
                return false;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DE FRANÇOIS - VERSION CORRIGÉE
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_LethalStrike : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            var target = currentTarget.Pawn;
            if (target == null || CasterPawn?.Map == null)
                return false;

            try
            {
                // 75% de chance d'instant kill, sinon dégâts massifs
                if (Rand.Chance(0.75f))
                {
                    Messages.Message($"💀 {target.LabelShort} est éliminé par François !",
                        MessageTypeDefOf.ThreatBig);
                    var lethalDamage = new DamageInfo(DamageDefOf.ExecutionCut, 9999f, 10f, -1f, CasterPawn);
                    target.TakeDamage(lethalDamage);
                }
                else
                {
                    // Dégâts très importants si pas d'instant kill
                    var heavyDamage = new DamageInfo(DamageDefOf.Cut, 150f, 2f, -1f, CasterPawn);
                    target.TakeDamage(heavyDamage);
                    Messages.Message($"⚔️ {target.LabelShort} survit de justesse !",
                        MessageTypeDefOf.NeutralEvent);
                }

                // Effet visuel dramatique
                FleckMaker.ThrowMetaIcon(target.Position, target.Map, FleckDefOf.Heart);
                for (int i = 0; i < 10; i++)
                {
                    FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), 
                        target.Map, 2f, Color.red);
                }

                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur LethalStrike: {e.Message}");
                return false;
            }
        }
    }

    public class Verb_CastAbility_FearAura : Verb_CastAbility
    {
        protected override bool ExecuteAbilityEffect()
        {
            if (CasterPawn?.Map == null)
                return false;

            try
            {
                CreateFearAura(CasterPawn);
                return true;
            }
            catch (System.Exception e)
            {
                Log.Error($"[DEBUG] Erreur FearAura: {e.Message}");
                return false;
            }
        }

        private void CreateFearAura(Pawn caster)
        {
            // Applique la peur à tous les ennemis dans un large rayon
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 12f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var enemy = cell.GetFirstPawn(caster.Map);
                    if (enemy != null && enemy.Faction != caster.Faction)
                    {
                        // Hediff de terreur
                        var fearHediff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FrancoisTerror"), enemy);
                        if (fearHediff != null)
                            enemy.health.AddHediff(fearHediff);
                    }
                }
            }

            Messages.Message("😱 François répand une aura de terreur !",
                MessageTypeDefOf.ThreatBig);

            // Effet visuel d'aura sombre
            for (int i = 0; i < 30; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.InsideUnitCircle.x * 8f, 0f, Rand.InsideUnitCircle.y * 8f);
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 1.5f, new Color(0.2f, 0f, 0f));
            }
        }
    }

    public class Verb_CastAbility_CommanderAura : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateCommanderAura(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateCommanderAura(Pawn caster)
        {
            // Buff tous les alliés proches
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 15f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var ally = cell.GetFirstPawn(caster.Map);
                    if (ally != null && ally.Faction == caster.Faction && ally != caster)
                    {
                        var commandBuff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_CommanderBoost"), ally);
                        if (commandBuff != null)
                            ally.health.AddHediff(commandBuff);
                    }
                }
            }

            Messages.Message("⚔️ Renoir inspire ses troupes !",
                MessageTypeDefOf.ThreatBig);

            // Effet visuel militaire
            for (int i = 0; i < 20; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-10f, 10f), 0f, Rand.Range(-10f, 10f));
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 1.5f, new Color(0.8f, 0.6f, 0.2f));
            }
        }
    }

    public class Verb_CastAbility_TacticalStrike : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Attaque tactique précise avec dégâts élevés
                    var tacticalDamage = new DamageInfo(DamageDefOf.Bullet, 120f, 3f, -1f, CasterPawn);
                    target.TakeDamage(tacticalDamage);

                    // Effet visuel de précision
                    FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 2f);
                    for (int i = 0; i < 5; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 1f, Color.yellow);
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_ExpeditionMemory : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateExpeditionMemory(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateExpeditionMemory(Pawn caster)
        {
            // Invoque 2-3 membres corrompus de l'expédition
            int memberCount = Rand.Range(2, 4);
            
            for (int i = 0; i < memberCount; i++)
            {
                // Trouver une position libre près du caster
                IntVec3 spawnPos = CellFinder.RandomClosewalkCellNear(caster.Position, caster.Map, 3);
                
                if (spawnPos.IsValid)
                {
                    // Créer un membre corrompu
                    PawnKindDef memberKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_CorruptedMember");
                    if (memberKind != null)
                    {
                        Pawn member = PawnGenerator.GeneratePawn(memberKind, caster.Faction);
                        GenSpawn.Spawn(member, spawnPos, caster.Map);
                        
                        // Effet visuel d'invocation
                        FleckMaker.ThrowDustPuffThick(spawnPos.ToVector3Shifted(), caster.Map, 2f, Color.green);
                    }
                }
            }

            Messages.Message($"👥 Renoir invoque les souvenirs de l'expédition !",
                MessageTypeDefOf.ThreatBig);
        }
    }
    // ═══════════════════════════════════════════════════════════
    // ABILITIES DU MAÎTRE DES LAMPES - VERSION RoM
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_LightManipulation : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Manipulation de la lumière causant confusion
                    var confusionHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LightConfusion"), target);
                    if (confusionHediff != null)
                        target.health.AddHediff(confusionHediff);

                    // Effet visuel lumineux
                    for (int i = 0; i < 8; i++)
                    {
                        FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 1.5f);
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_IlluminationAttacks : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Attaque de rayons lumineux
                    var lightDamage = new DamageInfo(DamageDefOf.Burn, 70f, 1.2f, -1f, CasterPawn);
                    target.TakeDamage(lightDamage);

                    // Aveuglement temporaire
                    target.stances.stunner.StunFor(180, CasterPawn);

                    // Effet visuel de rayon
                    FleckMaker.ThrowLightningGlow(target.Position.ToVector3Shifted(), target.Map, 3f);
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_LampMastery : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateLampMastery(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateLampMastery(Pawn caster)
        {
            // Aura protectrice du maître des lampes
            var masteryBuff = HediffMaker.MakeHediff(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LampMastery"), caster);
            if (masteryBuff != null)
                caster.health.AddHediff(masteryBuff);

            // Malédiction aux ennemis proches
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 8f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var enemy = cell.GetFirstPawn(caster.Map);
                    if (enemy != null && enemy.Faction != caster.Faction)
                    {
                        var curseHediff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_LampCurse"), enemy);
                        if (curseHediff != null)
                            enemy.health.AddHediff(curseHediff);
                    }
                }
            }

            // Effet visuel spectral
            for (int i = 0; i < 25; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-6f, 6f), 0f, Rand.Range(-6f, 6f));
                FleckMaker.ThrowLightningGlow(randomPos, caster.Map, 1.2f);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DE LA SIRÈNE - VERSION RoM
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_SonicWave : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateSonicWave(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateSonicWave(Pawn caster)
        {
            // Onde sonique affectant tous les ennemis dans un large rayon
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 15f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var enemy = cell.GetFirstPawn(caster.Map);
                    if (enemy != null && enemy.Faction != caster.Faction)
                    {
                        // Étourdissement sonique
                        var stunHediff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_SonicStun"), enemy);
                        if (stunHediff != null)
                            enemy.health.AddHediff(stunHediff);

                        // Dégâts soniques
                        var sonicDamage = new DamageInfo(DamageDefOf.Psychic, 45f, 0.8f, -1f, caster);
                        enemy.TakeDamage(sonicDamage);
                    }

                    // Effet visuel d'onde
                    FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 1f, new Color(0.5f, 0.7f, 0.9f));
                }
            }

            Messages.Message("🌊 La Sirène libère une onde sonique dévastatrice !",
                MessageTypeDefOf.ThreatBig);
        }
    }

    public class Verb_CastAbility_MindControl : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Contrôle mental temporaire
                    var controlHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_MindControlled"), target);
                    if (controlHediff != null)
                        target.health.AddHediff(controlHediff);

                    // Changer temporairement la faction
                    target.SetFaction(CasterPawn.Faction);

                    Messages.Message($"🧠 {target.LabelShort} est sous contrôle mental !",
                        MessageTypeDefOf.ThreatBig);

                    // Effet visuel psychique
                    for (int i = 0; i < 12; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 2f, 
                            new Color(0.8f, 0.2f, 0.8f));
                    }
                }
                return true;
            }
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DES VISAGES - VERSION RoM
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_FaceShift : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateFaceShift(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateFaceShift(Pawn caster)
        {
            // Altération d'identité pour tous les ennemis proches
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 10f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var enemy = cell.GetFirstPawn(caster.Map);
                    if (enemy != null && enemy.Faction != caster.Faction)
                    {
                        var shiftHediff = HediffMaker.MakeHediff(
                            DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FaceShifted"), enemy);
                        if (shiftHediff != null)
                            enemy.health.AddHediff(shiftHediff);
                    }
                }
            }

            Messages.Message("👤 Les Visages altèrent les identités !",
                MessageTypeDefOf.ThreatBig);

            // Effet visuel de distorsion
            for (int i = 0; i < 20; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-8f, 8f), 0f, Rand.Range(-8f, 8f));
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 2f, new Color(0.6f, 0.4f, 0.6f));
            }
        }
    }

    public class Verb_CastAbility_RealityDistortion : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateRealityDistortion(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateRealityDistortion(Pawn caster)
        {
            // Distorsion de la réalité créant des zones d'illusion
            for (int i = 0; i < 5; i++)
            {
                IntVec3 distortionCenter = caster.Position + new IntVec3(
                    Rand.Range(-15, 15), 0, Rand.Range(-15, 15));

                if (distortionCenter.InBounds(caster.Map))
                {
                    // Zone de distorsion 3x3
                    foreach (var cell in GenRadial.RadialCellsAround(distortionCenter, 2f, true))
                    {
                        if (cell.InBounds(caster.Map))
                        {
                            var pawn = cell.GetFirstPawn(caster.Map);
                            if (pawn != null && pawn.Faction != caster.Faction)
                            {
                                // Confusion d'identité
                                var confusionHediff = HediffMaker.MakeHediff(
                                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_IdentityConfusion"), pawn);
                                if (confusionHediff != null)
                                    pawn.health.AddHediff(confusionHediff);

                                // Dégâts psychiques
                                var psychicDamage = new DamageInfo(DamageDefOf.Psychic, 30f, 0.5f, -1f, caster);
                                pawn.TakeDamage(psychicDamage);
                            }

                            // Effet visuel kaléidoscopique
                            FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), caster.Map, 2f, 
                                new Color(Rand.Value, Rand.Value, Rand.Value));
                        }
                    }
                }
            }

            Messages.Message("🌀 Les Visages distordent la réalité !",
                MessageTypeDefOf.ThreatBig);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES DE LA PEINTRESSE - VERSION RoM
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_GommageRitual : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateGommageRitual(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateGommageRitual(Pawn caster)
        {
            // Rituel de gommage - menace tous les pawns d'un âge spécifique
            int gommageNumber = Rand.Range(15, 65);
            
            Messages.Message($"📜 LA PEINTRESSE INSCRIT LE NOMBRE {gommageNumber} !",
                MessageTypeDefOf.ThreatBig);

            // Affecte tous les pawns de cet âge
            var affectedPawns = PawnsFinder.AllCaravansAndTravellingTransporters_Alive
                .Where(p => p.ageTracker.AgeBiologicalYears == gommageNumber).ToList();

            foreach (var pawn in affectedPawns)
            {
                if (pawn.Faction != caster.Faction)
                {
                    var gommageHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageCountdown"), pawn);
                    if (gommageHediff != null)
                        pawn.health.AddHediff(gommageHediff);

                    Messages.Message($"💀 {pawn.LabelShort} ({gommageNumber} ans) est marqué pour le gommage !",
                        MessageTypeDefOf.ThreatBig);
                }
            }

            // Effet visuel mystique
            for (int i = 0; i < 50; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-20f, 20f), 0f, Rand.Range(-20f, 20f));
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 3f, new Color(1f, 0f, 0f));
            }
        }
    }

    public class Verb_CastAbility_CosmicBrush : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Marquage cosmique
                    var paintHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_CosmicPaint"), target);
                    if (paintHediff != null)
                        target.health.AddHediff(paintHediff);

                    // Dégâts de distorsion
                    var cosmicDamage = new DamageInfo(DamageDefOf.Psychic, 60f, 1.5f, -1f, CasterPawn);
                    target.TakeDamage(cosmicDamage);

                    // Effet visuel cosmique
                    for (int i = 0; i < 15; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 2f, 
                            new Color(0.8f, 0.2f, 0.8f));
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_NumberInscription : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Inscription d'un nombre néfaste
                    var numberHediff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_NumberCurse"), target);
                    if (numberHediff != null)
                        target.health.AddHediff(numberHediff);

                    int cursedNumber = Rand.Range(1, 999);
                    Messages.Message($"🔢 La Peintresse inscrit le nombre {cursedNumber} sur {target.LabelShort} !",
                        MessageTypeDefOf.ThreatBig);

                    // Effet visuel numérique
                    for (int i = 0; i < 10; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 1.5f, 
                            new Color(0.9f, 0.1f, 0.1f));
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_MonolithPower : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateMonolithPower(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateMonolithPower(Pawn caster)
        {
            // Attaque finale dévastatrice
            Messages.Message("⚫ LA PEINTRESSE CANALISE LE POUVOIR DU MONOLITHE !",
                MessageTypeDefOf.ThreatBig);

            // Dégâts massifs à tous les ennemis sur la carte
            var allEnemies = caster.Map.mapPawns.AllPawns
                .Where(p => p.Faction != caster.Faction).ToList();

            foreach (var enemy in allEnemies)
            {
                var monolithDamage = new DamageInfo(DamageDefOf.Psychic, 100f, 5f, -1f, caster);
                enemy.TakeDamage(monolithDamage);

                // Hediff de pouvoir du monolithe
                var monolithHediff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_MonolithCurse"), enemy);
                if (monolithHediff != null)
                    enemy.health.AddHediff(monolithHediff);
            }

            // Effet visuel apocalyptique
            for (int i = 0; i < 100; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-25f, 25f), 0f, Rand.Range(-25f, 25f));
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 5f, Color.black);
            }

            // Auto-dégâts à la Peintresse (attaque ultime coûteuse)
            var selfDamage = new DamageInfo(DamageDefOf.Deterioration, 150f, 1f, -1f, caster);
            caster.TakeDamage(selfDamage);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // ABILITIES SUPPLÉMENTAIRES - CAPACITÉS SPÉCIALES
    // ═══════════════════════════════════════════════════════════

    public class Verb_CastAbility_ImmortalityFragment : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                // Fragment d'immortalité - régénération extrême
                var immortalityBuff = HediffMaker.MakeHediff(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_DivineRegeneration"), CasterPawn);
                if (immortalityBuff != null)
                {
                    immortalityBuff.Severity = 2.0f; // Double effet
                    CasterPawn.health.AddHediff(immortalityBuff);
                }

                Messages.Message($"✨ {CasterPawn.LabelShort} active un fragment d'immortalité !",
                    MessageTypeDefOf.ThreatBig);

                // Effet visuel divin
                for (int i = 0; i < 30; i++)
                {
                    FleckMaker.ThrowLightningGlow(CasterPawn.Position.ToVector3Shifted(), CasterPawn.Map, 4f);
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_FractureParfaite : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Fracture parfaite - ignore toute armure et résistance
                    var fractureDamage = new DamageInfo(DamageDefOf.Cut, 200f, 10f, -1f, CasterPawn);
                    target.TakeDamage(fractureDamage);

                    Messages.Message($"💎 Fracture parfaite sur {target.LabelShort} !",
                        MessageTypeDefOf.ThreatBig);

                    // Effet visuel cristallin
                    for (int i = 0; i < 20; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 2f, 
                            new Color(0.8f, 0.9f, 1f));
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_CorruptedRage : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                // Rage corrompue - augmente drastiquement les dégâts mais réduit la défense
                var rageBuff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, CasterPawn);
                rageBuff.Severity = 0.1f; // Léger effet négatif
                CasterPawn.health.AddHediff(rageBuff);

                // Effet de rage (simulation via message et effets visuels)
                Messages.Message($"😡 {CasterPawn.LabelShort} entre dans une rage corrompue !",
                    MessageTypeDefOf.ThreatBig);

                // Effet visuel de rage
                for (int i = 0; i < 25; i++)
                {
                    FleckMaker.ThrowDustPuffThick(CasterPawn.Position.ToVector3Shifted(), CasterPawn.Map, 3f, 
                        new Color(0.8f, 0f, 0f));
                }
                return true;
            }
            return false;
        }
    }

    public class Verb_CastAbility_SilenceAura : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateSilenceAura(CasterPawn);
                return true;
            }
            return false;
        }

        private void CreateSilenceAura(Pawn caster)
        {
            // Aura de silence empêchant l'utilisation d'abilities
            foreach (var cell in GenRadial.RadialCellsAround(caster.Position, 10f, true))
            {
                if (cell.InBounds(caster.Map))
                {
                    var enemy = cell.GetFirstPawn(caster.Map);
                    if (enemy != null && enemy.Faction != caster.Faction)
                    {
                        // Réduire drastiquement la conscience pour simuler le silence
                        var silenceHediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, enemy);
                        silenceHediff.Severity = 0.5f;
                        enemy.health.AddHediff(silenceHediff);
                    }
                }
            }

            Messages.Message("🔇 Une aura de silence s'étend !",
                MessageTypeDefOf.ThreatBig);

            // Effet visuel de silence (particules grises)
            for (int i = 0; i < 20; i++)
            {
                Vector3 randomPos = caster.Position.ToVector3Shifted() + new Vector3(
                    Rand.Range(-8f, 8f), 0f, Rand.Range(-8f, 8f));
                FleckMaker.ThrowDustPuffThick(randomPos, caster.Map, 1.5f, Color.gray);
            }
        }
    }

    public class Verb_CastAbility_InvisibleBarriers : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                var target = currentTarget.Pawn;
                if (target != null)
                {
                    // Barrières invisibles - ralentissement extrême
                    target.stances.stunner.StunFor(300, CasterPawn); // 5 secondes

                    Messages.Message($"🚧 {target.LabelShort} est bloqué par des barrières invisibles !",
                        MessageTypeDefOf.NeutralEvent);

                    // Effet visuel subtil (barrières invisibles)
                    for (int i = 0; i < 8; i++)
                    {
                        FleckMaker.ThrowDustPuffThick(target.Position.ToVector3Shifted(), target.Map, 1f, 
                            new Color(0.5f, 0.5f, 0.5f, 0.3f));
                    }
                }
                return true;
            }
            return false;
        }
    }
}
