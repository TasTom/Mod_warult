
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Mod_warult
{
    public static class PaintressManager
    {
        public static void SpawnPaintressOnObscurContinent()
        {
            int tile = FindObscurContinentTile();
            if (tile < 0)
            {
                Log.Error("Aucune tuile Continent Obscur trouvée !");
                return;
            }

            SitePartDef partDef = DefDatabase<SitePartDef>.GetNamedSilentFail("Expedition33_PeintresseSitePart");
            if (partDef == null)
            {
                Log.Error("SitePartDef Monolith introuvable !");
                return;
            }

            Faction bossFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("Expedition33")) ?? Faction.OfAncientsHostile;
            Site site = SiteMaker.MakeSite(partDef, tile, bossFaction);
            site.SetFaction(bossFaction);
            Find.WorldObjects.Add(site);

            Find.LetterStack.ReceiveLetter(
                "CONFRONTATION FINALE",
                "Le Monolithe de la Paintress est révélé sur le Continent Obscur. Préparez-vous à l’affronter !",
                LetterDefOf.ThreatBig,
                site
            );
        }

        private static int FindObscurContinentTile()
        {
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                var tile = Find.WorldGrid[i];
                if (tile.PrimaryBiome?.defName == "Expedition33_ContinentObscur")
                    return i;
            }
            return -1;
        }
    }

}