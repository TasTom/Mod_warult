using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Mod_warult
{
    public class CompAbilityUser : ThingComp
    {
        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser)props;
        
        private List<AbilityDef> learnedAbilities = new List<AbilityDef>();
        private Dictionary<AbilityDef, int> abilityCooldowns = new Dictionary<AbilityDef, int>();
        
        public Pawn AbilityUserPawn => parent as Pawn;
        
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            
            if (!respawningAfterLoad && Props?.abilityDefs != null)
            {
                foreach (var abilityDef in Props.abilityDefs)
                {
                    LearnAbility(abilityDef);
                }
            }
        }
        
        public void LearnAbility(AbilityDef abilityDef)
        {
            if (!learnedAbilities.Contains(abilityDef))
            {
                learnedAbilities.Add(abilityDef);
                if (AbilityUserPawn?.abilities != null)
                {
                    AbilityUserPawn.abilities.GainAbility(abilityDef);
                }
            }
        }
        
        public bool CanCastAbility(AbilityDef abilityDef)
        {
            if (!learnedAbilities.Contains(abilityDef)) return false;
            
            if (abilityCooldowns.TryGetValue(abilityDef, out int cooldownEnd))
            {
                return GenTicks.TicksGame >= cooldownEnd;
            }
            
            return true;
        }
        
        public void StartCooldown(AbilityDef abilityDef)
        {
            int cooldownTicks = abilityDef.cooldownTicksRange.min;
            abilityCooldowns[abilityDef] = GenTicks.TicksGame + cooldownTicks;
        }
        
        public override void CompTick()
        {
            base.CompTick();
            
            // Nettoyer les cooldowns expirés
            var expiredCooldowns = abilityCooldowns
                .Where(kvp => GenTicks.TicksGame >= kvp.Value)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var expired in expiredCooldowns)
            {
                abilityCooldowns.Remove(expired);
            }
        }
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref learnedAbilities, "learnedAbilities", LookMode.Def);
            Scribe_Collections.Look(ref abilityCooldowns, "abilityCooldowns", LookMode.Def, LookMode.Value);
        }
    }
    
    public class CompProperties_AbilityUser : CompProperties
    {
        public List<AbilityDef> abilityDefs;
        
        public CompProperties_AbilityUser()
        {
            compClass = typeof(CompAbilityUser);
        }
    }
}
