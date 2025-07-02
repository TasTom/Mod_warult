using System;
using RimWorld;
using Verse;

namespace Mod_warult
{
    public class CompProperties_PictoProgress : CompProperties
    {
        public string luminaDefName;
        public int battlesRequired = 4;
        public string pictoType;

        public CompProperties_PictoProgress()
        {
            this.compClass = typeof(CompPictoProgress);
        }
    }

    public class CompPictoProgress : ThingComp
    {
        private int battlesWon = 0;
        private bool luminaUnlocked = false;

        public CompProperties_PictoProgress Props => (CompProperties_PictoProgress)this.props;

        public int BattlesWon => battlesWon;
        public int BattlesRequired => Props.battlesRequired;
        public bool IsLuminaUnlocked => luminaUnlocked;
        public float ProgressPercentage => (float)battlesWon / Props.battlesRequired;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref battlesWon, "battlesWon", 0);
            Scribe_Values.Look(ref luminaUnlocked, "luminaUnlocked", false);
        }

        public void RegisterBattleVictory(Pawn wearer)
        {
            if (luminaUnlocked) return;

            battlesWon++;
            
            // Message de progression
            Messages.Message(
                $"{parent.Label} : {battlesWon}/{Props.battlesRequired} victoires",
                MessageTypeDefOf.NeutralEvent
            );

            // Vérifier si la Lumina est débloquée
            if (battlesWon >= Props.battlesRequired)
            {
                UnlockLumina(wearer);
            }
        }

        private void UnlockLumina(Pawn wearer)
        {
            luminaUnlocked = true;
            
            // Ajouter l'hediff Lumina au porteur
            HediffDef luminaDef = HediffDef.Named(Props.luminaDefName);
            if (luminaDef != null)
            {
                wearer.health.AddHediff(luminaDef);
            }

            // Message de débloquage
            Find.LetterStack.ReceiveLetter(
                "Lumina Débloquée !",
                $"{wearer.Name.ToStringShort} a maîtrisé la Lumina '{Props.luminaDefName}' grâce au {parent.Label} !\n\n" +
                $"Cette compétence passive restera active même sans porter le Picto.",
                LetterDefOf.PositiveEvent,
                wearer
            );

            Log.Message($"[Expedition33] Lumina {Props.luminaDefName} débloquée pour {wearer.Name.ToStringShort}");
        }

        public override string CompInspectStringExtra()
        {
            if (luminaUnlocked)
            {
                return $"Lumina débloquée : {Props.luminaDefName}";
            }
            else
            {
                return $"Progression Lumina : {battlesWon}/{Props.battlesRequired} victoires";
            }
        }
    }
}
