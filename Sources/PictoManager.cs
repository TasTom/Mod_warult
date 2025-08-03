using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Mod_warult
{
    public class PictoManager : GameComponent
    {
        /*–––––– SINGLETON ––––––*/
        public static PictoManager Instance { get; private set; }

        /*–––––– DONNÉES SAUVEGARDÉES ––––––*/
        private HashSet<string> learnedPictoTypes = new HashSet<string>();
        private Dictionary<string, string> pictoAbilities = new Dictionary<string, string>();

        /*–––––– SUIVI EN PARTIE ––––––*/
        private readonly List<CompPicto> activePictos = new List<CompPicto>();

        public PictoManager(Game _) => Instance = this;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref learnedPictoTypes, "learnedPictoTypes", LookMode.Value);
            Scribe_Collections.Look(ref pictoAbilities, "pictoAbilities", LookMode.Value, LookMode.Value);

            if (learnedPictoTypes == null)
                learnedPictoTypes = new HashSet<string>();
            if (pictoAbilities == null)
                pictoAbilities = new Dictionary<string, string>();
        }

        /*------------------ ENREGISTREMENT ------------------*/
        public void RegisterPicto(CompPicto picto)
        {
            if (picto != null && !activePictos.Contains(picto))
                activePictos.Add(picto);
        }

        public void LearnPicto(string pictoType, string ability)
        {
            learnedPictoTypes.Add(pictoType);
            pictoAbilities[pictoType] = ability;
            Log.Message("Expedition33_PictoMastered".Translate(pictoType, ability));
        }

        /*------------------ INTERROGATION ------------------*/
        public bool IsPictoLearned(string pictoType) => learnedPictoTypes.Contains(pictoType);
        public List<string> GetLearnedPictos() => learnedPictoTypes.ToList();

        /*------------------ LUMINAS ------------------*/
        public void ActivateLumina(string pictoType, Pawn caster)
        {
            if (!IsPictoLearned(pictoType))
            {
                Messages.Message("Expedition33_PictoNotLearned".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            foreach (var pawn in PawnsFinder.AllMaps_FreeColonists)
                ApplyLuminaEffect(pawn, pictoType);

            Messages.Message("Expedition33_LuminaActivated".Translate(pictoType),
                MessageTypeDefOf.PositiveEvent, false);
        }

        private static void ApplyLuminaEffect(Pawn pawn, string pictoType)
        {
            var luminaDef = HediffDef.Named($"Expedition33_Lumina_{pictoType}");
            if (luminaDef == null) return;

            var h = HediffMaker.MakeHediff(luminaDef, pawn);
            h.Severity = 1f;
            pawn.health.AddHediff(h);
        }
    }
}
