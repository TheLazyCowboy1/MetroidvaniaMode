using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Linq;
using System.Reflection;

namespace EasyModSetup.MeadowCompat;

public abstract class EasyEntityState : OnlineEntity.EntityData.EntityDataState
{
    private static bool HooksApplied = false;
    private static Type[] RegisteredTypes;
    private static Hook EntityHook;
    public static void ApplyHooks()
    {
        if (HooksApplied) return;

        RegisteredTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(EasyEntityState))).ToArray();
        if (RegisteredTypes.Length > 0) //only add if there's a use for it
            EntityHook = new(typeof(OnlineGameMode).GetMethod(nameof(OnlineGameMode.NewEntity)), OnlineEntity_OnAvailable);

        HooksApplied = true;
    }
    public static void RemoveHooks()
    {
        if (!HooksApplied) return;

        EntityHook?.Undo();

        HooksApplied = false;
    }
    private static void OnlineEntity_OnAvailable(Action<OnlineGameMode, OnlineEntity, OnlineResource> orig, OnlineGameMode self, OnlineEntity e, OnlineResource r)
    {
        orig(self, e, r);
        foreach (Type t in RegisteredTypes)
        {
            try
            {
                EasyEntityState s = Activator.CreateInstance(t) as EasyEntityState; //this feels so horrible to do
                if (s.AttachTo(e))
                {
                    var d = e.AddData(s.MakeData(e));
                    SimplerPlugin.Log($"Attached data {d} to entity {e}");
                }
            }
            catch (Exception ex) { SimplerPlugin.Error(ex); }
        }
    }
    public abstract bool AttachTo(OnlineEntity entity);

    public abstract void WriteTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity);


    public override Type GetDataType() => typeof(EasyEntityData<>).MakeGenericType(this.GetType());

    private class EasyEntityData<T> : OnlineEntity.EntityData where T : EasyEntityState, new()
    {
        public EasyEntityData() : base() { }
        public EasyEntityData(OnlineEntity entity) : base() { } //required functionality

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            T t = new();
            t.WriteTo(this, entity);
            return t;
        }

        public override string ToString() => $"EasyEntityData<{typeof(T).FullName}>";
    }

}