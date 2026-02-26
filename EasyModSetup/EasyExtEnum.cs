using System;
using System.Reflection;

namespace EasyModSetup;

[AttributeUsage(AttributeTargets.Field)]
public class EasyExtEnum : Attribute
{
    //private const string PREFIX = "MVM_";

    public string ID = null; //used to specify the ID

    public static void Register()
    {
        try
        {
            string debug = "Registered ExtEnums: ";
            string fieldErrors = "Field Errors: ";
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypesSafely())
            {
                FieldInfo[] infos = type.GetStaticFieldsSafely();
                foreach (FieldInfo info in infos)
                {
                    try
                    {
                        EasyExtEnum att = info.GetCustomAttribute<EasyExtEnum>();
                        if (att != null)
                        {
                            string name = att.ID ?? info.Name;
                            info.SetValue(null, Activator.CreateInstance(info.FieldType, name, true));
                            debug += type.Name + ":" + name + ", ";
                        }
                        else if (info.FieldType.IsSubclassOf(typeof(ExtEnumBase))) //look for ALL static ExtEnums
                        {
                            //reading the value will hopefully ensure they get initialized in a consistent order
                            if (info.GetValue(null) is ExtEnumBase val)
                                debug += $"read {type.Name}.{info.Name}:{val.value}, ";
                        }
                    }
                    catch { fieldErrors += $"{type.FullName}.{info.Name}, "; }
                }
            }

            SimplerPlugin.Log(debug, 1);
            SimplerPlugin.Log(fieldErrors, 2);
        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }
    }
    public static void Unregister()
    {
        string fieldErrors = "Field Errors: ";
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypesSafely())
        {
            FieldInfo[] infos = type.GetStaticFieldsSafely();
            foreach (FieldInfo info in infos)
            {
                try
                {
                    EasyExtEnum att = info.GetCustomAttribute<EasyExtEnum>();
                    if (att != null)
                    {
                        //(info.GetValue(null) as ExtEnum<>)?.Unregister();
                        info.FieldType.GetMethod("Unregister").Invoke(info.GetValue(null), new object[] { });
                    }
                }
                catch { fieldErrors += $"{type.FullName}.{info.Name}"; }
            }
            SimplerPlugin.Log(fieldErrors, 0);
        }
    }

}
