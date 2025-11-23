using MetroidvaniaMode.Tools;
using System;

namespace MetroidvaniaMode.Items;

public static class CustomItems
{
    [EasyExtEnum]
    public static AbstractPhysicalObject.AbstractObjectType HealFruit;

    public static void ApplyHooks()
    {
        On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;
        On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
        On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;

        //heal fruit
        //IL.Player.GrabUpdate += Player_GrabUpdate;
        On.Player.GrabUpdate += Player_GrabUpdate;
    }

    public static void RemoveHooks()
    {
        On.AbstractPhysicalObject.Realize -= AbstractPhysicalObject_Realize;
        On.ItemSymbol.SpriteNameForItem -= ItemSymbol_SpriteNameForItem;
        On.ItemSymbol.ColorForItem -= ItemSymbol_ColorForItem;

        //IL.Player.GrabUpdate -= Player_GrabUpdate;
        On.Player.GrabUpdate -= Player_GrabUpdate;
    }

    //Object realizing
    private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
    {
        orig(self);

        try //put this after, because we want the stuck objects to be realized too
        {
            if (self.type == HealFruit)
            {
                self.realizedObject = new HealFruit(self);
                return;
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

    //Sprite names
    private static string ItemSymbol_SpriteNameForItem(On.ItemSymbol.orig_SpriteNameForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        try
        {
            if (itemType == HealFruit)
            {
                return orig(AbstractPhysicalObject.AbstractObjectType.DangleFruit, intData); //use DangleFruit sprite
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        return orig(itemType, intData);
    }
    //Sprite colors
    private static UnityEngine.Color ItemSymbol_ColorForItem(On.ItemSymbol.orig_ColorForItem orig, AbstractPhysicalObject.AbstractObjectType itemType, int intData)
    {
        try
        {
            if (itemType == HealFruit)
            {
                return new(0, 0.9f, 0);
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        return orig(itemType, intData);
    }


    //Pretend player isn't full when attempting to eat heal fruits
    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        try
        {
            if (self.FoodInStomach >= self.MaxFoodInStomach)
            {
                bool firstFoodIsHealFruit = false;
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] != null && self.grasps[i].grabbed is IPlayerEdible ed && ed.Edible) //...make sure it's actually edible
                    {
                        firstFoodIsHealFruit = self.grasps[i].grabbed is HealFruit;
                        break;
                    }
                }
                if (firstFoodIsHealFruit)
                {
                    self.playerState.foodInStomach--; //temporarily let the player eat
                    orig(self, eu);
                    self.playerState.foodInStomach = self.MaxFoodInStomach;
                    return;
                }
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, eu);
    }

}
