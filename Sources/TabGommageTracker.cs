using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

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
			Widgets.Label(titleRect, "SURVEILLANCE DU GOMMAGE - EXPEDITION 33");
			Text.Font = GameFont.Small;

			Rect contentRect = new Rect(0f, 60f, inRect.width, inRect.height - 60f);
			Rect viewRect = new Rect(0f, 0f, contentRect.width - 16f, 800f);

			Widgets.BeginScrollView(contentRect, ref scrollPosition, viewRect);

			float curY = 0f;

			// Statut de la Paintress
			DrawPaintressStatus(ref curY, viewRect.width, gameComp);

			// Age actuellement peint
			DrawCurrentPaintedAge(ref curY, viewRect.width, gameComp);

			// Prochaine prediction
			DrawNextGommagePredict(ref curY, viewRect.width, gameComp);

			// Colons proteges
			DrawProtectedColonists(ref curY, viewRect.width);

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
			string status = gameComp.paintressAlive ? "ACTIVE - DANGER IMMINENT" : "NEUTRALISEE";

			Text.Font = GameFont.Medium;
			Widgets.Label(textRect, $"Statut de la Paintress: {status}");
			Text.Font = GameFont.Small;

			curY += 90f;
		}

		private void DrawCurrentPaintedAge(ref float curY, float width, GameComponent_PaintressMonolith gameComp)
		{
			Rect rect = new Rect(0f, curY, width, 60f);
			Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.1f, 0.1f, 0.8f));

			string ageText = gameComp.currentPaintedAge != -1 ? $"{gameComp.currentPaintedAge} ans" : "Aucun age peint";

			Rect textRect = new Rect(10f, curY + 10f, width - 20f, 40f);
			Text.Font = GameFont.Medium;
			Widgets.Label(textRect, $"Age Maudit Actuel: {ageText}");
			Text.Font = GameFont.Small;

			// Compte les colons menaces
			if (gameComp.currentPaintedAge != -1)
			{
				int threatenedCount = Find.CurrentMap.mapPawns.FreeColonists
					.Count(p => p.ageTracker.AgeBiologicalYears == gameComp.currentPaintedAge);

				if (threatenedCount > 0)
				{
					Rect warningRect = new Rect(width - 200f, curY + 20f, 180f, 20f);
					Widgets.Label(warningRect, $"WARNING: {threatenedCount} colon(s) menace(s)");
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
					Widgets.Label(textRect, $"Prochain Gommage estime dans: {daysRemaining} jour(s)");
				}
				else
				{
					Widgets.Label(textRect, "GOMMAGE IMMINENT !");
				}
			}
			else
			{
				Widgets.Label(textRect, "Prochaine prediction: Inconnue");
			}

			curY += 60f;
		}

		private void DrawProtectedColonists(ref float curY, float width)
		{
			Rect rect = new Rect(0f, curY, width, 100f);
			Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.3f, 0.1f, 0.8f));

			Rect titleRect = new Rect(10f, curY + 5f, width - 20f, 25f);
			Text.Font = GameFont.Medium;
			Widgets.Label(titleRect, "Colons Proteges:");
			Text.Font = GameFont.Small;

			var protectedPawns = Find.CurrentMap.mapPawns.FreeColonists
				.Where(p => IsProtectedFromGommage(p))
				.ToList();

			Rect listRect = new Rect(10f, curY + 30f, width - 20f, 65f);
			if (protectedPawns.Any())
			{
				string protectedNames = string.Join(", ", protectedPawns.Select(p => p.Name.ToStringShort));
				Widgets.Label(listRect, $"Proteges ({protectedPawns.Count}): {protectedNames}");
			}
			else
			{
				Widgets.Label(listRect, "Aucun colon protege !");
			}

			curY += 110f;
		}

		private void DrawGommageHistory(ref float curY, float width)
		{
			Rect rect = new Rect(0f, curY, width, 80f);
			Widgets.DrawBoxSolid(rect, new Color(0.3f, 0.3f, 0.1f, 0.8f));

			Rect titleRect = new Rect(10f, curY + 5f, width - 20f, 25f);
			Text.Font = GameFont.Medium;
			Widgets.Label(titleRect, "Historique des Gommages:");
			Text.Font = GameFont.Small;

			Rect historyRect = new Rect(10f, curY + 30f, width - 20f, 45f);
			Widgets.Label(historyRect, "Derniers ages gommes: [Historique a implementer]");

			curY += 90f;
		}

		private bool IsProtectedFromGommage(Pawn pawn)
		{
			var shieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
				DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_GommageProtection")
			);

			var fieldProtection = pawn.health.hediffSet.GetFirstHediffOfDef(
				DefDatabase<HediffDef>.GetNamedSilentFail("Expedition33_FieldProtection")
			);

			return shieldProtection != null || fieldProtection != null;
		}
	}
}
