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
		//public IntVec3 monolithPosition = IntVec3.Invalid;

		// NOUVEAU - Variables pour la r�apparition
		private int lastSiteCheckTick = -1;
		private bool needsSiteRecreation = false;
		private int siteRecreationAttempts = 0;
		private const int MAX_RECREATION_ATTEMPTS = 3;

		private const int TICKS_PER_YEAR = 3600000;
		private const int WARNING_DAYS_BEFORE = 1;

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
				Log.Message($"Carte importante enregistr�e : {map.info.parent?.Label ?? "Site du monolithe"}");
			}
		}

		public override void StartedNewGame()
		{
			base.StartedNewGame();
			if (!initialized)
			{
				InitializeMonolith();
				Log.Message("=== NOUVELLE PARTIE - MONOLITHE INITIALIS� ===");
			}
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();
			if (!initialized && currentPaintedAge == -1)
			{
				InitializeMonolith();
				Log.Message("=== PARTIE CHARG�E - MONOLITHE INITIALIS� ===");
			}
		}

		private void InitializeMonolith()
		{
			if (initialized) return;

			currentPaintedAge = Rand.Range(50, 61);
			nextPaintingTick = Find.TickManager.TicksGame + TICKS_PER_YEAR;
			initialWarningTick = Find.TickManager.TicksGame + 60000; // 1 jour complet

			worldTileMonolith = FindValidLandTile();
			initialized = true;

			Log.Message($"Monolithe initialis� - �ge peint: {currentPaintedAge}");
			Log.Message($"Tuile terrestre s�lectionn�e: {worldTileMonolith}");
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
				Log.Message($"Tuile valide trouv�e parmi {validTiles.Count} options : {chosenTile}");
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

			Log.Error("Aucune tuile terrestre trouv�e ! Utilise tuile 0");
			return 0;
		}

		private bool IsValidMonolithTile(Tile tile, int tileIndex)
		{
			if (tile.WaterCovered) return false;
			if (tile.temperature < -20f) return false;
			if (tile.hilliness == Hilliness.Impassable) return false;
			if (Find.WorldObjects.AnyWorldObjectAt(tileIndex)) return false;

			if (tile.biome != null)
			{
				string biomeName = tile.biome.defName;
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

			int currentTick = Find.TickManager.TicksGame;

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
					Log.Message("Monde pas encore pr�t, retard de la r�v�lation du site");
				}
			}

			// NOUVEAU - V�rifie l'existence du site toutes les heures
			if (currentTick % 2500 == 0) // Toutes les heures (2500 ticks)
			{
				CheckMonolithSiteExists();
			}

			// NOUVEAU - Recr�e le site si n�cessaire
			if (needsSiteRecreation && currentTick > lastSiteCheckTick + 60000) // 1 jour apr�s disparition
			{
				RecreateMonolithSite();
				needsSiteRecreation = false;
			}

			// Persistance forc�e
			if (Find.TickManager.TicksGame % 60000 == 0)
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
				Log.Message($"Monolithe Status - Age: {currentPaintedAge}, Site r�v�l�: {siteRevealed}");
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

		// NOUVEAU - V�rifie si le site du monolithe existe encore
		private void CheckMonolithSiteExists()
		{
			if (!siteRevealed || worldTileMonolith == -1) return;

			try
			{
				Site monolithSite = Find.WorldObjects.Sites
					.FirstOrDefault(s => s.Tile == worldTileMonolith);

				if (monolithSite == null)
				{
					Log.Warning("Site du monolithe disparu - Programmation de la recr�ation");
					needsSiteRecreation = true;
					lastSiteCheckTick = Find.TickManager.TicksGame;
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
							Log.Message("Paintress morte d�tect�e - Site sera recr�� avec nouvelle Paintress");
							needsSiteRecreation = true;
							lastSiteCheckTick = Find.TickManager.TicksGame;
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

		// NOUVEAU - Recr�e le site du monolithe avec message dramatique
		private void RecreateMonolithSite()
		{
			if (siteRecreationAttempts >= MAX_RECREATION_ATTEMPTS)
			{
				Log.Warning($"Nombre maximum de tentatives de recr�ation atteint ({MAX_RECREATION_ATTEMPTS})");
				return;
			}

			try
			{
				siteRecreationAttempts++;
				Log.Message($"Recr�ation du site du monolithe en cours... (Tentative {siteRecreationAttempts}/{MAX_RECREATION_ATTEMPTS})");

				int newTile = FindValidLandTile();
				if (newTile != worldTileMonolith)
				{
					worldTileMonolith = newTile;
					Log.Message($"Nouvelle tuile s�lectionn�e pour le monolithe : {worldTileMonolith}");
				}

				CreateVisibleMonolithSite();

				string letterText = $"ALERTE EXP�DITION 33 !\n\n" +
								   $"Nos �claireurs rapportent une d�couverte troublante :\n\n" +
								   $"\"Le Monolithe de la Paintress est r�apparu ! Elle a surv�cu et a reconstruit son domaine mystique.\"\n\n" +
								   $"L'�ge maudit reste le m�me : {currentPaintedAge} ans\n\n" +
								   $"La Paintress semble plus d�termin�e que jamais. Son pouvoir artistique " +
								   $"lui permet de recr�er son sanctuaire m�me apr�s sa destruction.\n\n" +
								   $"Pr�parez une nouvelle exp�dition ! Cette fois, assurez-vous de l'�liminer d�finitivement !";

				Find.LetterStack.ReceiveLetter(
					"LE MONOLITHE A R�APPARU !",
					letterText,
					LetterDefOf.ThreatBig,
					new GlobalTargetInfo(worldTileMonolith)
				);

				Log.Message("Site du monolithe recr�� avec succ�s");
			}
			catch (Exception e)
			{
				Log.Error($"Erreur lors de la recr�ation du site : {e.Message}");
				needsSiteRecreation = true;
				lastSiteCheckTick = Find.TickManager.TicksGame;
			}
		}

		// NOUVEAU - Force la recr�ation (pour debug)
		public void ForceRecreateMonolithSite()
		{
			needsSiteRecreation = true;
			lastSiteCheckTick = Find.TickManager.TicksGame - 60000;
			siteRevealed = false;
			siteRecreationAttempts = 0;
			Log.Message("Recr�ation forc�e du site du monolithe programm�e");
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
							Log.Message("Site du monolithe forc� en persistance via hack");
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
			string letterText = $"Des �claireurs de l'Exp�dition 33 rapportent une d�couverte terrifiante :\n\n" +
							   $"\"Nous avons localis� le monolithe mystique ! La Paintress y a d�j� peint un num�ro : {currentPaintedAge}\"\n\n" +
							   $"Tous vos colons de {currentPaintedAge} ans sont d�sormais en danger mortel. " +
							   $"Le Gommage peut survenir � tout moment.\n\n" +
							   $"L'emplacement du monolithe vient d'�tre r�v�l� sur la carte du monde. " +
							   $"Formez une caravane et rendez-vous sur les lieux pour affronter la Paintress !\n\n" +
							   $"C'est votre seule chance de briser le cycle du Gommage !";

			Find.LetterStack.ReceiveLetter(
				"MONOLITHE D�COUVERT !",
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
					Log.Error("WorldGrid ou WorldObjects non initialis�");
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
					Log.Error("SiteMaker.MakeSite a retourn� null");
					CreateFallbackMessage();
					return;
				}

				monolithSite.customLabel = "Monolithe de la Paintress";
				Find.WorldObjects.Add(monolithSite);
				siteRevealed = true;

				Log.Message($"Site standard cr�� avec succ�s � la tuile {worldTileMonolith}");
			}
			catch (Exception e)
			{
				Log.Error($"Erreur dans la cr�ation de site standard : {e.Message}");
				CreateFallbackMessage();
			}
		}

		private void CreateFallbackMessage()
		{
			try
			{
				siteRevealed = true;

				string letterText = $"Des �claireurs de l'Exp�dition 33 rapportent :\n\n" +
								   $"\"Nous avons localis� le Monolithe de la Paintress ! Elle y a peint le num�ro : {currentPaintedAge}\"\n\n" +
								   $"Tous vos colons de {currentPaintedAge} ans sont en danger mortel. " +
								   $"Le Gommage peut survenir � tout moment.\n\n" +
								   $"Malheureusement, les coordonn�es exactes sont illisibles. " +
								   $"Restez vigilants et pr�parez vos d�fenses !";

				Find.LetterStack.ReceiveLetter(
					"MONOLITHE LOCALIS� !",
					letterText,
					LetterDefOf.ThreatBig
				);

				Log.Message("Message de fallback envoy� - pas de site physique cr��");
			}
			catch (Exception fallbackEx)
			{
				Log.Error($"Erreur m�me dans le fallback : {fallbackEx.Message}");
			}
		}

		private void SendPaintingWarning()
		{
			if (!initialized) return;

			string letterText = $"Des �claireurs de l'Exp�dition 33 rapportent une activit� inqui�tante :\n\n" +
							   $"\"La Paintress s'approche de son monolithe mystique. Dans exactement 1 jour, " +
							   $"elle peindra un nouveau num�ro qui d�terminera l'�ge des prochaines victimes du Gommage.\"\n\n" +
							   $"�ge actuellement peint : {currentPaintedAge} ans\n\n" +
							   $"Pr�parez-vous... Le destin de vos colons se joue demain.";

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
			nextPaintingTick = Find.TickManager.TicksGame + TICKS_PER_YEAR;

			string letterText = $"La Paintress a peint un nouveau num�ro sur son monolithe !\n\n" +
							   $"�GE PEINT : {currentPaintedAge} ANS\n\n" +
							   $"Tous vos colons de cet �ge exact sont maintenant en danger mortel. " +
							   $"Le Gommage peut survenir � tout moment.\n\n" +
							   $"Seule l'�limination de la Paintress peut briser ce cycle maudit !";

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
				Find.Storyteller.incidentQueue.Add(gommageIncident, Find.TickManager.TicksGame + Rand.Range(60000, 300000), parms);
			}
		}

		public void OnPaintressKilled()
		{
			paintressAlive = false;
			currentPaintedAge = -1;

			string letterText = "La Paintress a �t� �limin�e !\n\n" +
							   "Son monolithe mystique s'effrite en poussi�re. Le cycle du Gommage est bris�. " +
							   "Vos colons sont enfin en s�curit�, lib�r�s de cette terreur artistique.\n\n" +
							   "L'Exp�dition 33 peut enfin conna�tre la paix.";

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