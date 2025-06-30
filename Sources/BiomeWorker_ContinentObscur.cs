using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Mod_warult
{
    public class BiomeWorker_ContinentObscur : BiomeWorker
    {
        public override float GetScore(Tile tile, int tileID)
        {
            if (tile.WaterCovered)
                return -100f;

            if (tile.temperature < 5f || tile.temperature > 35f)
                return 0f;

            if (tile.hilliness == Hilliness.Flat)
                return 0f;

            if (tile.rainfall < 400f)
                return 0f;

            // Bonus pour les zones montagneuses
            if (tile.hilliness == Hilliness.Mountainous)
                return 25f;

            if (tile.hilliness == Hilliness.LargeHills)
                return 20f;

            if (tile.hilliness == Hilliness.SmallHills)
                return 15f;

            return 10f;
        }

    }
}