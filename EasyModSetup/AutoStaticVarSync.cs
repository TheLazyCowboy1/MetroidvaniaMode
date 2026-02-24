using RainMeadow;
using System;
using System.Linq;
using System.Reflection;

namespace EasyModSetup;

public class AutoStaticVarSync
{
    public class AutoSynced : Attribute
    {
        //...there's nothing that really needs to go here
    }

    private static FieldInfo[] SyncedFields;
    private static PropertyInfo[] SyncedProperties;
    private static FieldInfo[] SyncedConfigs;

    private static bool IsSupportedType(Type t) => t == typeof(bool) || t == typeof(int) || t == typeof(float) || t == typeof(string);

    public static void RegisterSyncedVars()
    {
        var types = Assembly.GetExecutingAssembly().GetTypes();
        var tempFields = types.SelectMany(
            t => t.GetFields(BindingFlags.Static)
                .Where(f => f.GetCustomAttribute<AutoSynced>() != null)
            );
        //isolate Configurables
        SyncedConfigs = tempFields.Where(f => f.FieldType.IsSubclassOf(typeof(ConfigurableBase))).ToArray();
        SyncedFields = tempFields.Except(SyncedConfigs) //don't include configs
            .Where(f =>
            {
                if (IsSupportedType(f.FieldType)) return true; //it's supported; it's fine
                Plugin.Error($"Unsupported auto-sync type: {f.FieldType.Name} at {f.DeclaringType.FullName}");
                return false;
            }
            ).ToArray(); //everything but configs

        SyncedProperties = types.SelectMany(
            t => t.GetProperties(BindingFlags.Static)
                .Where(p =>
                {
                    if (p.GetCustomAttribute<AutoSynced>() == null) return false; //
                    if (IsSupportedType(p.PropertyType)) return true; //it's supported; it's fine
                    Plugin.Error($"Unsupported auto-sync type: {p.PropertyType.Name} at {p.DeclaringType.FullName}");
                    return false;
                }
                )
            ).ToArray();

        if (SyncedConfigs.Length > 0 || SyncedFields.Length > 0 || SyncedProperties.Length > 0) //only add it if it's actually storing something
        {
            try
            {
                StaticVarSyncData.HookToLobby();
            } catch (Exception ex) { Plugin.Error(ex); }
        }
    }

    //since these reference Meadow, they'll need to go in a separate file
    private class StaticVarSyncData : OnlineResource.ResourceData
    {
        public static void HookToLobby()
        {
            OnlineResource.OnAvailable += r => r.AddData(new StaticVarSyncData());
        }

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new StaticVarSyncState();
        }

        private class StaticVarSyncState : ResourceDataState
        {
            public override Type GetDataType() => typeof(StaticVarSyncData);

            [OnlineField]
            public bool[] bools;
            [OnlineField]
            public int[] ints;
            [OnlineField]
            public float[] floats;
            [OnlineField]
            public string[] strings;

            public StaticVarSyncState()
            {
                bools = SyncedFields.Where(f => f.FieldType == typeof(bool)).Select(f => (bool)f.GetValue(null))
                    .Concat(SyncedProperties.Where(p => p.PropertyType == typeof(bool)).Select(p => (bool)p.GetValue(null)))
                    .ToArray();
                ints = SyncedFields.Where(f => f.FieldType == typeof(int)).Select(f => (int)f.GetValue(null))
                    .Concat(SyncedProperties.Where(p => p.PropertyType == typeof(int)).Select(p => (int)p.GetValue(null)))
                    .ToArray();
                floats = SyncedFields.Where(f => f.FieldType == typeof(float)).Select(f => (float)f.GetValue(null))
                    .Concat(SyncedProperties.Where(p => p.PropertyType == typeof(float)).Select(p => (float)p.GetValue(null)))
                    .ToArray();
                strings = SyncedFields.Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null)) //pack fields first
                    .Concat(SyncedProperties.Where(p => p.PropertyType == typeof(string)).Select(p => (string)p.GetValue(null))) //then properties
                    .Concat(SyncedConfigs.Select(c => (c.GetValue(null) as ConfigurableBase).BoxedValue.ToString())) //then configs
                    .ToArray();
            }

            public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
            {
                int bi = 0, ii = 0, fi = 0, si = 0;
                foreach (FieldInfo f in SyncedFields) //go through all fields
                {
                    Type fType = f.FieldType;
                    if (fType == typeof(bool))
                        f.SetValue(null, bools[bi++]);
                    else if (fType == typeof(int))
                        f.SetValue(null, ints[ii++]);
                    else if (fType == typeof(float))
                        f.SetValue(null, floats[fi++]);
                    else if (fType == typeof(string))
                        f.SetValue(null, strings[si++]);
                }

                foreach (PropertyInfo p in SyncedProperties) //go through all properties WITHOUT resetting the counters
                {
                    Type pType = p.PropertyType;
                    if (pType == typeof(bool))
                        p.SetValue(null, bools[bi++]);
                    else if (pType == typeof(int))
                        p.SetValue(null, ints[ii++]);
                    else if (pType == typeof(float))
                        p.SetValue(null, floats[fi++]);
                    else if (pType == typeof(string))
                        p.SetValue(null, strings[si++]);
                }

                foreach (FieldInfo f in SyncedConfigs) //set all configs with remaining data
                {
                    (f.GetValue(null) as ConfigurableBase).BoxedValue = strings[si++];
                }
            }
        }
    }

}
