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
