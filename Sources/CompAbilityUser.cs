using System.Collections.Generic;
using Verse;
using RimWorld;
using Verse.AI;
using UnityEngine;

namespace Mod_warult
{
    // Composant qui équipe le pawn d'abilities custom
    public class CompAbilityUser : ThingComp
    {
        public CompProperties_AbilityUser Props => (CompProperties_AbilityUser)props;
        private DefModExtension_AbilityUser extensionProps;
        private List<AbilityDef> learnedAbilities = new List<AbilityDef>();
        private Dictionary<AbilityDef, int> abilityCooldowns = new Dictionary<AbilityDef, int>();

        public Pawn AbilityUserPawn => parent as Pawn;

        public DefModExtension_AbilityUser ExtensionProps
        {
            get
            {
                if (extensionProps == null && AbilityUserPawn?.def != null)
                    extensionProps = AbilityUserPawn.def.GetModExtension<DefModExtension_AbilityUser>();
                return extensionProps;
            }
        }

        // Ajout automatique des abilities référencées dans le DefModExtension
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad && ExtensionProps?.abilityDefs != null)
            {
                foreach (var abilityDef in ExtensionProps.abilityDefs)
                    LearnAbility(abilityDef);
            }
        }

        public void LearnAbility(AbilityDef abilityDef)
        {
            if (abilityDef == null || learnedAbilities.Contains(abilityDef))
                return;
            learnedAbilities.Add(abilityDef);
            // Pas besoin de .GainAbility - gestion custom ici
        }

        public bool CanCastAbility(AbilityDef abilityDef)
        {
            if (!learnedAbilities.Contains(abilityDef))
                return false;
            if (abilityCooldowns.TryGetValue(abilityDef, out int cooldownEnd))
                return GenTicks.TicksGame >= cooldownEnd;
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
            var expired = new List<AbilityDef>();
            foreach (var kvp in abilityCooldowns)
                if (GenTicks.TicksGame >= kvp.Value)
                    expired.Add(kvp.Key);
            foreach (var def in expired)
                abilityCooldowns.Remove(def);
        }

        // Utilisation réelle de la capacité : lance un Job dédié
        public void ForceUseAbility(AbilityDef abilityDef, LocalTargetInfo target)
        {
            if (!CanCastAbility(abilityDef))
            {
                Log.Warning("Expedition33_AbilityCannotUse".Translate(abilityDef.defName));
                return;
            }

            if (AbilityUserPawn == null || !AbilityUserPawn.Spawned)
            {
                Log.Error("Expedition33_AbilityUserPawnInvalid".Translate());
                return;
            }

            if (abilityDef == null)
            {
                Log.Error("Expedition33_AbilityDefNull".Translate());
                return;
            }

            // Création d'un Job
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Expedition33_ExecuteAbility", true), target);
            
            // Initialisation du verbe personnalisé de capacité
            var verb = new Verb_ExecuteAbility
            {
                caster = AbilityUserPawn,
                abilityDef = abilityDef,
                range = abilityDef.verbProperties?.range ?? 12f,
                cooldown = abilityDef.cooldownTicksRange.min,
                ability = new BossAbility( // uniquement si tu l'utilises encore, sinon retire-le
                    abilityDef.defName.Replace("Expedition33_", ""),
                    abilityDef.verbProperties?.range ?? 12f,
                    abilityDef.targetRequired == false
                )
            };

            // Liaison avec le job
            job.verbToUse = verb;
            Log.Message("Expedition33_AbilityJobRequested".Translate(abilityDef.label, target.ToStringSafe()));

            // Soumission du job
            AbilityUserPawn.jobs.StopAll(); // force l'arrêt du job actuel
            AbilityUserPawn.jobs.StartJob(job, JobCondition.InterruptForced, resumeCurJobAfterwards: false);

            // Démarrage du cooldown
            StartCooldown(abilityDef);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var abilityDef in learnedAbilities)
            {
                bool isDisabled = !CanCastAbility(abilityDef);
                string reason = isDisabled ? GetCooldownString(abilityDef) : null;
                var targetingParams = abilityDef.verbProperties?.targetParams;

                // Détection des self-cast uniquement
                bool selfCastOnly =
                    abilityDef.targetRequired == false &&
                    targetingParams?.canTargetSelf == true &&
                    targetingParams?.canTargetPawns != true &&
                    targetingParams?.canTargetLocations != true;

                if (selfCastOnly)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = abilityDef.label,
                        defaultDesc = abilityDef.description,
                        icon = abilityDef.uiIcon,
                        action = () => ForceUseAbility(abilityDef, AbilityUserPawn),
                        Disabled = isDisabled,
                        disabledReason = reason
                    };
                }
                else
                {
                    var gizmo = new Command_TargetAbility(this, abilityDef)
                    {
                        Disabled = isDisabled,
                        disabledReason = reason
                    };
                    yield return gizmo;
                }
            }
        }

        // Helper pour afficher le cooldown restant
        private string GetCooldownString(AbilityDef abilityDef)
        {
            if (abilityCooldowns.TryGetValue(abilityDef, out int cooldownEnd))
            {
                int ticksLeft = cooldownEnd - GenTicks.TicksGame;
                if (ticksLeft > 0)
                    return "Expedition33_AbilityCooldown".Translate((ticksLeft / 60f).ToString("0.0"));
            }
            return null;
        }

        public bool TryGetCooldownTicksLeft(AbilityDef def, out int ticksLeft)
        {
            if (abilityCooldowns.TryGetValue(def, out int endTick))
            {
                ticksLeft = endTick - GenTicks.TicksGame;
                return true;
            }
            ticksLeft = 0;
            return false;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref learnedAbilities, "learnedAbilities", LookMode.Def);
            Scribe_Collections.Look(ref abilityCooldowns, "abilityCooldowns", LookMode.Def, LookMode.Value);
        }
    }

    // Définition du composant à déclarer dans le XML ThingDef (section comps)
    public class CompProperties_AbilityUser : CompProperties
    {
        public List<AbilityDef> abilityDefs = new List<AbilityDef>();
        public bool autocast = true;

        public CompProperties_AbilityUser()
        {
            compClass = typeof(CompAbilityUser);
        }
    }

    // Extension pour lister les abilities disponibles dans un Def (section modExtensions)
    public class DefModExtension_AbilityUser : DefModExtension
    {
        public List<AbilityDef> abilityDefs;
        public bool autocast = true;
    }

    // Exemple de verbe de capacité (DOIT exister/être relié dans l'AbilityDef)
    public class Verb_ExecuteAbility : Verb
    {
        public AbilityDef abilityDef; // capacité assignée
        public BossAbility ability; // données internes
        public float range;
        public int cooldown;
        
            /* --------- Portées --------- */
        public float MinRange
            => ability != null ? 0f : (verbProps?.minRange ?? 0f);      // aucune limite mini

        public float Range
            => ability != null ? ability.range            // ex : 25f
                               : verbProps?.range ?? 12f;          // fallback

        protected override bool TryCastShot()
        {
            Log.Message("Expedition33_AbilityExecuting".Translate(abilityDef?.defName ?? "[UNKNOWN]"));
            Pawn targetPawn = currentTarget.Pawn;
            Pawn casterPawn = this.caster as Pawn;

            if (targetPawn != null && !targetPawn.Dead)
            {
                targetPawn.TakeDamage(new DamageInfo(DamageDefOf.EMP, 12f, 0f, -1f, casterPawn));
                FleckMaker.ThrowLightningGlow(targetPawn.Position.ToVector3Shifted(), targetPawn.Map, 2.5f);
                Messages.Message("Expedition33_AbilityCastOnTarget".Translate(targetPawn.LabelShortCap),
                    targetPawn, MessageTypeDefOf.TaskCompletion);
            }
            return true;
        }
    }

    public class Command_TargetAbility : Command_Target, ITargetingSource
    {
        private readonly CompAbilityUser comp;
        private readonly AbilityDef abilityDef;

        public Command_TargetAbility(CompAbilityUser comp, AbilityDef def)
        {
            this.comp = comp;
            this.abilityDef = def;
            defaultLabel = def.label;
            defaultDesc = def.description;
            icon = def.uiIcon;
            targetingParams = def.verbProperties?.targetParams;
        }

        public override bool Disabled => !comp.CanCastAbility(abilityDef);

        public string DisableReason
        {
            get
            {
                if (comp.TryGetCooldownTicksLeft(abilityDef, out int ticksLeft))
                {
                    float seconds = Mathf.Max(0, ticksLeft) / 60f;
                    return "Expedition33_AbilityCooldownRemaining".Translate(seconds.ToString("0.0"));
                }
                return "Expedition33_AbilityUnavailable".Translate();
            }
        }

        public override void ProcessInput(UnityEngine.Event ev)
        {
            base.ProcessInput(ev);
            Find.Targeter.BeginTargeting(this);
        }

        public Pawn CasterPawn => comp.parent as Pawn;
        public Thing Caster => comp.parent;
        public bool CasterIsPawn => true;
        public Texture2D UIIcon => ContentFinder<Texture2D>.Get("UI/Commands/Expedition33_MissionIcon", true);
        public Verb GetVerb => null;
        public bool MultiSelect => false;
        public bool HidePawnTooltips => false;
        public TargetingParameters targetParams => targetingParams;
        public float GetRange() => abilityDef.verbProperties?.range ?? 12f;
        public bool IsMeleeAttack => false;
        public bool Targetable => true;

        public void OrderForceTarget(LocalTargetInfo target)
        {
            comp.ForceUseAbility(abilityDef, target);
        }

        public void DrawHighlight(LocalTargetInfo target)
        {
            if (CasterPawn != null && GetRange() > 0)
            {
                // Cercle de portée (autour du lanceur)
                GenDraw.DrawRadiusRing(CasterPawn.Position, GetRange());
                // Cercle d'aperçu (highlight) autour de la case sous la souris
                if (target.IsValid)
                    GenDraw.DrawTargetHighlight(target);
            }
        }

        public void OnGUI(LocalTargetInfo target) { }

        public bool CanHitTarget(LocalTargetInfo target)
        {
            if (!target.IsValid || targetingParams == null)
                return false;
            TargetInfo tInfo = target.HasThing
                ? new TargetInfo(target.Thing)
                : new TargetInfo(target.Cell, CasterPawn.Map);
            return targetingParams.CanTarget(tInfo);
        }

        public bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            return CanHitTarget(target);
        }

        public ITargetingSource DestinationSelector => null;
    }
}
