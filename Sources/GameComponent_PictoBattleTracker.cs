
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace Mod_warult
{

    public class GameComponent_PictoBattleTracker : GameComponent
{
    public GameComponent_PictoBattleTracker(Game game) : base() { }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 == 0)
        {
            CheckForCompletedBattles();
        }
    }

    private void CheckForCompletedBattles()
    {
        foreach (Map map in Find.Maps)
        {
            // CORRIGÉ : Utiliser une méthode différente pour détecter les combats
            var hostilePawns = map.mapPawns.AllPawnsSpawned
                .Where(p => p.Faction != null && p.Faction.HostileTo(Faction.OfPlayer))
                .ToList();

            // Si aucun ennemi hostile visible, considérer que le combat est terminé
            if (!hostilePawns.Any() && map.mapPawns.FreeColonistsSpawned.Any())
            {
                RegisterBattleVictoryForPictoWearers(map);
            }
        }
    }

    private void RegisterBattleVictoryForPictoWearers(Map map)
    {
        var colonists = map.mapPawns.FreeColonistsSpawned;
        
        foreach (Pawn colonist in colonists)
        {
            var pictos = GetEquippedPictos(colonist);
            
            foreach (var picto in pictos)
            {
                var comp = picto.TryGetComp<CompPictoProgress>();
                comp?.RegisterBattleVictory(colonist);
            }
        }
    }

    private List<Apparel> GetEquippedPictos(Pawn pawn)
    {
        if (pawn?.apparel?.WornApparel == null) return new List<Apparel>();
        
        return pawn.apparel.WornApparel
            .Where(a => a.def.apparel?.tags?.Contains("Expedition33_Picto") == true)
            .ToList();
    }
}


}