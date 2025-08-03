using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using Verse.Sound;
using System;

namespace Mod_warult
{
    public enum CombatStance
    {
        None,
        Offensive, // +50% dégâts, +50% dégâts reçus
        Defensive, // -25% dégâts reçus, +1 AP sur parade
        Virtuous // +200% dégâts pour une attaque
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
                Messages.Message("Expedition33_StanceCancelled".Translate(), MessageTypeDefOf.NeutralEvent);
                RemoveStanceHediffs(parent as Pawn);
                return;
            }

            currentStance = newStance;
            if (newStance == CombatStance.Virtuous)
                virtuousUsesLeft = 1;

            ApplyStanceEffects();
            Messages.Message("Expedition33_StanceChanged".Translate(GetStanceDescription()), MessageTypeDefOf.NeutralEvent);
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
            return currentStance switch
            {
                CombatStance.Offensive => HediffDef.Named("Expedition33_OffensiveStance"),
                CombatStance.Defensive => HediffDef.Named("Expedition33_DefensiveStance"),
                CombatStance.Virtuous => HediffDef.Named("Expedition33_VirtuousStance"),
                _ => null
            };
        }

        private void RemoveStanceHediffs(Pawn pawn)
        {
            foreach (var hediffName in new[]
            {
                "Expedition33_OffensiveStance",
                "Expedition33_DefensiveStance",
                "Expedition33_VirtuousStance"
            })
            {
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named(hediffName));
                if (hediff != null)
                    pawn.health.RemoveHediff(hediff);
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
            return currentStance switch
            {
                CombatStance.Offensive => "Expedition33_OffensiveStanceDesc".Translate(),
                CombatStance.Defensive => "Expedition33_DefensiveStanceDesc".Translate(),
                CombatStance.Virtuous => "Expedition33_VirtuousStanceDesc".Translate(),
                _ => "Expedition33_NoStance".Translate()
            };
        }

        public override string CompInspectStringExtra()
        {
            return "Expedition33_CurrentStance".Translate(GetStanceDescription());
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is Pawn pawn && pawn.Faction == Faction.OfPlayer)
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
        private readonly CombatStance targetStance;
        private readonly CompCombatStance stanceComp;

        public Command_SetStance(CompCombatStance comp, CombatStance stance)
        {
            stanceComp = comp;
            targetStance = stance;
            defaultDesc = "Expedition33_ChangeStanceDesc".Translate();

            switch (stance)
            {
                case CombatStance.Offensive:
                    defaultLabel = "Expedition33_OffensiveStance".Translate();
                    defaultDesc += "\n" + "Expedition33_OffensiveStanceBonuses".Translate();
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true);
                    break;
                case CombatStance.Defensive:
                    defaultLabel = "Expedition33_DefensiveStance".Translate();
                    defaultDesc += "\n" + "Expedition33_DefensiveStanceBonuses".Translate();
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt", true);
                    break;
                case CombatStance.Virtuous:
                    defaultLabel = "Expedition33_VirtuousStance".Translate();
                    defaultDesc += "\n" + "Expedition33_VirtuousStanceBonuses".Translate();
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Fire", true);
                    break;
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            stanceComp.SetStance(targetStance);
            SoundDefOf.Click.PlayOneShotOnCamera();
        }

        public override bool Visible => stanceComp?.parent?.Spawned == true;
    }
}
