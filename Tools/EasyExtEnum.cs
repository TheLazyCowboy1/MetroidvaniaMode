using System;
using System.Reflection;

namespace MetroidvaniaMode.Tools;

[AttributeUsage(AttributeTargets.Field)]
public class EasyExtEnum : Attribute
{
    private const string PREFIX = "MVM_";

    public static void Register()
    {
        try
        {
            string debug = "Registered ExtEnums: ";
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                FieldInfo[] infos = type.GetFields();
                foreach (FieldInfo info in infos)
                {
                    try
                    {
                        EasyExtEnum att = info.GetCustomAttribute<EasyExtEnum>();
                        if (att != null)
                        {
                            info.SetValue(null, Activator.CreateInstance(info.FieldType, PREFIX + info.Name, true));
                            debug += type.Name + ":" + PREFIX + info.Name + ", ";
                        }
                    }
                    catch (Exception ex) { Plugin.Error("Error with field " + info.Name); Plugin.Error(ex); }
                }
            }

            Plugin.Log(debug, 0);
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }
    public static void Unregister()
    {
        Plugin.Error("NOT IMPLEMENTED!");
    }

}
