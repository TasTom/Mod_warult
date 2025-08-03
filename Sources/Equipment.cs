using RimWorld;
using Verse;

namespace Mod_warult
{
    // Bouclier anti-Gommage
    public class Apparel_AntiGommageShield : Apparel
    {
        public int protectionCharges = 3;
        public bool isActive = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref protectionCharges, "protectionCharges", 3);
            Scribe_Values.Look(ref isActive, "isActive", true);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Wearer != null && isActive && protectionCharges > 0)
            {
                CheckGommageProtection();
            }
        }

        private void CheckGommageProtection()
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null || !gameComp.paintressAlive) return;

            // Protection pour tous les colons >= à l'âge maudit
            if (Wearer.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge)
            {
                var existingProtection = Wearer.health.hediffSet.GetFirstHediffOfDef(
                    DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
                );
                if (existingProtection == null)
                {
                    Hediff protectionBuff = HediffMaker.MakeHediff(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection"),
                        Wearer
                    );
                    if (protectionBuff != null)
                    {
                        Wearer.health.AddHediff(protectionBuff);
                        Messages.Message(
                            "Expedition33_ShieldActivated".Translate(Wearer.Name.ToStringShort),
                            MessageTypeDefOf.NeutralEvent
                        );
                    }
                }
            }
        }

        public void ConsumeCharge()
        {
            if (protectionCharges > 0)
            {
                protectionCharges--;
                Log.Message("Expedition33_ShieldChargeConsumed".Translate(
                    Wearer?.Name?.ToStringShort ?? "Unknown", protectionCharges + 1, protectionCharges));
                Messages.Message(
                    "Expedition33_ShieldChargesRemaining".Translate(Wearer.Name.ToStringShort, protectionCharges),
                    MessageTypeDefOf.NeutralEvent
                );
                if (protectionCharges <= 0)
                {
                    isActive = false;
                    // Supprimer le hediff de protection
                    var protection = Wearer.health.hediffSet.GetFirstHediffOfDef(
                        DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
                    );
                    if (protection != null)
                    {
                        Wearer.health.RemoveHediff(protection);
                    }

                    Messages.Message(
                        "Expedition33_ShieldExhausted".Translate(Wearer.Name.ToStringShort),
                        MessageTypeDefOf.NegativeEvent
                    );
                }
            }
            else
            {
                Log.Warning("Expedition33_ShieldChargeEmpty".Translate(Wearer?.Name?.ToStringShort ?? "Unknown"));
            }
        }

        public override string GetInspectString()
        {
            string baseString = base.GetInspectString();
            string statusColor = isActive ? "✅" : "❌";
            baseString += "\n" + "Expedition33_ShieldStatus".Translate(statusColor, protectionCharges);
            baseString += "\n" + "Expedition33_ShieldState".Translate(isActive ? "Expedition33_Active".Translate() : "Expedition33_Exhausted".Translate());
            if (isActive && protectionCharges > 0)
            {
                baseString += "\n" + "Expedition33_ShieldProtection".Translate();
            }
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
            if (target?.kindDef?.defName == "Expedition33_Paintress")
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
            baseString += "\n" + "Expedition33_ArtisticPower".Translate(artisticPower.ToString("F1"));
            baseString += "\n" + "Expedition33_PaintCharges".Translate(paintCharges);
            return baseString;
        }
    }
}
