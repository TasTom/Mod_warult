using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Mod_warult
{
    public class Building_AntiGommageField : Building
    {
        private CompPowerTrader powerComp;
        private CompFlickable flickableComp;
        private static readonly int ProtectionRadius = 15;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            powerComp = GetComp<CompPowerTrader>();
            flickableComp = GetComp<CompFlickable>();
        }

        public bool IsActive => powerComp.PowerOn && (flickableComp?.SwitchIsOn ?? true);

        protected override void Tick()
        {
            base.Tick();
            if (IsActive && this.IsHashIntervalTick(250))
            {
                ApplyProtectionToPawns();
            }
        }

        private void ApplyProtectionToPawns()
        {
            foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
            {
                if (Position.DistanceTo(pawn.Position) <= ProtectionRadius)
                {
                    ApplyAntiGommageProtection(pawn);
                }
            }
        }

        private void ApplyAntiGommageProtection(Pawn pawn)
        {
            var protectionDef = HediffDef.Named("Expedition33_AntiGommageProtection");
            if (protectionDef != null)
            {
                var existing = pawn.health.hediffSet.GetFirstHediffOfDef(protectionDef);
                if (existing != null)
                {
                    existing.Severity = 1f;
                }
                else
                {
                    var hediff = HediffMaker.MakeHediff(protectionDef, pawn);
                    hediff.Severity = 1f;
                    pawn.health.AddHediff(hediff);
                }
            }
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            if (IsActive)
            {
                GenDraw.DrawRadiusRing(Position, ProtectionRadius, Color.yellow);
            }
        }

        public override string GetInspectString()
        {
            var text = base.GetInspectString();
            if (IsActive)
            {
                text += "\n" + "Expedition33_FieldActive".Translate();
                text += "\n" + "Expedition33_ProtectionRadius".Translate(ProtectionRadius);
                int protectedPawns = 0;
                foreach (var pawn in Map.mapPawns.FreeColonistsSpawned)
                {
                    if (Position.DistanceTo(pawn.Position) <= ProtectionRadius)
                        protectedPawns++;
                }
                text += "\n" + "Expedition33_ColonistsProtected".Translate(protectedPawns);
            }
            else
            {
                text += "\n" + "Expedition33_FieldInactive".Translate();
            }
            return text;
        }
    }

    [StaticConstructorOnStartup]
    public class GameComponent_AntiGommageRenderer : GameComponent
    {
        private static Material fieldMaterial;
        private static MaterialPropertyBlock matPropertyBlock;
        private static bool materialsInitialized = false;

        public GameComponent_AntiGommageRenderer(Game game) : base()
        {
            // Initialisation différée dans le thread principal
        }

        // Initialisation différée dans le thread principal
        private static void EnsureMaterialsInitialized()
        {
            if (!materialsInitialized)
            {
                fieldMaterial = MaterialPool.MatFrom("Effects/AntiGommageField", ShaderDatabase.Transparent);
                matPropertyBlock = new MaterialPropertyBlock();
                materialsInitialized = true;
            }
        }

        public override void GameComponentTick()
        {
            if (Find.CurrentMap == null) return;
            EnsureMaterialsInitialized();
            foreach (var building in Find.CurrentMap.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_AntiGommageField field && field.IsActive)
                {
                    DrawFieldEffect(field);
                }
            }
        }

        private void DrawFieldEffect(Building_AntiGommageField field)
        {
            Vector3 center = field.TrueCenter();
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            float fieldSize = 30f; // Rayon de 15 cases * 2
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one * fieldSize);
            float breathe = (Mathf.Sin(Time.realtimeSinceStartup * 1.5f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(0.2f, 0.4f, breathe);
            Color fieldColor = new Color(1f, 1f, 1f, alpha);
            matPropertyBlock.SetColor("_Color", fieldColor);
            Graphics.DrawMesh(MeshPool.plane10, matrix, fieldMaterial, 0, null, 0, matPropertyBlock);
        }
    }
}
