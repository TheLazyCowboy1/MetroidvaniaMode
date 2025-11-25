using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Abilities;

/// <summary>
/// Abilities that are nothing more than changing a pre-existing stat or variable.
/// </summary>
public static class StatAbilities
{
    /// <summary>
    /// Run when the game/cycle is started
    /// </summary>
    public static void ApplyStaticStats()
    {
        //StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat).
    }

    public static void ApplyHooks()
    {
        On.Player.ctor += Player_ctor;
    }

    public static void RemoveHooks()
    {
        On.Player.ctor -= Player_ctor;
    }


    //set player stats
    private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        //Acid Immunity
        abstractCreature.lavaImmune = CurrentAbilities.AcidImmunity;

        orig(self, abstractCreature, world);

        //Stat changes
        self.slugcatStats.runspeedFac += CurrentAbilities.ExtraRunSpeed;
    }
}
