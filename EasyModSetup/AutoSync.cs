using System;
using System.Linq;
using System.Reflection;

namespace EasyModSetup;

//public class AutoStaticVarSync
//{
public class AutoSync : Attribute
{
    //...there's nothing that really needs to go here
    //}

    public static FieldInfo[] SyncedFields;
    public static PropertyInfo[] SyncedProperties;
    public static FieldInfo[] SyncedConfigs;

    private static bool IsSupportedType(Type t) => t == typeof(bool) || t == typeof(int) || t == typeof(float) || t == typeof(string);

    public static void RegisterSyncedVars()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        var tempFields = types.SelectMany(
            t => t.GetFields(BindingFlags.Static)
                .Where(f => f.GetCustomAttribute<AutoSync>() != null)
            );
        //isolate Configurables
        SyncedConfigs = tempFields.Where(f => f.FieldType.IsSubclassOf(typeof(ConfigurableBase))).ToArray();
        SyncedFields = tempFields.Except(SyncedConfigs) //don't include configs
            .Where(f =>
            {
                if (IsSupportedType(f.FieldType)) return true; //it's supported; it's fine
                SimplerPlugin.Error($"Unsupported auto-sync type: {f.FieldType.Name} at {f.DeclaringType.FullName}");
                return false;
            }
            ).ToArray(); //everything but configs

        SyncedProperties = types.SelectMany(
            t => t.GetProperties(BindingFlags.Static)
                .Where(p =>
                {
                    if (p.GetCustomAttribute<AutoSync>() == null) return false; //
                    if (IsSupportedType(p.PropertyType)) return true; //it's supported; it's fine
                    SimplerPlugin.Error($"Unsupported auto-sync type: {p.PropertyType.Name} at {p.DeclaringType.FullName}");
                    return false;
                }
                )
            ).ToArray();

        /*if (SyncedConfigs.Length > 0 || SyncedFields.Length > 0 || SyncedProperties.Length > 0) //only add it if it's actually storing something
        {
            try
            {
                StaticVarSyncData.HookToLobby();
            } catch { SimplerPlugin.Log("Rain Meadow is not enabled", 0); }
        }*/
    }

}