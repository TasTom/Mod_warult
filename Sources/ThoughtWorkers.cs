using RimWorld;
using Verse;

namespace Mod_warult
{
    public class ThoughtWorker_GommageAnxiety : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            var gameComp = Current.Game.GetComponent<GameComponent_PaintressMonolith>();
            if (gameComp?.paintressAlive == true && gameComp.currentPaintedAge != -1)
            {
                // Anxiété si le colon est proche de l'âge maudit
                int ageGap = gameComp.currentPaintedAge - p.ageTracker.AgeBiologicalYears;
                if (ageGap <= 5 && ageGap >= 0)
                {
                    return ThoughtState.ActiveAtStage(0);
                }
            }
            return ThoughtState.Inactive;
        }
    }
}
