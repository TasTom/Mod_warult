using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Mod_warult
{
    public class BiomeWorker_ContinentObscur : BiomeWorker
    {
        // Signature 1.4+
        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            // Exemple de logique : donner un score élevé si tile.biome == biome
            if (biome.defName != "Expedition33_ContinentObscur")
                return -100f;                 // non concerné

            float score = 0f;

            // Facteurs basiques
            score += (float)tile.elevation / 500f;     // plus haut = plus obscur
            score -= tile.temperature / 10f;           // climat froid favorisé
                                                       // marécageux

            return score;
        }
    }
}

