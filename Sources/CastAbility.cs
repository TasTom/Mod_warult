using RimWorld;
using UnityEngine;
using Verse;

namespace Mod_warult
{
    public class Verb_CastAbility_ApplyHediff : Verb_CastAbility
    {
        // Propriétés configurables depuis les modExtensions ou directement dans la classe
        protected virtual HediffDef HediffToApply => GetHediffDef();
        protected virtual float HediffSeverity => 1f;
        protected virtual int DurationTicks => -1; // -1 = utilise la durée du hediff

        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                Pawn targetPawn = currentTarget.Pawn ?? CasterPawn;
                return ApplyHediffToPawn(targetPawn);
            }
            return false;
        }

        protected virtual bool ApplyHediffToPawn(Pawn pawn)
        {
            if (pawn == null || HediffToApply == null) return false;

            // Crée et applique le hediff
            Hediff hediff = HediffMaker.MakeHediff(HediffToApply, pawn);
            hediff.Severity = HediffSeverity;

            // Applique une durée spécifique si définie
            if (DurationTicks > 0)
            {
                HediffComp_Disappears comp = hediff.TryGetComp<HediffComp_Disappears>();
                if (comp != null)
                {
                    comp.ticksToDisappear = DurationTicks;
                }
            }

            pawn.health.AddHediff(hediff);

            // Effets visuels optionnels
            CreateEffects(pawn);

            return true;
        }

        protected virtual void CreateEffects(Pawn pawn)
        {
            // Effet de particules basique
            FleckMaker.ThrowDustPuff(pawn.Position.ToVector3Shifted(), pawn.Map, 1.2f);
        }

        protected virtual HediffDef GetHediffDef()
        {
            // Méthode virtuelle pour permettre aux classes héritées de définir leur hediff
            return null;
        }
    }

    // Classes spécialisées pour chaque ability
    public class Verb_CastAbility_Regeneration : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_RegenerationBuff");

        protected override void CreateEffects(Pawn pawn)
        {
            base.CreateEffects(pawn);
            // Effet vert pour la régénération
            FleckMaker.ThrowDustPuff(pawn.Position.ToVector3Shifted(), pawn.Map, 1.5f);
        }
    }

    public class Verb_CastAbility_IronWill : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_IronWillBuff");
    }

    public class Verb_CastAbility_BerserkerRage : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_BerserkerRageBuff");

        protected override void CreateEffects(Pawn pawn)
        {
            base.CreateEffects(pawn);
            // Effet rouge pour la rage
            FleckMaker.ThrowHeatGlow(pawn.Position, pawn.Map, 1.5f);
        }
    }

    public class Verb_CastAbility_TimeSlip : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_TimeSlipBuff");
    }

    public class Verb_CastAbility_IronSkin : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_IronSkinBuff");
    }

    public class Verb_CastAbility_Fortress : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_FortressBuff");

        protected override void CreateEffects(Pawn pawn)
        {
            base.CreateEffects(pawn);
            // Effet de bouclier doré
            FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 2f);
        }
    }

    public class Verb_CastAbility_LuckyStrike : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_LuckyStrikeBuff");
    }

    public class Verb_CastAbility_Miracle : Verb_CastAbility_ApplyHediff
    {
        protected override HediffDef HediffToApply => DefDatabase<HediffDef>.GetNamed("Expedition33_MiracleBuff");

        protected override void CreateEffects(Pawn pawn)
        {
            base.CreateEffects(pawn);
            // Effet divin brillant
            FleckMaker.ThrowLightningGlow(pawn.Position.ToVector3Shifted(), pawn.Map, 3f);
        }
    }

    public class Verb_CastAbility_Explosion : Verb_CastAbility
    {
        // Propriétés de l'explosion configurables
        protected virtual float ExplosionRadius => 3.5f;
        protected virtual int BaseDamage => 50;
        protected virtual float ArmorPenetration => 0.5f;
        protected virtual DamageDef DamageType => DamageDefOf.Bomb;

        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateExplosion();
                return true;
            }
            return false;
        }

        protected virtual void CreateExplosion()
        {
            IntVec3 targetCell = currentTarget.Cell;
            Map map = CasterPawn.Map;

            // Créer l'explosion
            GenExplosion.DoExplosion(
                center: targetCell,
                map: map,
                radius: ExplosionRadius,
                damType: DamageType,
                instigator: CasterPawn,
                damAmount: BaseDamage,
                armorPenetration: ArmorPenetration,
                explosionSound: SoundDefOf.Explosion_FirefoamPopper,
                weapon: null,
                projectile: null,
                intendedTarget: currentTarget.Thing,
                postExplosionSpawnThingDef: null,
                postExplosionSpawnChance: 0f,
                postExplosionSpawnThingCount: 0,
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 0,
                chanceToStartFire: 0.1f,
                damageFalloff: true
            );

            // Effets visuels additionnels
            FleckMaker.ThrowSmoke(targetCell.ToVector3Shifted(), map, ExplosionRadius);
        }
    }
    
     public class Verb_CastAbility_QuickStep : Verb_Jump
    {
        protected override bool TryCastShot()
        {
            if (base.TryCastShot())
            {
                CreateTeleportEffects();
                return true;
            }
            return false;
        }

        protected virtual void CreateTeleportEffects()
        {
            IntVec3 startPos = CasterPawn.Position;
            IntVec3 endPos = currentTarget.Cell;
            Map map = CasterPawn.Map;

            // Effets au point de départ
            FleckMaker.ThrowDustPuff(startPos.ToVector3Shifted(), map, 1.5f);
            FleckMaker.ThrowLightningGlow(startPos.ToVector3Shifted(), map, 1f);

            // Effets au point d'arrivée (sera joué quand le pawn arrive)
            FleckMaker.ThrowDustPuff(endPos.ToVector3Shifted(), map, 1.5f);
            FleckMaker.ThrowLightningGlow(endPos.ToVector3Shifted(), map, 1f);
        }
    }

}
