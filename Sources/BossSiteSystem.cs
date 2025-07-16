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
    /*───────────────────────────────────────────────────────────*\
    |  1.  Extension XML portée par chaque IncidentDef           |
    \*───────────────────────────────────────────────────────────*/
    public class BossSiteExtension : DefModExtension
    {
        public string sitePartDefName;   // ex : Expedition33_GobluSitePart
        public string bossDefName;       // ex : Expedition33_Goblu
        public string questId;           // ex : ActeI_VallonsFleuris
        public float  threatLevel = 1f;  // réserve pour Storyteller
    }

    /*───────────────────────────────────────────────────────────*\
    |  2.  Worker unique qui crée réellement le point d’intérêt |
    \*───────────────────────────────────────────────────────────*/
    public class IncidentWorker_SpawnBossSite : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) => true;

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var ext = def.GetModExtension<BossSiteExtension>();
            if (ext == null)
            {
                Log.Error($"[Expedition33] BossSiteExtension manquante sur {def.defName}");
                return false;
            }

            /*--- 2-A  Sélection d’une tuile ­--------------------------------*/
            PlanetTile planetTile;
            if (!TileFinder.TryFindNewSiteTile(out planetTile, 8, 15, false, null))
            {
                Log.Warning("[Expedition33] Aucune tuile disponible pour le site boss");
                return false;
            }
            int tile = planetTile.tileId;

            /*--- 2-B  Récupération du SitePartDef ­---------------------------*/
            SitePartDef partDef = DefDatabase<SitePartDef>.GetNamedSilentFail(ext.sitePartDefName);
            if (partDef == null)
            {
                Log.Error($"[Expedition33] SitePartDef introuvable : {ext.sitePartDefName}");
                return false;
            }

            /*--- 2-C  Création et enregistrement du site ­-------------------*/
            Site site = SiteMaker.MakeSite(partDef, tile, parms.faction);
            site.SetFaction(parms.faction);
            Find.WorldObjects.Add(site);

            /*--- 2-D  Lettre d’information ­---------------------------------*/
            Find.LetterStack.ReceiveLetter(
                GetLetterTitle(ext.questId),
                GetLetterText(ext.questId, ext.bossDefName),
                LetterDefOf.ThreatBig,
                site);

            Log.Message($"[Expedition33] Site boss « {ext.sitePartDefName} » créé (tuile {tile})");
            return true;
        }

        /*──────────────────── Lettres ────────────────────*/
        private static string GetLetterTitle(string questId) => questId switch
        {
            "ActeI_VallonsFleuris"   => "🌸 Vallons Fleuris découverts",
            "ActeI_OceanSuspendu"    => "🌊 Océan suspendu localisé",
            "ActeI_SanctuaireAncien" => "🏛️ Sanctuaire ancien trouvé",
            "ActeI_NidEsquie"        => "⛰️ Nid d’Esquie repéré",
            "ActeI_Final"            => "💡 Sanctuaire de lumière révélé",
            "ActeII_TerresOubliees"  => "🌑 Terres oubliées découvertes",
            "ActeII_Manoir"          => "🏰 Manoir de Renoir localisé",
            "ActeII_LesAxons"        => "⚡ Gardiens Axons détectés",
            "ActeII_Final"           => "⚫ Monolithe révélé",
            _                        => "Site d’expédition découvert"
        };

        private static string GetLetterText(string questId, string boss) => questId switch
        {
            "ActeI_VallonsFleuris" =>
                "Nos éclaireurs ont repéré des vallons iridescents où rôdent les Névrons. "
              + "Formez une caravane pour éliminer " + boss + " et progresser dans l’expédition.",

            "ActeI_OceanSuspendu" =>
                "Un océan en lévitation défie toutes les lois ! "
              + boss + " en garde l’accès. Préparez-vous à l’impossible.",

            "ActeI_SanctuaireAncien" =>
                "Des ruines cyclopéennes recèlent les secrets du Gommage. "
              + boss + " vous y attend.",

            "ActeI_NidEsquie" =>
                "François règne sur ces falaises. Une erreur et vos colons seront balayés.",

            "ActeI_Final" =>
                "Le Maître des Lampes concentre la lumière pour barrer votre route ; sa défaite "
              + "ouvrira l’Acte II.",

            "ActeII_TerresOubliees" =>
                "Le Dualiste vous défie dans les Terres oubliées. Préparez-vous.",

            "ActeII_Manoir" =>
                "Renoir détient des fragments de vérité ; affrontez-le dans son manoir hanté.",

            "ActeII_LesAxons" =>
                "Les Axons protègent l’accès au Monolithe. Éliminez-les.",

            "ActeII_Final" =>
                "La Peintresse vous attend. L’ultime affrontement approche…",

            _ => $"Un objectif critique a été localisé. {boss} est sur place."
        };
    }

    /*───────────────────────────────────────────────────────────*\
    |  3.  Spawn du boss lorsqu’on génère la map du site         |
    \*───────────────────────────────────────────────────────────*/
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
                Log.Warning("[Expedition33] Aucune BossSiteExtension trouvée sur le site");
        }


        private static void SpawnBoss(Map map, string bossDefName)
        {
            if (string.IsNullOrEmpty(bossDefName))
            {
                Log.Error("[Expedition33] bossDefName vide : vérifiez l’IncidentDef !");
                return;
            }

            PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(bossDefName);
            if (kind == null)
            {
                Log.Error($"[Expedition33] PawnKindDef introuvable : {bossDefName}");
                return;
            }

            Faction bossFaction = Find.FactionManager.FirstFactionOfDef(
                DefDatabase<FactionDef>.GetNamedSilentFail("Expedition33_Nevrons"))
                ?? Faction.OfAncientsHostile;            // fallback sûr

            Pawn boss = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, bossFaction, PawnGenerationContext.NonPlayer, map.Tile));

            if (boss == null)
            {
                Log.Error($"[Expedition33] Échec de génération du boss « {bossDefName} »");
                return;                               // ⬅️ stoppe proprement
            }

            IntVec3 spawnPos;
            if (!CellFinder.TryFindRandomSpawnCellForPawnNear(
                    map.Center, map, out spawnPos, 12))
            {
                spawnPos = map.Center;          // position de secours
            }

            ConfigureBossHostileBehavior(boss, map);

            GenSpawn.Spawn(boss, spawnPos, map);
            if (boss.playerSettings != null)
                boss.playerSettings.hostilityResponse = HostilityResponseMode.Attack;

            var lord = LordMaker.MakeNewLord(
                boss.Faction, 
                new LordJob_DefendBossSite(spawnPos, 20f), 
                map, 
                new List<Pawn> { boss });

            // Empêche le boss de fuir individuellement (API 1.5)
            boss.mindState.exitMapAfterTick = -1;
            boss.mindState.forcedGotoPosition = IntVec3.Invalid;

            Messages.Message($"⚔️ {boss.LabelShort.CapitalizeFirst()} vous attend !",
                            new TargetInfo(spawnPos, map), MessageTypeDefOf.ThreatBig);
        }



        private static void ConfigureBossHostileBehavior(Pawn boss, Map map)
        {
            // Force l'état mental agressif
            if (boss.mindState != null)
            {
                boss.mindState.anyCloseHostilesRecently = true;
                boss.mindState.canFleeIndividual = false; // Le boss ne fuit pas
            }

            // Assure que le boss est en état de combat
            if (boss.jobs != null)
            {
                boss.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
            }

            var gommage = Find.FactionManager.FirstFactionOfDef(
                      DefDatabase<FactionDef>.GetNamed("Expedition33_Nevrons"));

            // ✅ Ne change de faction que si c’est nécessaire
            if (boss.Faction != gommage)
                boss.SetFaction(gommage);

            // Force l'agression contre les colons
            var playerPawns = map.mapPawns.FreeColonists.ToList();
            if (playerPawns.Any())
            {
                foreach (var colonist in playerPawns.Take(3)) // Cible les 3 premiers colons
                {
                    boss.mindState.enemyTarget = colonist;
                    break;
                }
            }
        }

    }

    /*───────────────────────────────────────────────────────────*\
            |  4.  Surveiller et recréer un site perdu (sauvegardes)      |
            \*───────────────────────────────────────────────────────────*/
        public static class BossSiteManager
        {
            private static readonly Dictionary<string, int> activeSitesTick = new();

            public static void EnsureBossSite(string questId)
            {
                if (SitePresent(questId) || activeSitesTick.ContainsKey(questId)) return;

                string incidentDefName = GetIncidentForQuest(questId);
                IncidentDef inc = DefDatabase<IncidentDef>.GetNamedSilentFail(incidentDefName);
                if (inc == null) return;

                IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, Find.World);
                parms.faction = Find.FactionManager.FirstFactionOfDef(
                    DefDatabase<FactionDef>.GetNamed("Expedition33_Nevrons"));

                if (inc.Worker.TryExecute(parms))
                {
                    activeSitesTick[questId] = GenTicks.TicksGame;
                    Log.Warning($"[Expedition33] Site recréé pour {questId}");
                }
            }

            /*── Helpers ───────────────────────────────────────────*/
            private static bool SitePresent(string questId) =>
                Find.WorldObjects.AllWorldObjects.OfType<Site>()
                    .Any(s => s.Label.Contains(GetExpectedSiteLabel(questId)));

            private static string GetIncidentForQuest(string questId) => questId switch
            {
                "ActeI_VallonsFleuris" => "Expedition33_SpawnEvequeSite",
                "ActeI_OceanSuspendu" => "Expedition33_SpawnGobluSite",
                "ActeI_SanctuaireAncien" => "Expedition33_SpawnSakapatateUltimeSite",
                "ActeI_NidEsquie" => "Expedition33_SpawnFrancoisSite",
                "ActeI_Final" => "Expedition33_SpawnMaitreDesLampesSite",
                "ActeII_TerresOubliees" => "Expedition33_SpawnDualisteSite",
                "ActeII_Manoir" => "Expedition33_SpawnRenoirSite",
                "ActeII_LesAxons" => "Expedition33_SpawnSireneSite",
                "ActeII_Final" => "Expedition33_SpawnPeintresseSite",
                _ => null
            };

            private static string GetExpectedSiteLabel(string questId) => questId switch
            {
                "ActeI_VallonsFleuris" => "Vallons Fleuris",
                "ActeI_OceanSuspendu" => "Océan Suspendu",
                "ActeI_SanctuaireAncien" => "Sanctuaire Ancien",
                "ActeI_NidEsquie" => "Nid d'Esquie",
                "ActeI_Final" => "Sanctuaire de Lumière",
                "ActeII_TerresOubliees" => "Terres Oubliées",
                "ActeII_Manoir" => "Manoir de Renoir",
                "ActeII_LesAxons" => "Axons",
                "ActeII_Final" => "Monolithe",
                _ => ""
            };
        }
}
