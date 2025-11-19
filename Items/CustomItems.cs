using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Reflection;

namespace MetroidvaniaMode.Items;

public static class CustomItems
{
    [ItemID]
    public static AbstractPhysicalObject.AbstractObjectType HealFruit;

    public static void ApplyHooks()
    {
        On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;
        On.ItemSymbol.SpriteNameForItem += ItemSymbol_SpriteNameForItem;
        On.ItemSymbol.ColorForItem += ItemSymbol_ColorForItem;

        //heal fruit
        IL.Player.GrabUpdate += Player_GrabUpdate;
    }

    public static void RemoveHooks()
    {
        On.AbstractPhysicalObject.Realize -= AbstractPhysicalObject_Realize;
        On.ItemSymbol.SpriteNameForItem -= ItemSymbol_SpriteNameForItem;
        On.ItemSymbol.ColorForItem -= ItemSymbol_ColorForItem;

        IL.Player.GrabUpdate -= Player_GrabUpdate;
    }

    //Object realizing
    private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
    {
        try
        {
            if (self.type == HealFruit)
            {
                self.realizedObject = new HealFruit(self);
                return;
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self);
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
                return new(0, 1, 0);
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        return orig(itemType, intData);
    }


    //Make Heal Fruit always edible, just like Mushrooms
    private static void Player_GrabUpdate(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            while (c.TryGotoNext(MoveType.After, x => x.MatchIsinst<Mushroom>())) //do this for EVERY "is Mushroom"
            {
                //"is Mushroom" is ALREADY loaded onto the stack, so we must reference it
                c.Emit(OpCodes.Ldarg_0); //load player
                c.Emit(OpCodes.Ldloc_S, 6); //load grasp idx
                c.EmitDelegate<Func<bool, Player, int, bool>>((bool isMush, Player self, int grasp) => isMush || self.grasps[grasp].grabbed is HealFruit);
                //c.Emit(OpCodes.Or); //mushroom OR heal fruit works
                
                //c.Emit(OpCodes.Dup); //duplicate the self.grasps[i].grabbed object, so we can test it twice
                //c.Emit(OpCodes.Isinst, typeof(HealFruit));
                //c.Index++; //skip IsInst<Mushroom>
                //c.Emit(OpCodes.Or);

                Plugin.Log("Heal Fruit IL hook successful! ...I hope");
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }


    private class ItemID : Attribute
    {
        //public string ID;
        //public ItemID(string iD) : base()
        //{
            //ID = iD;
        //}
    }

    private const string PREFIX = "MVM_";

    public static void Register()
    {
        try
        {
            string debug = "Registered Items: ";
            FieldInfo[] infos = typeof(CustomItems).GetFields();
            foreach (FieldInfo info in infos)
            {
                try
                {
                    ItemID att = info.GetCustomAttribute<ItemID>();
                    if (att != null)
                    {
                        info.SetValue(null, Activator.CreateInstance(info.FieldType, PREFIX + info.Name, true));
                        debug += PREFIX + info.Name + ", ";
                    }
                } catch (Exception ex) { Plugin.Error("Error with field " + info.Name); Plugin.Error(ex); }
            }

            Plugin.Log(debug, 0);
        } catch (Exception ex) { Plugin.Error(ex); }
    }
    public static void Unregister()
    {
        Plugin.Error("NOT IMPLEMENTED!");
    }
}
