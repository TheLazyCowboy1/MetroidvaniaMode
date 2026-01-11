using System;
using System.Collections.Generic;

namespace MetroidvaniaMode.Creatures;

public static class CustomCreatures
{

    public static void ApplyHooks()
    {
        On.StaticWorld.InitCustomTemplates += StaticWorld_InitCustomTemplates;
    }

    public static void RemoveHooks()
    {
        On.StaticWorld.InitCustomTemplates -= StaticWorld_InitCustomTemplates;
    }

    /// <summary>
    /// Temporarily just adjusts pink lizards to think they can fly
    /// </summary>
    /// <param name="orig"></param>
    private static void StaticWorld_InitCustomTemplates(On.StaticWorld.orig_InitCustomTemplates orig)
    {
        orig();

        try
        {
            CreatureTemplate temp = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard);
            temp.doPreBakedPathing = false;
            temp.preBakedPathingAncestor = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly);
            temp.canFly = true;
            Plugin.Log("Made pink lizards think they can fly", 0);
        } catch (Exception ex) { Plugin.Error(ex); }
    }

}
