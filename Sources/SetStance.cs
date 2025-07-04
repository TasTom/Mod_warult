using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;

namespace Mod_warult
{
    public enum CombatStance
    {
        None,
        Offensive,    // +50% dégâts, +50% dégâts reçus
        Defensive,    // -25% dégâts reçus, +1 AP sur parade
        Virtuous      // +200% dégâts pour une attaque
    }
    
    public class CompCombatStance : ThingComp
    {
        private CombatStance currentStance = CombatStance.None;
        private int virtuousUsesLeft = 0;
        
        public CombatStance CurrentStance => currentStance;
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentStance, "currentStance", CombatStance.None);
            Scribe_Values.Look(ref virtuousUsesLeft, "virtuousUsesLeft", 0);
        }
        
        public void SetStance(CombatStance newStance)
        {
            if (currentStance == newStance)
            {
                currentStance = CombatStance.None;
                Messages.Message("Stance annulée", MessageTypeDefOf.NeutralEvent);
                return;
            }
            
            currentStance = newStance;
            
            if (newStance == CombatStance.Virtuous)
            {
                virtuousUsesLeft = 1;
            }
            
            ApplyStanceEffects();
            
            string stanceMsg = GetStanceDescription();
            Messages.Message($"Stance: {stanceMsg}", MessageTypeDefOf.NeutralEvent);
        }
        
        private void ApplyStanceEffects()
        {
            Pawn pawn = parent as Pawn;
            if (pawn == null) return;
            
            RemoveStanceHediffs(pawn);
            
            HediffDef stanceHediff = GetStanceHediff();
            if (stanceHediff != null)
            {
                Hediff hediff = HediffMaker.MakeHediff(stanceHediff, pawn);
                pawn.health.AddHediff(hediff);
            }
        }
        
        private HediffDef GetStanceHediff()
        {
            switch (currentStance)
            {
                case CombatStance.Offensive:
                    return HediffDef.Named("Expedition33_OffensiveStance");
                case CombatStance.Defensive:
                    return HediffDef.Named("Expedition33_DefensiveStance");
                case CombatStance.Virtuous:
                    return HediffDef.Named("Expedition33_VirtuousStance");
                default:
                    return null;
            }
        }
        
        private void RemoveStanceHediffs(Pawn pawn)
        {
            string[] stanceHediffs = {
                "Expedition33_OffensiveStance",
                "Expedition33_DefensiveStance", 
                "Expedition33_VirtuousStance"
            };
            
            foreach (string hediffName in stanceHediffs)
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(
                    HediffDef.Named(hediffName));
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }
        
        public void OnAttackUsed()
        {
            if (currentStance == CombatStance.Virtuous)
            {
                virtuousUsesLeft--;
                if (virtuousUsesLeft <= 0)
                {
                    SetStance(CombatStance.None);
                }
            }
        }
        
        private string GetStanceDescription()
        {
            switch (currentStance)
            {
                case CombatStance.Offensive:
                    return "Offensive (+50% dégâts, +50% dégâts reçus)";
                case CombatStance.Defensive:
                    return "Defensive (-25% dégâts reçus, bonus parade)";
                case CombatStance.Virtuous:
                    return "Virtuous (+200% dégâts pour une attaque)";
                default:
                    return "Aucune";
            }
        }
        
        public override string CompInspectStringExtra()
        {
            if (currentStance == CombatStance.None)
                return "Stance: Aucune";
            else
                return $"Stance: {GetStanceDescription()}";
        }
        
        // ✅ GIZMOS SANS HARMONY
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is Pawn pawn && pawn.IsColonistPlayerControlled)
            {
                yield return new Command_SetStance(this, CombatStance.Offensive);
                yield return new Command_SetStance(this, CombatStance.Defensive);
                yield return new Command_SetStance(this, CombatStance.Virtuous);
            }
        }
    }
    
    public class CompProperties_CombatStance : CompProperties
    {
        public CompProperties_CombatStance()
        {
            compClass = typeof(CompCombatStance);
        }
    }
    
    public class Command_SetStance : Command
    {
        private CompCombatStance stanceComp;
        private CombatStance targetStance;
        
        public Command_SetStance(CompCombatStance comp, CombatStance stance)
        {
            stanceComp = comp;
            targetStance = stance;
            
            switch (stance)
            {
                case CombatStance.Offensive:
                    defaultLabel = "Stance Offensive";
                    defaultDesc = "+50% dégâts, +50% dégâts reçus";
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true);
                    break;
                case CombatStance.Defensive:
                    defaultLabel = "Stance Défensive";
                    defaultDesc = "-25% dégâts reçus, bonus parade";
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                    break;
                case CombatStance.Virtuous:
                    defaultLabel = "Stance Vertueuse";
                    defaultDesc = "+200% dégâts pour une attaque";
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Fire", true);
                    break;
            }
        }
        
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            stanceComp.SetStance(targetStance);
        }
        
        // ✅ PROPRIÉTÉ VISIBLE CORRECTE
        public override bool Visible => stanceComp?.parent?.Spawned == true;
    }
}
