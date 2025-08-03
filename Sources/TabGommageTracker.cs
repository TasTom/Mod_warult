using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Mod_warult
{
    public class MainTabWindow_GommageTracker : MainTabWindow
    {
        private Vector2 scrollPosition;
        private static readonly Vector2 WinSize = new Vector2(800f, 600f);

        public override Vector2 RequestedTabSize => WinSize;

        public override void DoWindowContents(Rect inRect)
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp == null) return;

            Rect titleRect = new Rect(0f, 0f, inRect.width, 50f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Expedition33_GommageTrackerTitle".Translate());
            Text.Font = GameFont.Small;

            Rect contentRect = new Rect(0f, 60f, inRect.width, inRect.height - 60f);
            Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, 800f);
            Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

            float curY = 0f;

            if (!gameComp.paintressAlive)
            {
                Widgets.Label(new Rect(0f, 0f, inRect.width, 50f), "Expedition33_GommageCycleBroken".Translate());
                return; // Ne pas afficher le reste de l'UI
            }

            // Statut de la Paintress
            DrawPaintressStatus(ref curY, viewRect.width, gameComp);
            // Age actuellement peint
            DrawCurrentPaintedAge(ref curY, viewRect.width, gameComp);
            // Prochaine prediction
            DrawNextGommagePredict(ref curY, viewRect.width, gameComp);
            // Colons protégés
            DrawProtectedColonists(ref curY, viewRect.width);
            // Analyse des menaces
            DrawThreatAnalysis(ref curY, viewRect.width, gameComp);
            // Historique des Gommages
            DrawGommageHistory(ref curY, viewRect.width);

            Widgets.EndScrollView();
        }

        private void DrawPaintressStatus(ref float curY, float width, GameComponent_PaintressMonolith gameComp)
        {
            Rect rect = new Rect(0f, curY, width, 80f);
            Color bgColor = gameComp.paintressAlive ? new Color(0.4f, 0.1f, 0.1f, 0.8f) : new Color(0.1f, 0.4f, 0.1f, 0.8f);
            Widgets.DrawBoxSolid(rect, bgColor);

            Rect textRect = new Rect(10f, curY + 10f, width - 20f, 60f);
            string status = gameComp.paintressAlive ? "Expedition33_PaintressActive".Translate() : "Expedition33_PaintressNeutralized".Translate();
            Text.Font = GameFont.Medium;
            Widgets.Label(textRect, "Expedition33_PaintressStatus".Translate(status));
            Text.Font = GameFont.Small;
            curY += 90f;
        }

        private void DrawCurrentPaintedAge(ref float curY, float width, GameComponent_PaintressMonolith gameComp)
        {
            Rect rect = new Rect(0f, curY, width, 60f);
            Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.1f, 0.1f, 0.8f));

            string ageText = gameComp.currentPaintedAge != -1 ? 
                "Expedition33_CurrentCursedAge".Translate(gameComp.currentPaintedAge) : 
                "Expedition33_NoAgeCurrentlyPainted".Translate();

            Rect textRect = new Rect(10f, curY + 10f, width - 20f, 40f);
            Text.Font = GameFont.Medium;
            Widgets.Label(textRect, ageText);
            Text.Font = GameFont.Small;

            // Compte les colons >= à l'âge maudit
            if (gameComp.currentPaintedAge != -1)
            {
                int threatenedCount = Find.CurrentMap.mapPawns.FreeColonists
                    .Count(p => p.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge);
                if (threatenedCount > 0)
                {
                    Rect warningRect = new Rect(width - 200f, curY + 20f, 180f, 20f);
                    Widgets.Label(warningRect, "Expedition33_ColonistsThreatened".Translate(threatenedCount));
                }
            }
            curY += 70f;
        }

        private void DrawNextGommagePredict(ref float curY, float width, GameComponent_PaintressMonolith gameComp)
        {
            Rect rect = new Rect(0f, curY, width, 50f);
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.2f, 0.3f, 0.8f));
            Rect textRect = new Rect(10f, curY + 10f, width - 20f, 30f);

            if (gameComp.nextPaintingTick > 0)
            {
                int ticksRemaining = gameComp.nextPaintingTick - GenTicks.TicksGame;
                if (ticksRemaining > 0)
                {
                    int daysRemaining = ticksRemaining / 60000; // 60000 ticks = 1 jour
                    Widgets.Label(textRect, "Expedition33_NextGommageIn".Translate(daysRemaining));
                }
                else
                {
                    Widgets.Label(textRect, "Expedition33_GommageImminent".Translate());
                }
            }
            else
            {
                Widgets.Label(textRect, "Expedition33_NextPredictionUnknown".Translate());
            }
            curY += 60f;
        }

        private void DrawProtectedColonists(ref float curY, float width)
        {
            Rect rect = new Rect(0f, curY, width, 100f);
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.3f, 0.1f, 0.8f));

            Rect titleRect = new Rect(10f, curY + 5f, width - 20f, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Expedition33_ProtectedColonists".Translate());
            Text.Font = GameFont.Small;

            var protectedPawns = Find.CurrentMap.mapPawns.FreeColonists
                .Where(p => IsProtectedFromGommage(p))
                .ToList();

            Rect listRect = new Rect(10f, curY + 30f, width - 20f, 65f);
            if (protectedPawns.Any())
            {
                string protectedNames = string.Join(", ", protectedPawns.Select(p => p.Name.ToStringShort));
                Widgets.Label(listRect, "Expedition33_ProtectedCount".Translate(protectedPawns.Count, protectedNames));
            }
            else
            {
                Widgets.Label(listRect, "Expedition33_NoProtectedColonists".Translate());
            }
            curY += 110f;
        }

        private bool IsProtectedFromGommage(Pawn pawn)
        {
            // Protection par hediff du bouclier
            var shieldHediffProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
            );

            // Protection par hediff du champ
            var fieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_AntiGommageProtection")
            );

            // Vérification directe du bouclier équipé
            bool hasActiveShield = false;
            if (pawn.apparel?.WornApparel != null)
            {
                foreach (var apparel in pawn.apparel.WornApparel)
                {
                    if (apparel is Apparel_AntiGommageShield shield)
                    {
                        hasActiveShield = shield.isActive && shield.protectionCharges > 0;
                        if (hasActiveShield) break;
                    }
                }
            }

            // Vérification des générateurs de champ actifs
            bool inProtectionField = false;
            if (pawn.Map != null)
            {
                var generators = pawn.Map.listerBuildings.allBuildingsColonist
                    .OfType<Building_AntiGommageField>()
                    .Where(g => g.IsActive);

                foreach (var generator in generators)
                {
                    if (pawn.Position.DistanceTo(generator.Position) <= 15)
                    {
                        inProtectionField = true;
                        break;
                    }
                }
            }

            return shieldHediffProtection != null || fieldProtection != null || hasActiveShield || inProtectionField;
        }

        private void DrawThreatAnalysis(ref float curY, float width, GameComponent_PaintressMonolith gameComp)
        {
            Rect rect = new Rect(0f, curY, width, 140f);
            Widgets.DrawBoxSolid(rect, new Color(0.2f, 0.1f, 0.3f, 0.8f));

            Rect titleRect = new Rect(10f, curY + 5f, width - 20f, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Expedition33_ThreatAnalysis".Translate());
            Text.Font = GameFont.Small;

            if (gameComp.currentPaintedAge != -1)
            {
                var threatenedPawns = Find.CurrentMap.mapPawns.FreeColonists
                    .Where(p => p.ageTracker.AgeBiologicalYears >= gameComp.currentPaintedAge)
                    .ToList();

                float textY = curY + 30f;

                // Afficher les détails de protection pour chaque colon
                foreach (var pawn in threatenedPawns)
                {
                    bool isProtected = IsProtectedFromGommage(pawn);
                    string protectionDetails = GetProtectionDetails(pawn);
                    GUI.color = isProtected ? Color.green : Color.red;
                    string status = isProtected ? "✅" : "⚠️";
                    Widgets.Label(new Rect(15f, textY, width - 30f, 18f),
                        "Expedition33_ColonistThreatStatus".Translate(status, pawn.Name.ToStringShort, 
                            pawn.ageTracker.AgeBiologicalYears, protectionDetails));
                    GUI.color = Color.white;
                    textY += 18f;
                }

                if (!threatenedPawns.Any())
                {
                    GUI.color = Color.yellow;
                    Widgets.Label(new Rect(15f, textY, width - 30f, 20f),
                        "Expedition33_NoColonistsOfThisAge".Translate());
                    GUI.color = Color.white;
                }
            }
            curY += 150f;
        }

        private string GetProtectionDetails(Pawn pawn)
        {
            var details = new List<string>();

            // Vérifier bouclier équipé
            var shield = pawn.apparel?.WornApparel?
                .OfType<Apparel_AntiGommageShield>()
                .FirstOrDefault();

            if (shield?.isActive == true && shield.protectionCharges > 0)
            {
                details.Add("Expedition33_ShieldProtection".Translate(shield.protectionCharges));
            }

            // Vérifier hediff protection
            var hediffProt = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
            );
            if (hediffProt != null)
            {
                details.Add("Expedition33_ShieldEffect".Translate());
            }

            // Vérifier champ de protection
            var fieldProt = pawn.health.hediffSet.GetFirstHediffOfDef(
                DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_AntiGommageProtection")
            );
            if (fieldProt != null)
            {
                details.Add("Expedition33_ProtectiveField".Translate());
            }

            return details.Any() ? string.Join(", ", details) : "Expedition33_NotProtected".Translate();
        }

        private void DrawGommageHistory(ref float curY, float width)
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            
            // Calculer la hauteur dynamique selon le nombre d'entrées
            int entryCount = gameComp?.gommageHistory?.Count ?? 0;
            float dynamicHeight = 80f + (entryCount * 25f) + (entryCount > 0 ? 20f : 0f);

            Rect rect = new Rect(0f, curY, width, dynamicHeight);
            Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.1f, 0.8f));

            Rect titleRect = new Rect(10f, curY + 5f, width - 20f, 25f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "Expedition33_GommageHistory".Translate());
            Text.Font = GameFont.Small;

            float historyY = curY + 35f;

            if (gameComp?.gommageHistory != null && gameComp.gommageHistory.Any())
            {
                // En-têtes de colonnes
                Rect headerRect = new Rect(10f, historyY, width - 20f, 20f);
                GUI.color = Color.gray;
                Widgets.Label(new Rect(headerRect.x, headerRect.y, 60f, 20f), "Expedition33_Day".Translate());
                Widgets.Label(new Rect(headerRect.x + 70f, headerRect.y, 50f, 20f), "Expedition33_Age".Translate());
                Widgets.Label(new Rect(headerRect.x + 130f, headerRect.y, 70f, 20f), "Expedition33_Victims".Translate());
                Widgets.Label(new Rect(headerRect.x + 210f, headerRect.y, 70f, 20f), "Expedition33_Protected".Translate());
                Widgets.Label(new Rect(headerRect.x + 290f, headerRect.y, 80f, 20f), "Expedition33_Status".Translate());
                GUI.color = Color.white;
                historyY += 25f;

                // Ligne de séparation
                Widgets.DrawLineHorizontal(10f, historyY - 2f, width - 20f);

                // Afficher les entrées (les plus récentes en premier)
                var recentEntries = gameComp.gommageHistory
                    .OrderByDescending(e => e.tickOccurred)
                    .Take(8) // Limite à 8 entrées pour l'affichage
                    .ToList();

                foreach (var entry in recentEntries)
                {
                    Rect entryRect = new Rect(10f, historyY, width - 20f, 22f);

                    // Alternance de couleur de fond
                    if (recentEntries.IndexOf(entry) % 2 == 0)
                    {
                        Widgets.DrawBoxSolid(entryRect, new Color(0.2f, 0.2f, 0.15f, 0.3f));
                    }

                    // Jour
                    Widgets.Label(new Rect(entryRect.x, entryRect.y, 60f, 20f), "Expedition33_DayNumber".Translate(entry.dayOccurred));

                    // Âge
                    GUI.color = new Color(1f, 0.8f, 0.8f);
                    Widgets.Label(new Rect(entryRect.x + 70f, entryRect.y, 50f, 20f), "Expedition33_AgeYears".Translate(entry.age));
                    GUI.color = Color.white;

                    // Victimes
                    GUI.color = entry.totalVictims > 0 ? Color.red : Color.white;
                    Widgets.Label(new Rect(entryRect.x + 130f, entryRect.y, 70f, 20f), entry.totalVictims.ToString());
                    GUI.color = Color.white;

                    // Protégés
                    GUI.color = entry.protectedCount > 0 ? Color.green : Color.white;
                    Widgets.Label(new Rect(entryRect.x + 210f, entryRect.y, 70f, 20f), entry.protectedCount.ToString());
                    GUI.color = Color.white;

                    // Statut
                    GUI.color = entry.GetStatusColor();
                    Widgets.Label(new Rect(entryRect.x + 290f, entryRect.y, 80f, 20f), entry.GetStatusText());
                    GUI.color = Color.white;

                    historyY += 25f;
                }

                // Statistiques résumées
                if (gameComp.gommageHistory.Count > 0)
                {
                    historyY += 10f;
                    Widgets.DrawLineHorizontal(10f, historyY - 5f, width - 20f);

                    int totalGommages = gameComp.gommageHistory.Count;
                    int totalVictims = gameComp.gommageHistory.Sum(e => e.totalVictims);
                    int totalProtected = gameComp.gommageHistory.Sum(e => e.protectedCount);
                    int successfulBlocks = gameComp.gommageHistory.Count(e => e.totalVictims == 0);

                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.cyan;
                    string statsText = "Expedition33_GommageStats".Translate(totalGommages, totalVictims, totalProtected, successfulBlocks);
                    Widgets.Label(new Rect(15f, historyY, width - 30f, 18f), statsText);
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }
            else
            {
                // Aucun historique disponible
                Rect noHistoryRect = new Rect(10f, historyY, width - 20f, 25f);
                GUI.color = Color.gray;
                Widgets.Label(noHistoryRect, "Expedition33_NoGommageRecorded".Translate());
                GUI.color = Color.white;
            }

            curY += dynamicHeight + 10f;
        }
    }

    [StaticConstructorOnStartup]
    public class PaintressEventScheduler : GameComponent
    {
        public PaintressEventScheduler(Game game) : base() { }

        public override void GameComponentTick()
        {
            if (GenTicks.TicksGame % 60000 != 0) return; // Vérification quotidienne

            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp?.paintressAlive != true) return;

            // Événements aléatoires liés à la Paintress
            if (Rand.Chance(0.05f)) // 5% de chance par jour
            {
                TriggerRandomPaintressEvent();
            }
        }

        private void TriggerRandomPaintressEvent()
        {
            string[] eventKeys = {
                "Expedition33_PaintressEvent1",
                "Expedition33_PaintressEvent2",
                "Expedition33_PaintressEvent3",
                "Expedition33_PaintressEvent4"
            };

            string randomEventKey = eventKeys[Rand.Range(0, eventKeys.Length)];
            string randomEvent = randomEventKey.Translate();

            Messages.Message(
                "Expedition33_ExpeditionReport".Translate(randomEvent),
                MessageTypeDefOf.NeutralEvent
            );
        }
    }
}
