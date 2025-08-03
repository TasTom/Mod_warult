using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Mod_warult
{
    public class BossSiteExtension : DefModExtension
    {
        public string sitePartDefName; // ex : Expedition33_GobluSitePart
        public string bossDefName; // ex : Expedition33_Goblu
        public string questId; // ex : ActeI_VallonsFleuris
        public float threatLevel = 1f; // réserve pour Storyteller
        /* NEW */
        public List<string> forcedApparelDefs = new List<string>(); // ex : {"Expedition33_ArmureSacree","Expedition33_CasqueSacree"}
    }

    public class IncidentWorker_SpawnBossSite : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) => true;

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var ext = def.GetModExtension<BossSiteExtension>();
            if (ext == null)
            {
                Log.Error("Expedition33_BossSiteExtensionMissing".Translate(def.defName));
                return false;
            }

            // ✅ VÉRIFICATION AMÉLIORÉE : Ne crée pas si le site existe déjà
            if (BossSiteManager.SiteExistsForQuest(ext.questId))
            {
                Log.Message("Expedition33_SiteAlreadyExists".Translate(ext.questId));
                return false; // Site déjà présent, pas besoin d'en créer un nouveau
            }

            // Sélection d'une tuile
            if (!TileFinder.TryFindNewSiteTile(out PlanetTile planetTile, 8, 15, false, null))
            {
                Log.Warning("Expedition33_NoAvailableTile".Translate());
                return false;
            }

            int tile = planetTile.tileId;

            // Récupération du SitePartDef
            SitePartDef partDef = DefDatabase<SitePartDef>.GetNamedSilentFail(ext.sitePartDefName);
            if (partDef == null)
            {
                Log.Error("Expedition33_SitePartDefNotFound".Translate(ext.sitePartDefName));
                return false;
            }

            // Création et enregistrement du site
            Site site = SiteMaker.MakeSite(partDef, tile, parms.faction);
            site.SetFaction(parms.faction);

            // ✅ MARQUAGE DU SITE AVEC L'ID DE QUÊTE
            BossSiteManager.RegisterSiteForQuest(site, ext.questId);

            Find.WorldObjects.Add(site);

            // Lettre d'information
            Find.LetterStack.ReceiveLetter(
                GetLetterTitle(ext.questId),
                GetLetterText(ext.questId, ext.bossDefName),
                LetterDefOf.ThreatBig,
                site);

            Log.Message("Expedition33_BossSiteCreated".Translate(ext.sitePartDefName, tile));
            return true;
        }

        private static string GetLetterTitle(string questId) => questId switch
        {
            "ActeI_VallonsFleuris" => "Expedition33_FloweringValleys".Translate(),
            "ActeI_OceanSuspendu" => "Expedition33_SuspendedOcean".Translate(),
            "ActeI_SanctuaireAncien" => "Expedition33_AncientSanctuary".Translate(),
            "ActeI_NidEsquie" => "Expedition33_EsquieNest".Translate(),
            "ActeI_Final" => "Expedition33_LightSanctuary".Translate(),
            "ActeII_TerresOubliees" => "Expedition33_ForgottenLands".Translate(),
            "ActeII_Manoir" => "Expedition33_RenoirManor".Translate(),
            "ActeII_SireneQuest" => "Expedition33_SireneQuest".Translate(),
            "ActeII_VisagesQuest" => "Expedition33_VisagesQuest".Translate(),
            "ActeII_LesAxons" => "Expedition33_AxonGuardians".Translate(),
            "ActeII_Final" => "Expedition33_MonolithRevealed".Translate(),
            _ => "Expedition33_ExpeditionSiteDiscovered".Translate()
        };

        private static string GetLetterText(string questId, string boss) => questId switch
        {
            "ActeI_VallonsFleuris" => "Expedition33_FloweringValleysDesc".Translate(boss),
            "ActeI_OceanSuspendu" => "Expedition33_SuspendedOceanDesc".Translate(boss),
            "ActeI_SanctuaireAncien" => "Expedition33_AncientSanctuaryDesc".Translate(boss),
            "ActeI_NidEsquie" => "Expedition33_EsquieNestDesc".Translate(),
            "ActeI_Final" => "Expedition33_LightSanctuaryDesc".Translate(),
            "ActeII_TerresOubliees" => "Expedition33_ForgottenLandsDesc".Translate(),
            "ActeII_Manoir" => "Expedition33_RenoirManorDesc".Translate(),
            "ActeII_SireneQuest" => "Expedition33_SireneQuestDesc".Translate(),
            "ActeII_VisagesQuest" => "Expedition33_VisagesQuestDesc".Translate(),
            "ActeII_LesAxons" => "Expedition33_AxonGuardiansDesc".Translate(),
            "ActeII_Final" => "Expedition33_MonolithRevealedDesc".Translate(),
            _ => "Expedition33_CriticalObjectiveLocated".Translate(boss)
        };
    }

    public class IncidentWorker_SpawnAxonSites : IncidentWorker
{
    protected override bool CanFireNowSub(IncidentParms parms) => true;

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        // Empêcher un 2ᵉ spawn
        if (Find.WorldObjects.AllWorldObjects.OfType<MapParent>().Any(p => p.def.defName == "Expedition33_AxonSite"))
            return false;

