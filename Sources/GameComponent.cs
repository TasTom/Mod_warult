using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld.Planet;
using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse.Sound;

namespace Mod_warult
{
    // GameComponent - Stats
    public class GameComponent_ExpeditionStats : GameComponent
    {
        public int totalGommages = 0;
        public int survivedGommages = 0;
        public int completedMissions = 0;
        public int paintressSightings = 0;
        public List<int> cursedAges = new List<int>();

        // CORRECTION : Constructeur avec paramètre Game
        public GameComponent_ExpeditionStats(Game game) : base()
        {
            if (cursedAges == null)
                cursedAges = new List<int>();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref totalGommages, "totalGommages", 0);
            Scribe_Values.Look(ref survivedGommages, "survivedGommages", 0);
            Scribe_Values.Look(ref completedMissions, "completedMissions", 0);
            Scribe_Values.Look(ref paintressSightings, "paintressSightings", 0);
            Scribe_Collections.Look(ref cursedAges, "cursedAges", LookMode.Value);

            if (cursedAges == null)
                cursedAges = new List<int>();
        }

        public void RecordGommage(int age, bool hadVictims)
        {
            totalGommages++;
            if (!hadVictims) survivedGommages++;
            if (!cursedAges.Contains(age)) cursedAges.Add(age);
        }

        public void RecordMissionCompleted()
        {
            completedMissions++;
        }

