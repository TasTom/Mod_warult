using RimWorld;
using Verse;

namespace Mod_warult
{
    [StaticConstructorOnStartup]
    public static class ExpeditionResearchDefs
    {
        // Tier 1 - Recherches de base
        public static ResearchProjectDef GommageAnalysis;
        public static ResearchProjectDef ArtisticDetection;
        public static ResearchProjectDef BasicProtection;

        // Tier 2 - Recherches interm�diaires
        public static ResearchProjectDef CursedAgeDetector;
        public static ResearchProjectDef AntiGommageShield;
        public static ResearchProjectDef ArtisticWeapons;

        // Tier 3 - Recherches avanc�es
        public static ResearchProjectDef AntiGommageField;
        public static ResearchProjectDef CounterPaintBrush;
        public static ResearchProjectDef TemporalSanctuary;

        static ExpeditionResearchDefs()
        {
            CacheResearchDefs();
        }

        private static void CacheResearchDefs()
        {
            GommageAnalysis = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_GommageAnalysis");
            ArtisticDetection = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_ArtisticDetection");
            BasicProtection = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_BasicProtection");

            CursedAgeDetector = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_CursedAgeDetector");
            AntiGommageShield = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_AntiGommageShield");
            ArtisticWeapons = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_ArtisticWeapons");

            AntiGommageField = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_AntiGommageField");
            CounterPaintBrush = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_CounterPaintBrush");
            TemporalSanctuary = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Expedition33_TemporalSanctuary");
        }
    }
}
