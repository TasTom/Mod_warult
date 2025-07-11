using RimWorld;
using Verse;

namespace Mod_warult
{
    // Bouclier anti-Gommage
    public class Apparel_AntiGommageShield : Apparel
    {
        public int protectionCharges = 3;
        public bool isActive = true;

        protected override void Tick()
        {
            base.Tick();

            if (Wearer != null && isActive)
            {
                CheckGommageProtection();
            }
        }

        private void CheckGommageProtection()
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.paintressAlive) return;

            if (Wearer.ageTracker.AgeBiologicalYears == gameComp.currentPaintedAge)
            {
                if (protectionCharges > 0)
                {
                    Hediff protectionBuff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection"),
                        Wearer
                    );

                    if (protectionBuff != null && !Wearer.health.hediffSet.HasHediff(protectionBuff.def))
                    {
                        Wearer.health.AddHediff(protectionBuff);
                    }
                }
            }
        }

        public void ConsumeCharge()
        {
            protectionCharges--;
            if (protectionCharges <= 0)
            {
                isActive = false;

                Messages.Message(
                    $"Le bouclier anti-Gommage de {Wearer.Name.ToStringShort} est �puis� !",
                    MessageTypeDefOf.NegativeEvent
                );
            }
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            baseString += $"\nCharges restantes : {protectionCharges}/3";
            baseString += $"\nStatut : {(isActive ? "Actif" : "�puis�")}";
            return baseString;
        }
    }

    // Arme artistique
    public class Weapon_ArtisticBlade : ThingWithComps
    {
        private float artisticPower = 1.0f;
        private int paintCharges = 10;

        public override void PostMake()
        {
            base.PostMake();

            CompQuality qualityComp = this.TryGetComp<CompQuality>();
            if (qualityComp != null)
            {
                artisticPower = 1.0f + ((int)qualityComp.Quality * 0.2f);
            }
        }

        public float GetArtisticDamageMultiplier(Pawn target)
        {
            if (target?.kindDef?.defName == "Expedition33_PaintressMonster")
            {
                return artisticPower * 2.0f;
            }

            if (target?.kindDef?.defName?.Contains("Painted") == true)
            {
                return artisticPower * 1.5f;
            }

            return 1.0f;
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            baseString += $"\nPouvoir artistique : {artisticPower:F1}x";
            baseString += $"\nCharges de peinture : {paintCharges}/10";
            return baseString;
        }
    }
}
