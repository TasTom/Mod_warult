using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace Mod_warult
{
    public class Window_ExpeditionProgression : Window
    {
        private Pawn selectedPawn;
        // private Vector2 scrollPosition;

        public Window_ExpeditionProgression(Pawn pawn)
        {
            selectedPawn = pawn;
            doCloseX = true;
            doCloseButton = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize => new Vector2(650f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            var progression = ExpeditionProgressionHelper.GetOrCreateProgression(selectedPawn);
            if (progression == null) return;

            // ✅ Zone de contenu avec marge
            Rect contentRect = new Rect(10f, 10f, inRect.width - 20f, inRect.height - 50f);

            float currentY = 0f;

            // Titre
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 35f);
            Widgets.Label(titleRect, $"Progression de {selectedPawn.Name}");
            Text.Font = GameFont.Small;
            currentY += 45f;

            // ✅ Ligne de séparation
            Widgets.DrawLineHorizontal(contentRect.x, contentRect.y + currentY, contentRect.width);
            currentY += 10f;

            // Informations de niveau
            DrawLevelInfo(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 80f), progression);
            currentY += 90f;

            // Graphique de progression XP
            DrawXPChart(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 80f), progression);
            currentY += 90f;

            // ✅ Ligne de séparation
            Widgets.DrawLineHorizontal(contentRect.x, contentRect.y + currentY, contentRect.width);
            currentY += 10f;

            // Attributs détaillés
            DrawAttributeDetails(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 200f), progression);
        }


        private void DrawLevelInfo(Rect rect, Hediff_ExpeditionProgression progression)
        {
            // ✅ Affichage niveau actuel - CORRIGÉ
            Text.Font = GameFont.Small;
            string levelText = $"Niveau {progression.currentLevel}";

            if (progression.currentLevel < 99)
            {
                float requiredXP = progression.XPRequiredForNextLevel();
                float currentXP = progression.TotalXP();
                float nextLevelXP = 0f;

                // Calculer l'XP pour le niveau actuel
                for (int i = 1; i < progression.currentLevel; i++)
                {
                    nextLevelXP += 100f + (i * 25f) + Mathf.Pow(i, 1.5f) * 10f;
                }

                float progressXP = currentXP - nextLevelXP;
                float progress = Mathf.Clamp01(progressXP / requiredXP);

                levelText += $" ({progress:P0} vers niveau {progression.currentLevel + 1})";

                // ✅ Barre de progression CORRIGÉE
                Rect progressBar = new Rect(rect.x, rect.y + 25f, rect.width - 20f, 20f);
                Widgets.FillableBar(progressBar, progress, SolidColorMaterials.NewSolidColorTexture(Color.blue), BaseContent.GreyTex, false);

                // Texte sur la barre
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(progressBar, $"{progressXP:F0} / {requiredXP:F0} XP");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            // ✅ Label niveau - Position corrigée
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25f), levelText);

            // ✅ XP détaillée - Position corrigée
            string xpText = $"Combat XP: {progression.combatXP:F0} | Âme XP: {progression.soulXP:F0} | Total: {progression.TotalXP():F0}";
            Widgets.Label(new Rect(rect.x, rect.y + 50f, rect.width, 25f), xpText);
        }


        private void DrawXPChart(Rect rect, Hediff_ExpeditionProgression progression)
        {
            Text.Font = GameFont.Small;

            // ✅ Titre du graphique
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "Répartition de l'Expérience");

            float totalXP = progression.combatXP + progression.soulXP;
            if (totalXP <= 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 25f, rect.width, 25f), "Aucune expérience acquise");
                return;
            }

            float combatRatio = progression.combatXP / totalXP;
            float soulRatio = progression.soulXP / totalXP;

            // ✅ Zone des barres - Position corrigée
            Rect barArea = new Rect(rect.x, rect.y + 25f, rect.width - 20f, 25f);

            // Barre Combat XP (rouge)
            if (combatRatio > 0)
            {
                Rect combatBar = new Rect(barArea.x, barArea.y, barArea.width * combatRatio, barArea.height);
                Widgets.DrawBoxSolid(combatBar, new Color(0.8f, 0.2f, 0.2f, 0.8f));
            }

            // Barre Soul XP (bleue)
            if (soulRatio > 0)
            {
                Rect soulBar = new Rect(barArea.x + (barArea.width * combatRatio), barArea.y, barArea.width * soulRatio, barArea.height);
                Widgets.DrawBoxSolid(soulBar, new Color(0.2f, 0.2f, 0.8f, 0.8f));
            }

            // ✅ Labels avec pourcentages - Position corrigée
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y + 55f, 250f, 20f),
                $"🔴 Combat: {progression.combatXP:F0} ({combatRatio:P0})");
            Widgets.Label(new Rect(rect.x + 250f, rect.y + 55f, 250f, 20f),
                $"🔵 Âme: {progression.soulXP:F0} ({soulRatio:P0})");
            Text.Font = GameFont.Small;
        }


        private void DrawAttributeDetails(Rect rect, Hediff_ExpeditionProgression progression)
        {
            float yOffset = 0f;

            // Titre de la section
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + yOffset, rect.width, 25f), "Attributs et Points");
            yOffset += 30f;

            // Points disponibles
            if (progression.availablePoints > 0)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(rect.x, rect.y + yOffset, rect.width, 25f),
                    $"⭐ Points disponibles: {progression.availablePoints}");
                GUI.color = Color.white;
                yOffset += 30f;
            }

            // ✅ ATTRIBUTS ÉTENDUS - 5 STATS
            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "💪 Vitalité", progression.vitality, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Vitality));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "⚔️ Puissance", progression.power, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Power));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "🏃 Agilité", progression.agility, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Agility));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "🛡️ Défense", progression.defense, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Defense));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "🍀 Chance", progression.chance, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Chance));
        }


        private void DrawAttributeRow(Rect rect, string attributeName, int value, bool canUpgrade, System.Action upgradeAction)
        {
            // ✅ Label attribut avec icône
            Widgets.Label(new Rect(rect.x, rect.y, 200f, rect.height), $"{attributeName}: {value}");

            // ✅ Bouton +1 - Position et style améliorés
            if (canUpgrade)
            {
                if (Widgets.ButtonText(new Rect(rect.x + 220f, rect.y, 50f, rect.height), "+1"))
                {
                    upgradeAction();
                }
            }
            else
            {
                // Bouton désactivé visuellement
                GUI.color = Color.gray;
                Widgets.ButtonText(new Rect(rect.x + 220f, rect.y, 50f, rect.height), "+1");
                GUI.color = Color.white;
            }
        }
    }

    // ✅ INTÉGRATION CORRECTE DU GIZMO DANS LE PATCH EXISTANT
    public static class ExpeditionGizmosExtension
    {
        public static void AddDetailedProgressionGizmo(this List<Gizmo> gizmos, Pawn pawn)
        {
            gizmos.Add(new Command_Action
            {
                defaultLabel = "Progression Détaillée",
                defaultDesc = "Ouvrir la fenêtre de progression complète",
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Progression"),
                action = () => Find.WindowStack.Add(new Window_ExpeditionProgression(pawn))
            });
        }
    }
    



}
