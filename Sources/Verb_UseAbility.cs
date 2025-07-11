using Verse;
using RimWorld;

namespace Mod_warult
{
    public abstract class Verb_UseAbility : Verb
    {
        public CompAbilityUser AbilityUserComp => CasterPawn?.GetComp<CompAbilityUser>();
        
        protected override bool TryCastShot()
        {
            // Vérifications de base
            if (CasterPawn?.Map == null) return false;
            
            // Vérifier que l'ability peut être lancée
            var abilityDef = GetAbilityDef();
            if (abilityDef != null && AbilityUserComp != null)
            {
                if (!AbilityUserComp.CanCastAbility(abilityDef))
                {
                    return false;
                }
                
                // Exécuter l'effet spécifique
                bool success = ExecuteAbilityEffect();
                
                if (success)
                {
                    // Démarrer le cooldown
                    AbilityUserComp.StartCooldown(abilityDef);
                }
                
                return success;
            }
            
            // Fallback : exécuter directement l'effet
            return ExecuteAbilityEffect();
        }
        
        protected virtual bool ExecuteAbilityEffect()
        {
            // À override dans les classes dérivées
            return true;
        }
        
        private AbilityDef GetAbilityDef()
        {
            // Trouver l'AbilityDef correspondant à cette classe Verb
            string verbClassName = GetType().Name;
            string abilityDefName = verbClassName.Replace("Verb_CastAbility_", "Expedition33_");
            
            return DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
        }
        
        public override bool CanHitTarget(LocalTargetInfo target)
        {
            return base.CanHitTarget(target);
        }
    }
}
