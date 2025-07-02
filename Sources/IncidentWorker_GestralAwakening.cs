using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Mod_warult // CORRIGÉ le namespace
{
    public class IncidentWorker_GestralAwakening : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapPawns.FreeColonistsSpawned
                .Any(p => p.def.defName == "Expedition33_Gestral");
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            var gestrals = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.def.defName == "Expedition33_Gestral").ToList();

            if (!gestrals.Any()) return false;

            List<Thing> rewards = GenerateExpeditionRewards();

            // CORRIGÉ la signature
            DropCellFinder.FindSafeLandingSpot(out IntVec3 dropSpot, null, map);

            foreach (Thing reward in rewards)
            {
                GenPlace.TryPlaceThing(reward, dropSpot, map, ThingPlaceMode.Near);
            }

            foreach (Pawn gestral in gestrals)
            {
                ApplyAncestralBlessing(gestral);
            }

            Find.LetterStack.ReceiveLetter(def.letterLabel, def.letterText, def.letterDef);
            return true;
        }

        private List<Thing> GenerateExpeditionRewards()
        {
            List<Thing> rewards = new List<Thing>();

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Rand.Range(200, 500);
            rewards.Add(silver);

            Thing components = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
            components.stackCount = Rand.Range(5, 15);
            rewards.Add(components);

            return rewards;
        }

        private void ApplyAncestralBlessing(Pawn gestral)
        {
            Hediff blessing = HediffMaker.MakeHediff(
                HediffDef.Named("Expedition33_AncestralBlessing"), gestral);
            gestral.health.AddHediff(blessing);
        }
    }

    public class IncidentWorker_SacredRiverPilgrimage : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapPawns.FreeColonistsSpawned
                .Any(p => p.def.defName == "Expedition33_Gestral");
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            var gestrals = map.mapPawns.FreeColonistsSpawned
                .Where(p => p.def.defName == "Expedition33_Gestral").ToList();
            
            if (!gestrals.Any()) return false;

            // CORRIGÉ Math.Min au lieu de Mathf.Min
            int pilgrims = Math.Min(gestrals.Count, Rand.Range(1, 4));
            var chosenPilgrims = gestrals.InRandomOrder().Take(pilgrims).ToList();

            foreach (Pawn pilgrim in chosenPilgrims)
            {
                ApplyPilgrimageBlessing(pilgrim);
            }

            GivePilgrimageRewards(map);

            string pilgrimage_text = $"{def.letterText}\n\n";
            pilgrimage_text += $"Les Gestrals partis en pèlerinage :\n";
            foreach (Pawn pilgrim in chosenPilgrims)
            {
                pilgrimage_text += $"• {pilgrim.Name.ToStringShort}\n";
            }
            pilgrimage_text += "\nIls reviendront avec des bénédictions ancestrales.";

            Find.LetterStack.ReceiveLetter(def.letterLabel, pilgrimage_text, def.letterDef);
            return true;
        }

        private void ApplyPilgrimageBlessing(Pawn gestral)
        {
            HediffDef blessingDef = HediffDef.Named("Expedition33_PilgrimageBlessing");
            if (blessingDef != null)
            {
                Hediff blessing = HediffMaker.MakeHediff(blessingDef, gestral);
                gestral.health.AddHediff(blessing);
            }
        }

        private void GivePilgrimageRewards(Map map)
        {
            List<Thing> rewards = new List<Thing>();
            
            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = Rand.Range(150, 300);
            rewards.Add(silver);

            Thing jade = ThingMaker.MakeThing(ThingDefOf.Jade);
            jade.stackCount = Rand.Range(5, 15);
            rewards.Add(jade);

            // CORRIGÉ la signature
            DropCellFinder.FindSafeLandingSpot(out IntVec3 dropSpot, null, map);
            foreach (Thing reward in rewards)
            {
                GenPlace.TryPlaceThing(reward, dropSpot, map, ThingPlaceMode.Near);
            }
        }
    }
}
