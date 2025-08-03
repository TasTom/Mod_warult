using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Mod_warult
{
    public class BiomeWorker_ContinentObscur : BiomeWorker
    {
        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            if (biome.defName != "Expedition33_ContinentObscur")
                return -100f;

            float score = 0f;
            score += (float)tile.elevation / 500f;
            score -= tile.temperature / 10f;

            return score;
        }
    }
}
