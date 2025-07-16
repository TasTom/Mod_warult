using RimWorld;
using Verse;
using System.Linq;

namespace Mod_warult
{
    public static class NarrativeEvents
    {
        public static void TriggerVersoArrival()
        {
            var existingVerso = Find.Maps
                .SelectMany(m => m.mapPawns.AllPawns)
                .FirstOrDefault(p => p.kindDef?.defName == "Expedition33_Verso");

            if (existingVerso != null)
            {
                Log.Warning("[Expedition33] Verso existe déjà, annulation du spawn");
                // ✅ Déclenche l'événement même si Verso existe déjà
                var trackerquest = QuestEventUtility.FindQuestTracker();
                if (trackerquest != null && trackerquest.currentQuestId == "ActeII_VersoArrival")
                    QuestEventUtility.TriggerGlobalEvent("EVENT_VERSO_JOINED");
                return;
            }

            // Génère Verso et l'ajoute à la faction du joueur
            var verso = PawnGenerator.GeneratePawn(
                DefDatabase<PawnKindDef>.GetNamed("Expedition33_Verso"));
            verso.SetFaction(Faction.OfPlayer);

            var map = Find.Maps.Where(m => m.IsPlayerHome).FirstOrDefault();
            if (map != null)
            {
                IntVec3 spawnSpot = CellFinder.RandomClosewalkCellNear(
                    map.Center, map, 20, x => x.Standable(map));

                GenSpawn.Spawn(verso, spawnSpot, map);

                Find.WindowStack.Add(new Dialog_MessageBox(
                    "🔮 UN ÉTRANGER MYSTÉRIEUX\n\n" +
                    "Un homme aux cheveux blancs apparaît près de votre campement. " +
                    "Malgré son apparence âgée, il porte l'uniforme des expéditionnaires.\n\n" +
                    "\"Je suis Verso, survivant de l'Expédition Zéro. Je connais les secrets " +
                    "que vous cherchez, et les horreurs qui vous attendent. Laissez-moi " +
                    "vous guider vers la vérité sur la Peintresse et le Gommage.\"\n\n" +
                    "Verso a rejoint votre expédition !"));
            }

            // ✅ AJOUTE CETTE LIGNE : Déclenche l'événement après le spawn
            var tracker = QuestEventUtility.FindQuestTracker();
            if (tracker != null && tracker.currentQuestId == "ActeII_VersoArrival")
                QuestEventUtility.TriggerGlobalEvent("EVENT_VERSO_JOINED");
        }




        public static void TriggerActeICompletion()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "🌅 FIN DE L'ACTE I\n\n" +
                "Le Maître des Lampes s'effondre dans un éclat de lumière pure. " +
                "Ses dernières paroles résonnent dans l'air :\n\n" +
                "\"L'Acte I... se termine... Mais les véritables épreuves... " +
                "commencent maintenant...\"\n\n" +
                "Une nouvelle phase de votre expédition débute. Les mystères " +
                "s'approfondissent, et la route vers la Peintresse se dessine."));
        }
        
        public static void TriggerFinalVictory()
        {
            Find.WindowStack.Add(new Dialog_MessageBox(
                "🎨 VICTOIRE FINALE\n\n" +
                "La Peintresse s'effondre, son pinceau cosmique se brisant en mille éclats. " +
                "Le Monolithe noir se fissure, et tous les nombres inscrits s'effacent " +
                "dans un flash de lumière pure.\n\n" +
                "\"Le... cycle... est... brisé...\"\n\n" +
                "Pour la première fois depuis des générations, aucun nombre ne sera " +
                "inscrit. Le Gommage appartient désormais au passé.\n\n" +
                "🏆 FÉLICITATIONS ! VOUS AVEZ COMPLÉTÉ EXPEDITION 33 !\n\n" +
                "L'humanité est libre du cycle mortel. Votre courage et votre " +
                "détermination ont sauvé tous ceux qui viendront après vous."));
        }
    }
}
