using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Mod_warult
{
    [DefOf]
    public static class Expedition33_ApparelLayerDefOf
    {
        public static ApparelLayerDef Expedition33_Accessories;

        static Expedition33_ApparelLayerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(Expedition33_ApparelLayerDefOf));
        }
    }

    public static class PawnAccessoryExtensions
    {
        private const int MAX_ACCESSORIES = 4;

        public static bool CanEquipAccessory(this Pawn pawn, Apparel accessory)
        {
            if (pawn?.apparel?.WornApparel == null) return false;

            var accessoryLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("Expedition33_Accessories");
            if (accessoryLayer == null) return true;

            var currentAccessories = pawn.apparel.WornApparel
                .Where(a => a.def.apparel.layers.Contains(accessoryLayer))
                .Count();

            return currentAccessories < MAX_ACCESSORIES;
        }

        public static int GetAccessoryCount(this Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) return 0;

            var accessoryLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("Expedition33_Accessories");
            if (accessoryLayer == null) return 0;

            return pawn.apparel.WornApparel
                .Count(a => a.def.apparel.layers.Contains(accessoryLayer));
        }

        public static List<Apparel> GetEquippedAccessories(this Pawn pawn)
        {
            if (pawn?.apparel?.WornApparel == null) return new List<Apparel>();

            var accessoryLayer = DefDatabase<ApparelLayerDef>.GetNamedSilentFail("Expedition33_Accessories");
            if (accessoryLayer == null) return new List<Apparel>();

            return pawn.apparel.WornApparel
                .Where(a => a.def.apparel.layers.Contains(accessoryLayer))
                .ToList();
        }

        public static bool TryEquipAccessory(this Pawn pawn, Apparel accessory)
        {
            if (!pawn.CanEquipAccessory(accessory))
            {
                Messages.Message(
                    "Expedition33_MaxAccessoriesReached".Translate(pawn.Name.ToStringShort, MAX_ACCESSORIES),
                    MessageTypeDefOf.RejectInput
                );
                return false;
            }

            return true;
        }
    }

    public class CompProperties_AccessoryLimit : CompProperties
    {
        public string slotType = "default";
        public int maxPerType = 1;

        public CompProperties_AccessoryLimit()
        {
            this.compClass = typeof(CompAccessoryLimit);
        }
    }

    public class CompAccessoryLimit : ThingComp
    {
        public CompProperties_AccessoryLimit Props => (CompProperties_AccessoryLimit)this.props;

        public override bool AllowStackWith(Thing other)
        {
            return false;
        }

        public override string CompInspectStringExtra()
        {
            if (parent.holdingOwner?.Owner is Pawn pawn)
            {
                int accessoryCount = pawn.GetAccessoryCount();
                return "Expedition33_AccessoriesCount".Translate(accessoryCount);
            }

            return null;
        }
    }

    public class Alert_TooManyAccessories : Alert
    {
        public Alert_TooManyAccessories()
        {
            this.defaultLabel = "Expedition33_TooManyAccessoriesAlert".Translate();
            this.defaultExplanation = "Expedition33_TooManyAccessoriesExplanation".Translate();
        }

        public override AlertReport GetReport()
        {
            return GetProblematicPawns().Any();
        }

        private IEnumerable<Pawn> GetProblematicPawns()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn colonist in map.mapPawns.FreeColonistsSpawned)
                {
                    if (colonist.GetAccessoryCount() > 4)
                    {
                        yield return colonist;
                    }
                }
            }
        }

        public override TaggedString GetExplanation()
        {
            var problematicPawns = GetProblematicPawns().Take(3);
            string text = this.defaultExplanation + "\n\n" + "Expedition33_AffectedColonists".Translate() + "\n";
            
            foreach (var pawn in problematicPawns)
            {
                text += "Expedition33_ColonistAccessoryCount".Translate(pawn.Name.ToStringShort, pawn.GetAccessoryCount());
            }

            return text;
        }
    }
}
