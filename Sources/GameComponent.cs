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
        public bool siteRevealed = false;
        private List<Map> importantMaps = new List<Map>();

        private const int TICKS_PER_DAY = 60000;
        private const int FIRST_GOMMAGE_DELAY_DAYS = 15; //15
        private const int GOMMAGE_CYCLE_DAYS = 60; //60

        public List<GommageHistoryEntry> gommageHistory = new List<GommageHistoryEntry>();

        public GameComponent_PaintressMonolith(Game game) : base()
        {
            if (importantMaps == null)
                importantMaps = new List<Map>();
        }

        public override void GameComponentTick()
        {
            if (!paintressAlive)
                return;

            if (!initialized && GenTicks.TicksGame > 600)
            {
                InitializePaintressSystem();
            }

            if (!paintressAlive) return;

            if (GenTicks.TicksGame % TICKS_PER_DAY == 0)
            {
                CheckAnnualGommageCycle();
            }
        }

        private void InitializePaintressSystem()
        {
            if (!paintressAlive)
                return;

            initialized = true;

            // CORRECTION : S'assurer que le délai est bien dans le futur
            int delayTicks = FIRST_GOMMAGE_DELAY_DAYS * TICKS_PER_DAY;
            nextPaintingTick = GenTicks.TicksGame + delayTicks;
            currentPaintedAge = Rand.Range(40, 55);

            Messages.Message(
                "Expedition33_PaintressSystemInit".Translate(currentPaintedAge, FIRST_GOMMAGE_DELAY_DAYS),
                MessageTypeDefOf.NeutralEvent
            );

            Log.Message($"Expedition33: System initialized at tick {GenTicks.TicksGame}. First Gommage scheduled for tick {nextPaintingTick} (in {delayTicks} ticks)");
        }


        private void CheckAnnualGommageCycle()
        {
            if (!paintressAlive || !initialized) // AJOUT : Vérifier que l'initialisation est terminée
                return;
            if (nextPaintingTick <= 0) return;

            int currentTick = GenTicks.TicksGame;
            int ticksUntilGommage = nextPaintingTick - currentTick;
            int warningTicks = 3 * TICKS_PER_DAY;

            // AJOUT : Debug pour voir les valeurs
            if (GenTicks.TicksGame % (TICKS_PER_DAY * 5) == 0) // Log tous les 5 jours
            {
                Log.Message($"Expedition33: Current tick {currentTick}, next Gommage at {nextPaintingTick}, ticks remaining: {ticksUntilGommage}");
            }

            if (ticksUntilGommage <= warningTicks && !hasBeenWarned)
            {
                hasBeenWarned = true;
                SendGommageWarning(ticksUntilGommage / TICKS_PER_DAY);
            }

            if (ticksUntilGommage <= 0)
            {
                ExecuteAnnualGommage();
                ScheduleNextAnnualGommage();
                hasBeenWarned = false;
            }
        }



        private void SendGommageWarning(int daysRemaining)
        {
            if (!paintressAlive)
                return;

            string letterText = "Expedition33_GommageWarningText".Translate(
                daysRemaining, currentPaintedAge, daysRemaining);

            Find.LetterStack.ReceiveLetter(
                "Expedition33_PaintressApproaches".Translate(),
                letterText,
                LetterDefOf.ThreatBig
            );
        }

        private void ExecuteAnnualGommage()
        {
            if (!paintressAlive)
                return;

            currentPaintedAge = CalculateNextAge();
            string letterText = "Expedition33_NewGommageAnnual".Translate(currentPaintedAge, currentPaintedAge);

            Find.LetterStack.ReceiveLetter(
                "Expedition33_NewGommageAnnualTitle".Translate(currentPaintedAge),
                letterText,
                LetterDefOf.ThreatBig
            );

            TriggerGommageIncident();
        }

        private void ScheduleNextAnnualGommage()
        {
            if (!paintressAlive)
                return;

            nextPaintingTick = GenTicks.TicksGame + (GOMMAGE_CYCLE_DAYS * TICKS_PER_DAY);
            Log.Message("Expedition33_NextGommageScheduled".Translate(nextPaintingTick));
        }

        private int CalculateNextAge()
        {
            currentPaintedAge--; // Décrémente d'abord
            return currentPaintedAge; // Puis retourne la nouvelle valeur
        }

        private void TriggerGommageIncident()
        {
            Log.Message("Expedition33: TriggerGommageIncident() called");

            if (!paintressAlive)
            {
                Log.Warning("Expedition33: Cannot trigger - Paintress not alive");
                return;
            }

            IncidentDef gommageIncident = DefDatabase<IncidentDef>.GetNamedSilentFail("Expedition33_Gommage");
            if (gommageIncident == null)
            {
                Log.Error("Expedition33: IncidentDef 'Expedition33_Gommage' not found!");
                Log.Error("Expedition33: Available IncidentDefs containing 'Expedition':");
                foreach (var def in DefDatabase<IncidentDef>.AllDefs.Where(d => d.defName.Contains("Expedition")))
                {
                    Log.Error($"  - {def.defName}");
                }
                return;
            }

            if (Find.CurrentMap == null)
            {
                Log.Warning("Expedition33: No current map available");
                return;
            }
            try
            {
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(gommageIncident.category, Find.CurrentMap);
                
                // CHANGEMENT : Exécution directe au lieu de programmation
                bool result = gommageIncident.Worker.TryExecute(parms);
                
                Log.Message($"Expedition33: Gommage incident executed directly - Result: {result}");
                
                if (!result)
                {
                    Log.Warning("Expedition33: Gommage incident execution failed - check CanFireNowSub conditions");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Expedition33: Error executing incident: {e.Message}");
            }
        }







        public int GetDaysUntilNextGommage()
        {
            if (nextPaintingTick <= 0) return -1;
            int ticksRemaining = nextPaintingTick - GenTicks.TicksGame;
            return Math.Max(0, ticksRemaining / TICKS_PER_DAY);
        }

        public bool IsGommageImminent(int warningDays = 3)
        {
            int daysRemaining = GetDaysUntilNextGommage();
            return daysRemaining >= 0 && daysRemaining <= warningDays;
        }

        public override string ToString()
        {
            if (!initialized) return "Expedition33_SystemNotInitialized".Translate();
            int daysRemaining = GetDaysUntilNextGommage();
            string status = paintressAlive ? "Expedition33_Active".Translate() : "Expedition33_Neutralized".Translate();
            return "Expedition33_PaintressStatus".Translate(status, currentPaintedAge, daysRemaining);
        }

        public void OnPaintressKilled()
        {
            paintressAlive = false;
            currentPaintedAge = -1;
            
            Find.LetterStack.ReceiveLetter(
                "Expedition33_GommageDefeated".Translate(),
                "Expedition33_PaintressKilledText".Translate(),
                LetterDefOf.PositiveEvent
            );
        }

        public void RecordGommageEvent(int age, int totalVictims, int protectedCount, int tickOccurred)
        {
            var entry = new GommageHistoryEntry
            {
                age = age,
                totalVictims = totalVictims,
                protectedCount = protectedCount,
                tickOccurred = tickOccurred,
                dayOccurred = tickOccurred / 60000
            };

            gommageHistory.Add(entry);
            if (gommageHistory.Count > 10)
            {
                gommageHistory.RemoveAt(0);
            }

            Log.Message("Expedition33_GommageRecorded".Translate(age, totalVictims, protectedCount));
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref currentPaintedAge, "currentPaintedAge", -1);
            Scribe_Values.Look(ref nextPaintingTick, "nextPaintingTick", -1);
            Scribe_Values.Look(ref paintressAlive, "paintressAlive", true);
            Scribe_Values.Look(ref worldTileMonolith, "worldTileMonolith", -1);
            Scribe_Values.Look(ref hasBeenWarned, "hasBeenWarned", false);
            Scribe_Values.Look(ref initialized, "initialized", false);
            Scribe_Values.Look(ref siteRevealed, "siteRevealed", false);
            Scribe_Collections.Look(ref importantMaps, "importantMaps", LookMode.Reference);
            if (importantMaps == null)
                importantMaps = new List<Map>();
            Scribe_Collections.Look(ref gommageHistory, "gommageHistory", LookMode.Deep);
            if (gommageHistory == null)
                gommageHistory = new List<GommageHistoryEntry>();
        }
    }

    public class GommageHistoryEntry : IExposable
    {
        public int age = 0;
        public int totalVictims = 0;
        public int protectedCount = 0;
        public int tickOccurred = 0;
        public int dayOccurred = 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref age, "age", 0);
            Scribe_Values.Look(ref totalVictims, "totalVictims", 0);
            Scribe_Values.Look(ref protectedCount, "protectedCount", 0);
            Scribe_Values.Look(ref tickOccurred, "tickOccurred", 0);
            Scribe_Values.Look(ref dayOccurred, "dayOccurred", 0);
        }

        public string GetStatusText()
        {
            if (totalVictims == 0)
                return "Expedition33_GommageBlocked".Translate();
            else if (protectedCount > 0)
                return "Expedition33_GommagePartial".Translate();
            else
                return "Expedition33_GommageTotal".Translate();
        }

        public Color GetStatusColor()
        {
            if (totalVictims == 0)
                return Color.green;
            else if (protectedCount > 0)
                return Color.yellow;
            else
                return Color.red;
        }
    }
}