        public void RecordPaintressSighting()
        {
            paintressSightings++;
        }
    }

    // GameComponent principal pour le monolithe
    public class GameComponent_PaintressMonolith : GameComponent
    {
        public int currentPaintedAge = -1;
        public int nextPaintingTick = -1;
        public bool paintressAlive = true;
        public int worldTileMonolith = -1;
        public bool hasBeenWarned = false;
        private bool initialized = false;
        private int initialWarningTick = -1;
        public bool siteRevealed = false;
        private List<Map> importantMaps = new List<Map>();

        // NOUVEAU - Variables pour la réapparition
        private int lastSiteCheckTick = -1;
        private bool needsSiteRecreation = false;
        private int siteRecreationAttempts = 0;
        private const int MAX_RECREATION_ATTEMPTS = 3;

        private const int TICKS_PER_YEAR = 3600000;
        private const int WARNING_DAYS_BEFORE = 1;

        // CORRECTION : Constructeur avec paramètre Game
        public GameComponent_PaintressMonolith(Game game) : base()
        {
            if (importantMaps == null)
                importantMaps = new List<Map>();
        }

        public void RegisterImportantMap(Map map)
        {
            if (!importantMaps.Contains(map))
            {
                importantMaps.Add(map);
                Log.Message($"Carte importante enregistrée : {map.info.parent?.Label ?? "Site du monolithe"}");
            }
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            if (!initialized)
            {
                InitializeMonolith();
                Log.Message("=== NOUVELLE PARTIE - MONOLITHE INITIALISÉ ===");
            }
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!initialized && currentPaintedAge == -1)
            {
                InitializeMonolith();
                Log.Message("=== PARTIE CHARGÉE - MONOLITHE INITIALISÉ ===");
            }
        }

        private void InitializeMonolith()
        {
            if (initialized) return;

            currentPaintedAge = Rand.Range(50, 61);
            nextPaintingTick = GenTicks.TicksGame + TICKS_PER_YEAR;
            initialWarningTick = GenTicks.TicksGame + 60000; // 1 jour complet

            worldTileMonolith = FindValidLandTile();
            initialized = true;

            Log.Message($"Monolithe initialisé - âge peint: {currentPaintedAge}");
            Log.Message($"Tuile terrestre sélectionnée: {worldTileMonolith}");
        }

        private int FindValidLandTile()
        {
            List<int> validTiles = new List<int>();

            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                Tile tile = Find.WorldGrid[i];

                if (IsValidMonolithTile(tile, i))
                {
                    validTiles.Add(i);
                }
            }

            if (validTiles.Count > 0)
            {
                int chosenTile = validTiles.RandomElement();
                Log.Message($"Tuile valide trouvée parmi {validTiles.Count} options : {chosenTile}");
                return chosenTile;
            }

            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                Tile tile = Find.WorldGrid[i];
                if (!tile.WaterCovered)
                {
                    Log.Warning($"Fallback - Tuile terrestre basique : {i}");
                    return i;
                }
            }

            Log.Error("Aucune tuile terrestre trouvée ! Utilise tuile 0");
            return 0;
        }

        private bool IsValidMonolithTile(Tile tile, int tileIndex)
        {
            if (tile.WaterCovered) return false;
            if (tile.temperature < -20f) return false;
            if (tile.hilliness == Hilliness.Impassable) return false;
            if (Find.WorldObjects.AnyWorldObjectAt(tileIndex)) return false;

            if (tile.PrimaryBiome != null)
            {
                string biomeName = tile.PrimaryBiome.defName;
                if (biomeName == "Ocean" || biomeName == "Lake" ||
                    biomeName == "IceSheet" || biomeName == "SeaIce")
                {
                    return false;
                }
            }

            return true;
        }

        public override void GameComponentTick()
        {
            if (!paintressAlive || !initialized) return;

            int currentTick = GenTicks.TicksGame;

            // Message initial
            if (initialWarningTick > 0 && currentTick >= initialWarningTick)
            {
                if (Find.WorldGrid != null && Find.WorldGrid.TilesCount > 0 &&
                    Find.FactionManager != null && Find.WorldObjects != null)
                {
                    SendInitialWarningAndRevealSite();
                    initialWarningTick = -1;
                }
                else
                {
                    initialWarningTick = currentTick + 1000;
                    Log.Message("Monde pas encore prêt, retard de la révélation du site");
                }
            }

            // NOUVEAU - Vérifie l'existence du site toutes les heures
            if (currentTick % 2500 == 0) // Toutes les heures (2500 ticks)
            {
                CheckMonolithSiteExists();
            }

            // NOUVEAU - Recrée le site si nécessaire
            if (needsSiteRecreation && currentTick > lastSiteCheckTick + 60000) // 1 jour après disparition
            {
                RecreateMonolithSite();
                needsSiteRecreation = false;
            }

            // Persistance forcée
            if (GenTicks.TicksGame % 60000 == 0)
            {
                ForceMonolithSitePersistence();

                foreach (Map importantMap in importantMaps.ToList())
                {
                    if (importantMap == null || importantMap.Index < 0)
                    {
                        importantMaps.Remove(importantMap);
                    }
                    else
                    {
                        Log.Message($"Maintien de la carte importante : {importantMap.info.parent?.Label ?? "Site du monolithe"}");
                    }
                }
            }

            int ticksUntilPainting = nextPaintingTick - currentTick;
            int daysUntilPainting = ticksUntilPainting / 60000;

            if (currentTick % 300000 == 0)
            {
                Log.Message($"Monolithe Status - Age: {currentPaintedAge}, Site révélé: {siteRevealed}");
            }

            if (daysUntilPainting <= WARNING_DAYS_BEFORE && !hasBeenWarned)
            {
                SendPaintingWarning();
                hasBeenWarned = true;
            }

            if (currentTick >= nextPaintingTick)
            {
                PaintNewAge();
                hasBeenWarned = false;
            }
        }

        // NOUVEAU - Vérifie si le site du monolithe existe encore
        private void CheckMonolithSiteExists()
        {
            if (!siteRevealed || worldTileMonolith == -1) return;

            try
            {
                Site monolithSite = Find.WorldObjects.Sites
                    .FirstOrDefault(s => s.Tile == worldTileMonolith);

                if (monolithSite == null)
                {
                    Log.Warning("Site du monolithe disparu - Programmation de la recréation");
                    needsSiteRecreation = true;
                    lastSiteCheckTick = GenTicks.TicksGame;
                    siteRevealed = false;
                    siteRecreationAttempts = 0;
                }
                else
                {
                    if (monolithSite.Map != null)
                    {
                        var paintress = monolithSite.Map.mapPawns.AllPawns
                            .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                        if (paintress == null || paintress.Dead)
                        {
                            Log.Message("Paintress morte détectée - Site sera recréé avec nouvelle Paintress");
                            needsSiteRecreation = true;
                            lastSiteCheckTick = GenTicks.TicksGame;
                            siteRecreationAttempts = 0;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans CheckMonolithSiteExists : {e.Message}");
            }
        }

        // NOUVEAU - Recrée le site du monolithe avec message dramatique
        private void RecreateMonolithSite()
        {
            if (siteRecreationAttempts >= MAX_RECREATION_ATTEMPTS)
            {
                Log.Warning($"Nombre maximum de tentatives de recréation atteint ({MAX_RECREATION_ATTEMPTS})");
                return;
            }

            try
            {
                siteRecreationAttempts++;
                Log.Message($"Recréation du site du monolithe en cours... (Tentative {siteRecreationAttempts}/{MAX_RECREATION_ATTEMPTS})");

                int newTile = FindValidLandTile();
                if (newTile != worldTileMonolith)
                {
                    worldTileMonolith = newTile;
                    Log.Message($"Nouvelle tuile sélectionnée pour le monolithe : {worldTileMonolith}");
                }

                CreateVisibleMonolithSite();

                string letterText = $"ALERTE EXPÉDITION 33 !\n\n" +
                                   $"Nos éclaireurs rapportent une découverte troublante :\n\n" +
                                   $"\"Le Monolithe de la Paintress est réapparu ! Elle a survécu et a reconstruit son domaine mystique.\"\n\n" +
                                   $"L'âge maudit reste le même : {currentPaintedAge} ans\n\n" +
                                   $"La Paintress semble plus déterminée que jamais. Son pouvoir artistique " +
                                   $"lui permet de recréer son sanctuaire même après sa destruction.\n\n" +
                                   $"Préparez une nouvelle expédition ! Cette fois, assurez-vous de l'éliminer définitivement !";

                Find.LetterStack.ReceiveLetter(
                    "LE MONOLITHE A RÉAPPARU !",
                    letterText,
                    LetterDefOf.ThreatBig,
                    new GlobalTargetInfo(worldTileMonolith)
                );

                Log.Message("Site du monolithe recréé avec succès");
            }
            catch (Exception e)
            {
                Log.Error($"Erreur lors de la recréation du site : {e.Message}");
                needsSiteRecreation = true;
                lastSiteCheckTick = GenTicks.TicksGame;
            }
        }

        // NOUVEAU - Force la recréation (pour debug)
        public void ForceRecreateMonolithSite()
        {
            needsSiteRecreation = true;
            lastSiteCheckTick = GenTicks.TicksGame - 60000;
            siteRevealed = false;
            siteRecreationAttempts = 0;
            Log.Message("Recréation forcée du site du monolithe programmée");
        }

        private void ForceMonolithSitePersistence()
        {
            try
            {
                Site monolithSite = Find.WorldObjects.Sites
                    .FirstOrDefault(s => s.Tile == worldTileMonolith);

                if (monolithSite?.Map != null)
                {
                    var paintress = monolithSite.Map.mapPawns.AllPawns
                        .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                    if (paintress != null && !paintress.Dead)
                    {
                        if (monolithSite.Map.mapPawns.FreeColonistsCount == 0)
                        {
                            Find.WorldObjects.Remove(monolithSite);
                            Find.WorldObjects.Add(monolithSite);
                            Log.Message("Site du monolithe forcé en persistance via hack");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans ForceMonolithSitePersistence : {e.Message}");
            }
        }

        private void SendInitialWarningAndRevealSite()
        {
            string letterText = $"Des éclaireurs de l'Expédition 33 rapportent une découverte terrifiante :\n\n" +
                               $"\"Nous avons localisé le monolithe mystique ! La Paintress y a déjà peint un numéro : {currentPaintedAge}\"\n\n" +
                               $"Tous vos colons de {currentPaintedAge} ans sont désormais en danger mortel. " +
                               $"Le Gommage peut survenir à tout moment.\n\n" +
                               $"L'emplacement du monolithe vient d'être révélé sur la carte du monde. " +
                               $"Formez une caravane et rendez-vous sur les lieux pour affronter la Paintress !\n\n" +
                               $"C'est votre seule chance de briser le cycle du Gommage !";

            Find.LetterStack.ReceiveLetter(
                "MONOLITHE DÉCOUVERT !",
                letterText,
                LetterDefOf.ThreatBig,
                new GlobalTargetInfo(worldTileMonolith)
            );

            CreateVisibleMonolithSite();
        }

        private void CreateVisibleMonolithSite()
        {
            if (siteRevealed) return;

            try
            {
                if (Find.WorldGrid == null || Find.WorldObjects == null)
                {
                    Log.Error("WorldGrid ou WorldObjects non initialisé");
                    CreateFallbackMessage();
                    return;
                }

                SitePartDef simpleSitePart = DefDatabase<SitePartDef>.GetNamedSilentFail("ItemStash");
                if (simpleSitePart == null)
                {
                    simpleSitePart = DefDatabase<SitePartDef>.AllDefs.FirstOrDefault();
                }

                if (simpleSitePart == null)
                {
                    Log.Error("Aucun SitePartDef disponible");
                    CreateFallbackMessage();
                    return;
                }

                Site monolithSite = SiteMaker.MakeSite(
                    simpleSitePart,
                    worldTileMonolith,
                    null
                );

                if (monolithSite == null)
                {
                    Log.Error("SiteMaker.MakeSite a retourné null");
                    CreateFallbackMessage();
                    return;
                }

                monolithSite.customLabel = "Monolithe de la Paintress";
                Find.WorldObjects.Add(monolithSite);
                siteRevealed = true;

                Log.Message($"Site standard créé avec succès à la tuile {worldTileMonolith}");
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans la création de site standard : {e.Message}");
                CreateFallbackMessage();
            }
        }

        private void CreateFallbackMessage()
        {
            try
            {
                siteRevealed = true;

                string letterText = $"Des éclaireurs de l'Expédition 33 rapportent :\n\n" +
                                   $"\"Nous avons localisé le Monolithe de la Paintress ! Elle y a peint le numéro : {currentPaintedAge}\"\n\n" +
                                   $"Tous vos colons de {currentPaintedAge} ans sont en danger mortel. " +
                                   $"Le Gommage peut survenir à tout moment.\n\n" +
                                   $"Malheureusement, les coordonnées exactes sont illisibles. " +
                                   $"Restez vigilants et préparez vos défenses !";

                Find.LetterStack.ReceiveLetter(
                    "MONOLITHE LOCALISÉ !",
                    letterText,
                    LetterDefOf.ThreatBig
                );

                Log.Message("Message de fallback envoyé - pas de site physique créé");
            }
            catch (Exception fallbackEx)
            {
                Log.Error($"Erreur même dans le fallback : {fallbackEx.Message}");
            }
        }

        private void SendPaintingWarning()
        {
            if (!initialized) return;

            string letterText = $"Des éclaireurs de l'Expédition 33 rapportent une activité inquiétante :\n\n" +
                               $"\"La Paintress s'approche de son monolithe mystique. Dans exactement 1 jour, " +
                               $"elle peindra un nouveau numéro qui déterminera l'âge des prochaines victimes du Gommage.\"\n\n" +
                               $"Âge actuellement peint : {currentPaintedAge} ans\n\n" +
                               $"Préparez-vous... Le destin de vos colons se joue demain.";

            Find.LetterStack.ReceiveLetter(
                "La Paintress s'approche du Monolithe",
                letterText,
                LetterDefOf.ThreatBig
            );
        }

        private void PaintNewAge()
        {
            if (!initialized) return;

            currentPaintedAge = Math.Max(20, currentPaintedAge - 1);
            nextPaintingTick = GenTicks.TicksGame + TICKS_PER_YEAR;

            string letterText = $"La Paintress a peint un nouveau numéro sur son monolithe !\n\n" +
                               $"ÂGE PEINT : {currentPaintedAge} ANS\n\n" +
                               $"Tous vos colons de cet âge exact sont maintenant en danger mortel. " +
                               $"Le Gommage peut survenir à tout moment.\n\n" +
                               $"Seule l'élimination de la Paintress peut briser ce cycle maudit !";

            Find.LetterStack.ReceiveLetter(
                $"NOUVEAU GOMMAGE : {currentPaintedAge} ANS",
                letterText,
                LetterDefOf.ThreatBig
            );

            ScheduleGommageEvent();
        }

        private void ScheduleGommageEvent()
        {
            if (!initialized) return;

            IncidentDef gommageIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("Expedition33_Gommage");
            if (gommageIncident != null && Find.CurrentMap != null)
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(gommageIncident.category, Find.CurrentMap);
                Find.Storyteller.incidentQueue.Add(gommageIncident, GenTicks.TicksGame + Rand.Range(60000, 300000), parms);
            }
        }

        public void OnPaintressKilled()
        {
            paintressAlive = false;
            currentPaintedAge = -1;

            string letterText = "La Paintress a été éliminée !\n\n" +
                               "Son monolithe mystique s'effrite en poussière. Le cycle du Gommage est brisé. " +
                               "Vos colons sont enfin en sécurité, libérés de cette terreur artistique.\n\n" +
                               "L'Expédition 33 peut enfin connaître la paix.";

            Find.LetterStack.ReceiveLetter(
                "Le Gommage est vaincu !",
                letterText,
                LetterDefOf.PositiveEvent
            );
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref currentPaintedAge, "currentPaintedAge", -1);
            Scribe_Values.Look(ref nextPaintingTick, "nextPaintingTick", -1);
            Scribe_Values.Look(ref paintressAlive, "paintressAlive", true);
            Scribe_Values.Look(ref worldTileMonolith, "worldTileMonolith", -1);
            Scribe_Values.Look(ref hasBeenWarned, "hasBeenWarned", false);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref initialWarningTick, "initialWarningTick", -1);
            Scribe_Values.Look(ref siteRevealed, "siteRevealed", false);
            Scribe_Values.Look(ref lastSiteCheckTick, "lastSiteCheckTick", -1);
            Scribe_Values.Look(ref needsSiteRecreation, "needsSiteRecreation", false);
            Scribe_Values.Look(ref siteRecreationAttempts, "siteRecreationAttempts", 0);
            Scribe_Collections.Look(ref importantMaps, "importantMaps", LookMode.Reference);

            if (importantMaps == null)
                importantMaps = new List<Map>();
        }
    }
}