        for (int i = 0; i < 2; i++)
        {
            var tile = TileFinder.RandomStartingTile();     // ou logique plus fine
            var site = (Site)WorldObjectMaker.MakeWorldObject(
                DefDatabase<WorldObjectDef>.GetNamed("Expedition33_AxonSite"));
            site.Tile = tile;
            site.SetFaction(Faction.OfAncientsHostile);
            Find.WorldObjects.Add(site);
        }
        Messages.Message("Deux sites Axons viennent d’apparaître !", MessageTypeDefOf.NeutralEvent);
        return true;
    }
}


    public class SitePartWorker_BossSpawn : SitePartWorker
    {
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            var site = map.Parent as Site;
            BossSiteExtension ext = site?
                .parts
                .Select(p => p.def.GetModExtension<BossSiteExtension>())
                .FirstOrDefault(e => e != null);

            if (ext != null)
                SpawnBoss(map, ext.bossDefName);
            else
                Log.Warning("Expedition33_NoBossSiteExtension".Translate());
        }

        private static void SpawnBoss(Map map, string bossDefName)
        {
            if (string.IsNullOrEmpty(bossDefName))
            {
                Log.Error("Expedition33_EmptyBossDefName".Translate());
                return;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(bossDefName);
            if (kind == null)
            {
                Log.Error("Expedition33_PawnKindDefNotFound".Translate(bossDefName));
                return;
            }

            Faction bossFaction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Nevrons"))
                ?? Faction.OfAncientsHostile; // fallback sûr

            Pawn boss = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, bossFaction, PawnGenerationContext.NonPlayer, map.Tile));

            if (boss == null)
            {
                Log.Error("Expedition33_BossGenerationFailed".Translate(bossDefName));
                return; // stoppe proprement
            }

            // ✅ NOUVEAU BLOC : HABILLAGE OBLIGATOIRE DU BOSS
            var site = map.Parent as Site;
            var bossExt = site?.parts
                .Select(p => p.def.GetModExtension<BossSiteExtension>())
                .FirstOrDefault(e => e?.bossDefName == bossDefName);

            if (bossExt?.forcedApparelDefs?.Count > 0)
            {
                // Retire tout ce que le boss porte déjà (s'il en a)
                if (boss.apparel?.WornApparel != null)
                {
                    var apparelToRemove = boss.apparel.WornApparel.ToList();
                    foreach (var apparel in apparelToRemove)
                    {
                        boss.apparel.Remove(apparel);
                        apparel.Destroy();
                    }
                }

                // Force l'équipement défini dans l'extension
                foreach (string defName in bossExt.forcedApparelDefs)
                {
                    ThingDef appDef = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                    if (appDef == null)
                    {
                        Log.Warning("Expedition33_ApparelNotFound".Translate(defName));
                        continue;
                    }

                    try
                    {
                        Apparel apparel = (Apparel)ThingMaker.MakeThing(appDef);
                        if (apparel != null)
                        {
                            boss.apparel.Wear(apparel, dropReplacedApparel: false);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"Expedition33_ApparelEquipError".Translate(defName, ex.Message));
                    }
                }
            }
            // ✅ FIN DU BLOC HABILLAGE

            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(
                map.Center, map, out IntVec3 spawnPos, 12))
            {
                spawnPos = map.Center; // position de secours
            }

            ConfigureBossHostileBehavior(boss, map);
            GenSpawn.Spawn(boss, spawnPos, map);

            if (boss.playerSettings != null)
                boss.playerSettings.hostilityResponse = HostilityResponseMode.Attack;

            var lord = LordMaker.MakeNewLord(
                boss.Faction,
                new LordJob_DefendBossSite(spawnPos, 80f),
                map,
                new List<Pawn> { boss });

            // Empêche le boss de fuir individuellement
            boss.mindState.exitMapAfterTick = -1;
            boss.mindState.forcedGotoPosition = IntVec3.Invalid;

            Messages.Message("Expedition33_BossAwaitsBattle".Translate(boss.LabelShort.CapitalizeFirst()),
                new TargetInfo(spawnPos, map), MessageTypeDefOf.ThreatBig);
        }



        private static void ConfigureBossHostileBehavior(Pawn boss, Map map)
        {
            if (boss.mindState != null)
            {
                boss.mindState.anyCloseHostilesRecently = true;
                boss.mindState.canFleeIndividual = false;
                boss.mindState.exitMapAfterTick = -1;
            }

            // ✅ CORRECT : Ne plus créer de job au spawn
            var playerPawns = map.mapPawns.FreeColonists
                .Where(p => !p.Downed && !p.Dead)
                .OrderBy(p => boss.Position.DistanceTo(p.Position))
                .ToList();

            if (playerPawns.Any())
            {
                boss.mindState.enemyTarget = playerPawns.First();
                Log.Message($"[DEBUG] {boss.LabelShort} configuré avec cible {playerPawns.First().LabelShort}");
                // ✅ SUPPRIMÉ : Ne plus créer de job ici
            }
        }





    }

    public static class BossSiteManager
    {
        // ✅ NOUVEAU SYSTÈME DE SUIVI DES SITES
        private static readonly Dictionary<string, int> registeredSites = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> activeSitesTick = new Dictionary<string, int>();

        public static void RegisterSiteForQuest(Site site, string questId)
        {
            if (site?.ID != null && !string.IsNullOrEmpty(questId))
            {
                registeredSites[questId] = site.ID;
                Log.Message("Expedition33_SiteRegistered".Translate(questId, site.ID));
            }
        }

        public static bool SiteExistsForQuest(string questId)
        {
            // Méthode 1 : Vérifier dans notre registre
            if (registeredSites.TryGetValue(questId, out int siteId))
            {
                var site = Find.WorldObjects.AllWorldObjects.OfType<Site>()
                    .FirstOrDefault(s => s.ID == siteId);
                if (site != null)
                {
                    return true; // Site trouvé dans le registre et existe encore
                }
                else
                {
                    registeredSites.Remove(questId); // Nettoie le registre
                }
            }

            // Méthode 2 : Recherche par SitePartDef (plus fiable)
            string expectedSitePartDef = GetSitePartDefForQuest(questId);
            if (!string.IsNullOrEmpty(expectedSitePartDef))
            {
                var matchingSites = Find.WorldObjects.AllWorldObjects.OfType<Site>()
                    .Where(s => s.parts.Any(p => p.def.defName == expectedSitePartDef))
                    .ToList();

                if (matchingSites.Any())
                {
                    // Re-enregistre le site trouvé
                    RegisterSiteForQuest(matchingSites.First(), questId);
                    return true;
                }
            }

            return false;
        }

        private static string GetSitePartDefForQuest(string questId) => questId switch
        {
            "ActeI_VallonsFleuris" => "Expedition33_EvequeSitePart",
            "ActeI_OceanSuspendu" => "Expedition33_GobluSitePart",
            "ActeI_SanctuaireAncien" => "Expedition33_SakapatateUltimeSitePart",
            "ActeI_NidEsquie" => "Expedition33_FrancoisSitePart",
            "ActeI_Final" => "Expedition33_MaitreDesLampesSitePart",
            "ActeII_TerresOubliees" => "Expedition33_DualisteSitePart",
            "ActeII_Manoir" => "Expedition33_RenoirSitePart",
            "ActeII_SireneQuest" => "Expedition33_SireneSitePart", 
            "ActeII_VisagesQuest" => "Expedition33_VisagesSitePart", 
            "ActeII_Final" => "Expedition33_PaintressSitePart",
            _ => null
        };

        public static void EnsureBossSite(string questId)
        {
            // ✅ VÉRIFICATION AMÉLIORÉE
            if (SiteExistsForQuest(questId))
            {
                // Ne log que si en mode debug
                if (Prefs.DevMode)
                {
                    Log.Message("Expedition33_SiteAlreadyPresent".Translate(questId));
                }
                return; // Site déjà présent, on ne fait rien
            }

            // Éviter les créations trop rapprochées
            if (activeSitesTick.ContainsKey(questId) &&
                GenTicks.TicksGame - activeSitesTick[questId] < 6000) // 0.1 jour
            {
                return;
            }

            string incidentDefName = GetIncidentForQuest(questId);
            IncidentDef inc = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
            if (inc == null) return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
            parms.faction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamed("Nevrons"));

            if (inc.Worker.TryExecute(parms))
            {
                activeSitesTick[questId] = GenTicks.TicksGame;
                Log.Message("Expedition33_SiteCreatedForQuest".Translate(questId));
            }
        }

        private static string GetIncidentForQuest(string questId) => questId switch
        {
            "ActeI_VallonsFleuris" => "Expedition33_SpawnEvequeSite",
            "ActeI_OceanSuspendu" => "Expedition33_SpawnGobluSite",
            "ActeI_SanctuaireAncien" => "Expedition33_SpawnSakapatateUltimeSite",
            "ActeI_NidEsquie" => "Expedition33_SpawnFrancoisSite",
            "ActeI_Final" => "Expedition33_SpawnMaitreDesLampesSite",
            "ActeII_TerresOubliees" => "Expedition33_SpawnDualisteSite",
            "ActeII_Manoir" => "Expedition33_SpawnRenoirSite",
            "ActeII_SireneQuest" => "Expedition33_SpawnSireneSite",
            "ActeII_VisagesQuest" => "Expedition33_SpawnVisagesSite",
            "ActeII_LesAxons" => "Expedition33_SpawnSireneSite",
            "ActeII_Final" => "Expedition33_SpawnPaintressSite",
            _ => null
        };

        // ✅ MÉTHODE DE NETTOYAGE
        public static void CleanupDestroyedSites()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in registeredSites)
            {
                var site = Find.WorldObjects.AllWorldObjects.OfType<Site>()
                    .FirstOrDefault(s => s.ID == kvp.Value);
                if (site == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                registeredSites.Remove(key);
            }
        }
    }

    public class GameComponent_BossSiteRespawner : GameComponent
    {
        private int lastCheckTick = 0;
        private static readonly int CHECK_INTERVAL = 18000; // toutes les 0,3 journée

        // Quêtes pour lesquelles un site doit toujours persister
        private static readonly List<string> persistentQuests = new List<string>
        {
            "ActeI_VallonsFleuris",
            "ActeI_OceanSuspendu",
            "ActeI_SanctuaireAncien",
            "ActeI_NidEsquie",
            "ActeI_Final",
            "ActeII_TerresOubliees",
            "ActeII_Manoir",
            "ActeII_SireneQuest",
            "ActeII_VisagesQuest",
            "ActeII_LesAxons",
            "ActeII_Final"
        };

        public GameComponent_BossSiteRespawner(Game game) : base() { }

        public override void GameComponentTick()
        {
            // Toutes les X ticks pour ne pas surcharger
            if (GenTicks.TicksGame < lastCheckTick + CHECK_INTERVAL) return;
            lastCheckTick = GenTicks.TicksGame;

            // ✅ NETTOYAGE PÉRIODIQUE
            BossSiteManager.CleanupDestroyedSites();

            // ✅ VÉRIFICATION PLUS STRICTE
            foreach (string questId in persistentQuests)
            {
                if (QuestManager.CurrentQuestId == questId)
                {
                    BossSiteManager.EnsureBossSite(questId);
                }
            }
        }
    }

    public class CompProperties_BossLootDrop : CompProperties
    {
        public ThingDef lootDef;
        public int lootCount = 1;

        public CompProperties_BossLootDrop()
        {
            this.compClass = typeof(CompBossLootDrop);
        }
    }

    public class CompBossLootDrop : ThingComp
    {
        public CompProperties_BossLootDrop Props => (CompProperties_BossLootDrop)this.props;

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);

            // Lors de la mort du boss (ou destruction), drop le loot à son emplacement
            if (mode == DestroyMode.KillFinalize && parent.Position.IsValid &&
                previousMap != null && Props.lootDef != null)
            {
                // Drop l'objet à l'emplacement exact de la mort
                Thing loot = ThingMaker.MakeThing(Props.lootDef);
                loot.stackCount = Props.lootCount;
                GenPlace.TryPlaceThing(loot, parent.Position, previousMap, ThingPlaceMode.Near);
            }
        }
    }



}
