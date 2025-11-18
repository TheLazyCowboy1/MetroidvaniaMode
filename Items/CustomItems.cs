using System;
using System.Reflection;

namespace MetroidvaniaMode.Items;

public static class CustomItems
{
    [ItemID("HealFruit")]
    public static PlacedObject.Type HealFruit;
    [ItemID("HealFruit")]
    public static AbstractPhysicalObject.AbstractObjectType AbHealFruit;


    private class ItemID : Attribute
    {
        public string ID;
        public ItemID(string iD) : base()
        {
            ID = iD;
        }
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
                        info.SetValue(null, Activator.CreateInstance(info.FieldType, PREFIX + att.ID, true));
                        debug += att.ID + ", ";
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
