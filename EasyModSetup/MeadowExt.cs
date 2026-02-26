using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EasyModSetup;

public static class MeadowExt
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnline(this AbstractPhysicalObject apo)
    {
        if (!SimplerPlugin.RainMeadowEnabled) return false; //don't even try if it won't work
        try
        {
            return MeadowCompat.MeadowExtCompat.IsOnline(apo);
        }
        catch (Exception ex) { SimplerPlugin.Error(ex); }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnline(this PhysicalObject obj) => IsOnline(obj.abstractPhysicalObject);


    //Stolen from Rain Meadow. Credits go to whoever wrote it there
    public static Type[] GetTypesSafely(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e) // happens often with soft-dependencies, did you know
        {
            return e.Types.Where(x => x != null).ToArray();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldInfo[] GetStaticFieldsSafely(this Type type) //the key is BindingFlags.DeclaredOnly
        => type.GetFields(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PropertyInfo[] GetStaticPropertiesSafely(this Type type)
        => type.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

}
