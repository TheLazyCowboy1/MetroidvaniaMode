using RainMeadow;
using System;
using System.Linq;
using System.Reflection;

namespace EasyModSetup.MeadowCompat;

public abstract class EasyResourceState : OnlineResource.ResourceData.ResourceDataState
{
    private static bool HooksApplied = false;
    private static Type[] RegisteredTypes;
    public static void ApplyHooks()
    {
        if (HooksApplied) return;

        RegisteredTypes = Assembly.GetExecutingAssembly().GetTypesSafely().Where(t => t.IsSubclassOf(typeof(EasyResourceState))).ToArray();
        if (RegisteredTypes.Length > 0) //only add if there's a use for it
            OnlineResource.OnAvailable += OnlineResource_OnAvailable;

        HooksApplied = true;
    }
    public static void RemoveHooks()
    {
        if (!HooksApplied) return;

        if (RegisteredTypes.Length > 0)
            OnlineResource.OnAvailable -= OnlineResource_OnAvailable;

        HooksApplied = false;
    }
    private static void OnlineResource_OnAvailable(OnlineResource r)
    {
        foreach (Type t in RegisteredTypes)
        {
            try
            {
                EasyResourceState s = Activator.CreateInstance(t) as EasyResourceState; //this feels so horrible to do
                if (s.AttachTo(r))
                {
                    var d = r.AddData(s.MakeData(r));
                    SimplerPlugin.Log($"Attached data {d} to resource {r}");
                }
            }
            catch (Exception ex) { SimplerPlugin.Error(ex); }
        }
    }
    public abstract bool AttachTo(OnlineResource resource);

    public abstract void WriteTo(OnlineResource.ResourceData data, OnlineResource resource);


    public override Type GetDataType() => typeof(EasyResourceData<>).MakeGenericType(this.GetType());

    private class EasyResourceData<T> : OnlineResource.ResourceData where T : EasyResourceState, new()
    {
        public EasyResourceData() : base() { }
        public EasyResourceData(OnlineResource resource) : base() { } //required functionality

        public override ResourceDataState MakeState(OnlineResource resource)
        {
            T t = new();
            t.WriteTo(this, resource);
            return t;
        }

        public override string ToString() => $"EasyResourceData<{typeof(T).FullName}>";
    }

}