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

            Rect contentRect = new Rect(10f, 10f, inRect.width - 20f, inRect.height - 50f);
            float currentY = 0f;

            // Titre
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 35f);
            Widgets.Label(titleRect, "Expedition33_ProgressionTitle".Translate(selectedPawn.Name.ToStringShort));
            Text.Font = GameFont.Small;
            currentY += 45f;

            // Ligne de séparation
            Widgets.DrawLineHorizontal(contentRect.x, contentRect.y + currentY, contentRect.width);
            currentY += 10f;

            // Informations de niveau
            DrawLevelInfo(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 80f), progression);
            currentY += 90f;

            // Graphique de progression XP
            DrawXPChart(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 80f), progression);
            currentY += 90f;

            // Ligne de séparation
            Widgets.DrawLineHorizontal(contentRect.x, contentRect.y + currentY, contentRect.width);
            currentY += 10f;

            // Attributs détaillés
            DrawAttributeDetails(new Rect(contentRect.x, contentRect.y + currentY, contentRect.width, 200f), progression);
        }

        private void DrawLevelInfo(Rect rect, Hediff_ExpeditionProgression progression)
        {
            Text.Font = GameFont.Small;
            string levelText = "Expedition33_CurrentLevel".Translate(progression.currentLevel);
            
            if (progression.currentLevel < 99)
            {
                float requiredXP = progression.XPRequiredForNextLevel();
                float currentXP = progression.TotalXP();
                float nextLevelXP = 0f;

                for (int i = 1; i < progression.currentLevel; i++)
                {
                    nextLevelXP += 100f + (i * 25f) + Mathf.Pow(i, 1.5f) * 10f;
                }

                float progressXP = currentXP - nextLevelXP;
                float progress = Mathf.Clamp01(progressXP / requiredXP);
                levelText += " " + "Expedition33_ProgressToNext".Translate(
                    (progress * 100f).ToString("F0"), progression.currentLevel + 1);

                // Barre de progression
                Rect progressBar = new Rect(rect.x, rect.y + 25f, rect.width - 20f, 20f);
                Widgets.FillableBar(progressBar, progress, SolidColorMaterials.NewSolidColorTexture(Color.blue), BaseContent.GreyTex, false);

                // Texte sur la barre
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Tiny;
                Widgets.Label(progressBar, "Expedition33_XPProgress".Translate(progressXP.ToString("F0"), requiredXP.ToString("F0")));
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
            }

            // Label niveau
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 25f), levelText);
            
            // XP détaillée
            string xpText = "Expedition33_XPBreakdown".Translate(
                progression.combatXP.ToString("F0"), 
                progression.soulXP.ToString("F0"), 
                progression.TotalXP().ToString("F0"));
            Widgets.Label(new Rect(rect.x, rect.y + 50f, rect.width, 25f), xpText);
        }

        private void DrawXPChart(Rect rect, Hediff_ExpeditionProgression progression)
        {
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "Expedition33_ExperienceDistribution".Translate());

            float totalXP = progression.combatXP + progression.soulXP;
            if (totalXP <= 0)
            {
                Widgets.Label(new Rect(rect.x, rect.y + 25f, rect.width, 25f), "Expedition33_NoExperienceYet".Translate());
                return;
            }

            float combatRatio = progression.combatXP / totalXP;
            float soulRatio = progression.soulXP / totalXP;

            // Zone des barres
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

            // Labels avec pourcentages
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y + 55f, 250f, 20f),
                "Expedition33_CombatXPLabel".Translate(progression.combatXP.ToString("F0"), (combatRatio * 100f).ToString("F0")));
            Widgets.Label(new Rect(rect.x + 250f, rect.y + 55f, 250f, 20f),
                "Expedition33_SoulXPLabel".Translate(progression.soulXP.ToString("F0"), (soulRatio * 100f).ToString("F0")));
            Text.Font = GameFont.Small;
        }

        private void DrawAttributeDetails(Rect rect, Hediff_ExpeditionProgression progression)
        {
            float yOffset = 0f;
            
            // Titre de la section
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(rect.x, rect.y + yOffset, rect.width, 25f), "Expedition33_AttributesAndPoints".Translate());
            yOffset += 30f;

            // Points disponibles
            if (progression.availablePoints > 0)
            {
                GUI.color = Color.yellow;
                Widgets.Label(new Rect(rect.x, rect.y + yOffset, rect.width, 25f),
                    "Expedition33_AvailablePoints".Translate(progression.availablePoints));
                GUI.color = Color.white;
                yOffset += 30f;
            }

            // Attributs étendus - 5 stats
            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "Expedition33_VitalityAttribute".Translate(), progression.vitality, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Vitality));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "Expedition33_PowerAttribute".Translate(), progression.power, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Power));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "Expedition33_AgilityAttribute".Translate(), progression.agility, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Agility));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "Expedition33_DefenseAttribute".Translate(), progression.defense, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Defense));
            yOffset += 35f;

            DrawAttributeRow(new Rect(rect.x, rect.y + yOffset, rect.width, 30f),
                "Expedition33_ChanceAttribute".Translate(), progression.chance, progression.availablePoints > 0,
                () => progression.TrySpendAttributePoint(AttributeType.Chance));
        }

        private void DrawAttributeRow(Rect rect, string attributeName, int value, bool canUpgrade, System.Action upgradeAction)
        {
            // Label attribut avec icône
            Widgets.Label(new Rect(rect.x, rect.y, 200f, rect.height), "Expedition33_AttributeValue".Translate(attributeName, value));
            
            // Bouton +1
            if (canUpgrade)
            {
                if (Widgets.ButtonText(new Rect(rect.x + 220f, rect.y, 50f, rect.height), "Expedition33_UpgradeButton".Translate()))
                {
                    upgradeAction();
                }
            }
            else
            {
                // Bouton désactivé visuellement
                GUI.color = Color.gray;
                Widgets.ButtonText(new Rect(rect.x + 220f, rect.y, 50f, rect.height), "Expedition33_UpgradeButton".Translate());
                GUI.color = Color.white;
            }
        }
    }

    public static class ExpeditionGizmosExtension
    {
        public static void AddDetailedProgressionGizmo(this List<Gizmo> gizmos, Pawn pawn)
        {
            gizmos.Add(new Command_Action
            {
                defaultLabel = "Expedition33_DetailedProgression".Translate(),
                defaultDesc = "Expedition33_DetailedProgressionDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Progression"),
                action = () => Find.WindowStack.Add(new Window_ExpeditionProgression(pawn))
            });
        }
    }
}
