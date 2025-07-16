using System.Collections.Generic;
using Verse;

namespace Mod_warult
{
    public class QuestData
    {
        public string questId;
        public string title;
        public string description;
        public List<string> objectives;
        public string triggerCondition;
        public string nextQuestId;
    }

    [StaticConstructorOnStartup]
    public static class QuestManager
    {
        public static readonly Dictionary<string, QuestData> AllQuests;

        static QuestManager()
        {
            AllQuests = new Dictionary<string, QuestData>
            {
                // ═══════════════════════════════════════════════════════════
                // PROLOGUE : LE DÉPART DE LUMIÈRE
                // ═══════════════════════════════════════════════════════════
                ["Prologue_Start"] = new QuestData
                {
                    questId = "Prologue_Start",
                    title = "Le Gommage de Sophie",
                    description = "La cérémonie du Gommage a eu lieu. Sophie a disparu dans les pétales. L'Expédition 33 doit maintenant partir vers le continent mystérieux pour briser ce cycle mortel.",
                    objectives = new List<string> { "Partir vers le continent.", "Survivre au voyage." },
                    triggerCondition = "EVENT_DEPARTURE",
                    nextQuestId = "ActeI_VallonsFleuris"
                },

                // ═══════════════════════════════════════════════════════════
                // ACTE I : L'EXPÉDITION DE GUSTAVE
                // ═══════════════════════════════════════════════════════════
                ["ActeI_VallonsFleuris"] = new QuestData
                {
                    questId = "ActeI_VallonsFleuris",
                    title = "Les Vallons Fleuris",
                    description = "Première exploration du continent. Une terre apparemment paisible cache des dangers inattendus. Un Mime mystérieux rôde dans les environs.",
                    objectives = new List<string> { "Explorer les Vallons Fleuris.", "Affronter les créatures locales.", "Vaincre l'Eveque." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Eveque",
                    nextQuestId = "ActeI_OceanSuspendu"
                },

                ["ActeI_OceanSuspendu"] = new QuestData
                {
                    questId = "ActeI_OceanSuspendu",
                    title = "L'Océan Suspendu",
                    description = "Un phénomène impossible : un océan qui flotte dans les airs. Ici, vous devez maîtriser la mystérieuse énergie de la Lumina et améliorer votre équipement.",
                    objectives = new List<string> { "Atteindre l'Océan Suspendu.", "Maîtriser la Lumina.", "Vaincre le Goblu." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Goblu",
                    nextQuestId = "ActeI_SanctuaireAncien"
                },

                ["ActeI_SanctuaireAncien"] = new QuestData
                {
                    questId = "ActeI_SanctuaireAncien",
                    title = "Le Sanctuaire Ancien",
                    description = "Ruines d'une civilisation oubliée. Le Village des Gestrals offre des quêtes secondaires et des indices sur l'histoire de ce monde.",
                    objectives = new List<string> { "Explorer le Sanctuaire Ancien.", "Aider le Village des Gestrals.", "Affronter le Sakapatate Ultime." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_SakapatateUltime",
                    nextQuestId = "ActeI_NidEsquie"
                },

                ["ActeI_NidEsquie"] = new QuestData
                {
                    questId = "ActeI_NidEsquie",
                    title = "Le Nid d'Esquie",
                    description = "Dans ces falaises dangereuses, vous devez obtenir une monture Esquie et développer la capacité de traverser les étendues d'eau. Mais un danger mortel vous attend.",
                    objectives = new List<string> { "Atteindre le Nid d'Esquie.", "Obtenir une monture Esquie.", "Survivre à François." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Francois",
                    nextQuestId = "ActeI_Final"
                },

                ["ActeI_Final"] = new QuestData
                {
                    questId = "ActeI_Final",
                    title = "Le Maître des Lampes",
                    description = "Le gardien final de l'Acte I. Une créature mystérieuse qui contrôle la lumière elle-même. Sa défaite ouvrira la voie vers l'Acte II et l'arrivée de Verso.",
                    objectives = new List<string> { "Localiser le Maître des Lampes.", "Vaincre le gardien de la lumière." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_MaitreDesLampes",
                    nextQuestId = "ActeII_VersoArrival"
                },

                // ═══════════════════════════════════════════════════════════
                // ACTE II : VERSO ET LA ROUTE VERS LE MONOLITHE
                // ═══════════════════════════════════════════════════════════
                ["ActeII_VersoArrival"] = new QuestData
                {
                    questId = "ActeII_VersoArrival",
                    title = "L'Arrivée de Verso",
                    description = "Un homme mystérieux apparaît : Verso, survivant de l'Expédition Zéro. Il connaît des secrets sur le Gommage et la Peintresse. Son arrivée marque le début d'une nouvelle phase.",
                    objectives = new List<string> { "Accueillir Verso dans l'expédition." },
                    triggerCondition = "EVENT_VERSO_JOINED",
                    nextQuestId = "ActeII_TerresOubliees"
                },

                ["ActeII_TerresOubliees"] = new QuestData
                {
                    questId = "ActeII_TerresOubliees",
                    title = "Les Terres Oubliées",
                    description = "Guidé par Verso, votre groupe pénètre dans des régions encore plus dangereuses. Un épéiste maître de la dualité vous barre la route.",
                    objectives = new List<string> { "Explorer les Terres Oubliées.", "Affronter les nouvelles menaces.", "Vaincre le Dualliste." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Dualiste",
                    nextQuestId = "ActeII_Manoir"
                },

                ["ActeII_Manoir"] = new QuestData
                {
                    questId = "ActeII_Manoir",
                    title = "Le Manoir de Renoir",
                    description = "Un manoir imposant se dresse devant vous. À l'intérieur, Renoir, une figure énigmatique liée au passé des expéditions, vous attend pour un affrontement décisif.",
                    objectives = new List<string> { "Atteindre le Manoir.", "Confronter Renoir.", "Découvrir les secrets du passé." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Renoir",
                    nextQuestId = "ActeII_LesAxons"
                },

                ["ActeII_LesAxons"] = new QuestData
                {
                    questId = "ActeII_LesAxons",
                    title = "Les Gardiens Axons",
                    description = "Pour atteindre le Monolithe de la Peintresse, vous devez vaincre ses deux gardiens : la Sirène et les Visages. Seule leur défaite permettra d'obtenir l'aide du Conservateur.",
                    objectives = new List<string> { "Localiser les Axons.", "Vaincre la Sirène.", "Vaincre les Visages.", "Obtenir l'aide du Conservateur." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Visages",
                    nextQuestId = "ActeII_Final"
                },

                // ═══════════════════════════════════════════════════════════
                // CONFRONTATION FINALE
                // ═══════════════════════════════════════════════════════════
                ["ActeII_Final"] = new QuestData
                {
                    questId = "ActeII_Final",
                    title = "L'Ascension du Monolithe",
                    description = "Le moment final est arrivé. Avec le Brise-bouclier en main, vous devez gravir le Monolithe et affronter la Peintresse elle-même pour briser le cycle du Gommage une fois pour toutes.",
                    objectives = new List<string> { "Obtenir le Brise-bouclier.", "Détruire la barrière du Monolithe.", "Gravir le Monolithe.", "Affronter la Peintresse." },
                    triggerCondition = "BOSS_DEFEATED_Expedition33_Peintresse",
                    nextQuestId = null // Fin de la progression narrative
                }
            };
        }
    }
}
