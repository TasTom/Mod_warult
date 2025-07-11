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
    public class MapComponent_MonolithSite : MapComponent
    {
        private bool paintressSpawned = false;
        private bool isMonolithSite = false;
        private bool paintressAlive = true;
        private bool preventRemoval = true;
        private bool hasRealColonists = false;
        private Pawn ghostColonist = null;
        private int persistenceTimer = 0;
        private int lastPersistenceCheck = 0;
        private int lastColonistTick = -1; // NOUVEAU - Dernier tick avec des colons

        public MapComponent_MonolithSite(Map map) : base(map)
        {
            CheckIfMonolithSite();
        }

        private void CheckIfMonolithSite()
        {
            try
            {
                if (map.info.parent != null && map.info.parent is Site)
                {
                    Site site = (Site)map.info.parent;

                    var gameComp = Current.Game?.GetComponent<GameComponent_PaintressMonolith>();
                    if (gameComp != null && gameComp.siteRevealed)
                    {
                        isMonolithSite = (site.Tile == gameComp.worldTileMonolith);
                    }
                }

                Log.Message(
                    $"MapComponent - IsMonolithSite: {isMonolithSite}, Parent: {map.info.parent?.GetType()?.Name}");
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans CheckIfMonolithSite : {e.Message}");
            }
        }

        public override void MapComponentTick()
        {
            if (!isMonolithSite) return;

            try
            {
                var realColonists = map.mapPawns.FreeColonists
                    .Where(p => p.Name?.ToStringShort != "Gardien Fant�me")
                    .ToList();

                hasRealColonists = realColonists.Any();

                // NOUVEAU - D�tecte l'abandon du site
                if (hasRealColonists)
                {
                    lastColonistTick = GenTicks.TicksGame;
                }
                else if (lastColonistTick > 0)
                {
                    int ticksSinceLastColonist = GenTicks.TicksGame - lastColonistTick;
                    int daysSinceAbandoned = ticksSinceLastColonist / 60000;

                    if (daysSinceAbandoned >= 3 && paintressAlive)
                    {
                        NotifyGameComponentOfAbandonment();
                        lastColonistTick = -1;
                    }
                }

                if (paintressAlive)
                {
                    ForcePersistence();
                }

                if (paintressAlive && !hasRealColonists)
                {
                    CreateGhostColonist();
                }
                else if (hasRealColonists && ghostColonist != null)
                {
                    RemoveGhostColonist();
                }

                if (!paintressSpawned && (hasRealColonists || ghostColonist != null))
                {
                    SpawnPaintressIfNeeded();
                }

                if (GenTicks.TicksGame % 60000 == 0)
                {
                    CheckPaintressStatus();
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans MapComponentTick : {e.Message}");
            }
        }

        // NOUVEAU - Notifie le GameComponent que le site a �t� abandonn�
        private void NotifyGameComponentOfAbandonment()
        {
            try
            {
                var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
                if (gameComp != null)
                {
                    Log.Message("Site du monolithe abandonn� - Notification au GameComponent");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans NotifyGameComponentOfAbandonment : {e.Message}");
            }
        }

        private void ForcePersistence()
        {
            try
            {
                Site monolithSite = Find.WorldObjects.Sites
                    .FirstOrDefault(s => s.Map == map);

                if (monolithSite != null)
                {
                    persistenceTimer++;

                    if (persistenceTimer % 60000 == 0)
                    {
                        string siteName = map.info.parent?.Label ?? "Site du monolithe";
                        Log.Message($"Site du monolithe maintenu actif - Timer: {persistenceTimer} - Site: {siteName}");

                        var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
                        if (gameComp != null)
                        {
                            gameComp.RegisterImportantMap(map);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans ForcePersistence : {e.Message}");
            }
        }

        private void CreateGhostColonist()
        {
            if (ghostColonist != null && !ghostColonist.Dead) return;

            try
            {
                IntVec3 hiddenPos;
                if (!CellFinder.TryFindRandomCellNear(map.Center, map, 50,
                        (IntVec3 c) => c.Standable(map) && c.Walkable(map), out hiddenPos))
                {
                    Log.Warning("Impossible de trouver une position pour le colon fant�me");
                    return;
                }

                PawnKindDef colonistKind = PawnKindDefOf.Colonist;
                ghostColonist = PawnGenerator.GeneratePawn(colonistKind, Faction.OfPlayer);
                ghostColonist.Name = new NameTriple("", "Gardien", "Fant�me");

                GenSpawn.Spawn(ghostColonist, hiddenPos, map);

                if (ghostColonist.jobs != null)
                    ghostColonist.jobs.StopAll();
                if (ghostColonist.pather != null)
                    ghostColonist.pather.StopDead();

                if (ghostColonist.needs?.mood != null)
                    ghostColonist.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.Catharsis);

                Log.Message($"Colon fant�me cr�� � {hiddenPos} pour maintenir la carte");
            }
            catch (Exception e)
            {
                Log.Error($"Erreur lors de la cr�ation du colon fant�me : {e.Message}");
            }
        }

        private void RemoveGhostColonist()
        {
            if (ghostColonist != null)
            {
                try
                {
                    ghostColonist.Destroy();
                    ghostColonist = null;
                    Log.Message("Vrais colons d�tect�s - Colon fant�me supprim�");
                }
                catch (Exception e)
                {
                    Log.Error($"Erreur lors de la suppression du colon fant�me : {e.Message}");
                }
            }
        }

        private void SpawnPaintressIfNeeded()
        {
            try
            {
                var existingPaintress = map.mapPawns.AllPawns
                    .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                if (existingPaintress != null)
                {
                    Log.Message("Paintress d�j� pr�sente sur la carte");
                    paintressSpawned = true;
                    return;
                }

                PawnKindDef paintressKind =
                    DefDatabase<PawnKindDef>.GetNamedSilentFail("Expedition33_PaintressMonster");
                if (paintressKind == null)
                {
                    Log.Error("PawnKindDef Expedition33_PaintressMonster non trouv� !");
                    return;
                }

                IntVec3 spawnPos = map.Center;
                if (!CellFinder.TryFindRandomCellNear(spawnPos, map, 15,
                        (IntVec3 c) => c.Standable(map) && c.Walkable(map), out spawnPos))
                {
                    Log.Warning("Position de spawn par d�faut utilis�e");
                    spawnPos = map.Center;
                }

                Faction expedition33 = Find.FactionManager.FirstFactionOfDef(
                    DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33")
                );

                Pawn paintress = PawnGenerator.GeneratePawn(paintressKind, expedition33);
                if (paintress == null)
                {
                    Log.Error("Impossible de g�n�rer la Paintress !");
                    return;
                }

                GenSpawn.Spawn(paintress, spawnPos, map);
                paintress.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter);

                paintressSpawned = true;

                Log.Message($"Paintress spawn�e automatiquement � {spawnPos}");

                Find.LetterStack.ReceiveLetter(
                    "LA PAINTRESS VOUS ATTEND !",
                    "En explorant cette r�gion myst�rieuse, vous d�couvrez la terrifiante Paintress !\n\n" +
                    "Cette entit� colossale manipule la r�alit� avec son pinceau magique. " +
                    "Comme dans Clair Obscur: Expedition 33, elle garde jalousement son monolithe.\n\n" +
                    "L'affrontement final commence maintenant !",
                    LetterDefOf.ThreatBig,
                    paintress
                );
            }
            catch (Exception e)
            {
                Log.Error($"Erreur lors du spawn de la Paintress : {e.Message}");
            }
        }

        private void CheckPaintressStatus()
        {
            try
            {
                var paintress = map.mapPawns.AllPawns
                    .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_PaintressMonster");

                if (paintress != null)
                {
                    paintressAlive = !paintress.Dead && !paintress.Destroyed;

                    if (!paintressAlive)
                    {
                        RemoveGhostColonist();
                        preventRemoval = false;
                        Log.Message("Paintress morte - Colon fant�me supprim�, carte peut �tre abandonn�e");
                    }
                }
                else if (paintressSpawned)
                {
                    paintressAlive = false;
                    RemoveGhostColonist();
                    preventRemoval = false;
                    Log.Message("Paintress disparue - Consid�r�e comme morte");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Erreur dans CheckPaintressStatus : {e.Message}");
            }
        }

        public override void MapGenerated()
        {
            if (isMonolithSite)
            {
                string siteName = map.info.parent?.Label ?? "Site du monolithe";
                Log.Message($"Carte du monolithe g�n�r�e - Persistance forc�e : {siteName}");

                var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
                if (gameComp != null)
                {
                    gameComp.RegisterImportantMap(map);
                }
            }
        }

        public override void MapRemoved()
        {
            if (isMonolithSite && paintressAlive && preventRemoval)
            {
                Log.Warning("Tentative de suppression de la carte du monolithe emp�ch�e - Paintress encore vivante");
                return;
            }

            Log.Message("Carte du monolithe supprim�e - Paintress morte ou removal autoris�");
            base.MapRemoved();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref paintressSpawned, "paintressSpawned", false);
            Scribe_Values.Look(ref isMonolithSite, "isMonolithSite", false);
            Scribe_Values.Look(ref paintressAlive, "paintressAlive", true);
            Scribe_Values.Look(ref preventRemoval, "preventRemoval", true);
            Scribe_Values.Look(ref hasRealColonists, "hasRealColonists", false);
            Scribe_Values.Look(ref persistenceTimer, "persistenceTimer", 0);
            Scribe_Values.Look(ref lastPersistenceCheck, "lastPersistenceCheck", 0);
            Scribe_Values.Look(ref lastColonistTick, "lastColonistTick", -1);
            Scribe_References.Look(ref ghostColonist, "ghostColonist");
        }
    }

    

}