using RainMeadow;
using System;
using System.Linq;
using System.Reflection;

namespace EasyModSetup.MeadowCompat;

/*
public class StaticVarSyncData : OnlineResource.ResourceData
{
    public static void HookToLobby()
    {
        Lobby.OnAvailable += r => r.AddData(new StaticVarSyncData());
    }

    public override ResourceDataState MakeState(OnlineResource resource)
    {
        return new StaticVarSyncState();
    }
*/
public class StaticVarSyncData : EasyResourceState
{
    //public override Type GetDataType() => typeof(StaticVarSyncData);

    public override bool AttachTo(OnlineResource resource) => resource is Lobby;

    [OnlineField]
    public bool[] bools;
    [OnlineField]
    public int[] ints;
    [OnlineField]
    public float[] floats;
    [OnlineField]
    public string[] strings;

    public override void WriteTo(OnlineResource.ResourceData data, OnlineResource resource)
    {
        bools = AutoSync.SyncedFields.Where(f => f.FieldType == typeof(bool)).Select(f => (bool)f.GetValue(null))
            //.Concat(AutoSync.SyncedProperties.Where(p => p.PropertyType == typeof(bool)).Select(p => (bool)p.GetValue(null)))
            .ToArray();
        ints = AutoSync.SyncedFields.Where(f => f.FieldType == typeof(int)).Select(f => (int)f.GetValue(null))
            //.Concat(AutoSync.SyncedProperties.Where(p => p.PropertyType == typeof(int)).Select(p => (int)p.GetValue(null)))
            .ToArray();
        floats = AutoSync.SyncedFields.Where(f => f.FieldType == typeof(float)).Select(f => (float)f.GetValue(null))
            //.Concat(AutoSync.SyncedProperties.Where(p => p.PropertyType == typeof(float)).Select(p => (float)p.GetValue(null)))
            .ToArray();
        strings = AutoSync.SyncedFields.Where(f => f.FieldType == typeof(string)).Select(f => (string)f.GetValue(null)) //pack fields first
            //.Concat(AutoSync.SyncedProperties.Where(p => p.PropertyType == typeof(string)).Select(p => (string)p.GetValue(null))) //then properties
            .Concat(AutoSync.SyncedConfigs.Select(c => (c.GetValue(null) as ConfigurableBase).BoxedValue.ToString())) //then configs
            .ToArray();
    }

    public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
    {
        int bi = 0, ii = 0, fi = 0, si = 0;
        foreach (FieldInfo f in AutoSync.SyncedFields) //go through all fields
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

        /*foreach (PropertyInfo p in AutoSync.SyncedProperties) //go through all properties WITHOUT resetting the counters
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
        }*/

        foreach (FieldInfo f in AutoSync.SyncedConfigs) //set all configs with remaining data
        {
            (f.GetValue(null) as ConfigurableBase).BoxedValue = strings[si++];
        }
    }
}