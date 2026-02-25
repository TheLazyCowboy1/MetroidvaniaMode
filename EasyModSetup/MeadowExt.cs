using System;
using System.Runtime.CompilerServices;

namespace EasyModSetup;

public static class MeadowExt
{
    public static bool MeadowEnabled = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLocal(this AbstractPhysicalObject apo)
    {
        if (!MeadowEnabled) return true; //don't even try if it won't work
        try
        {
            return MeadowCompat.MeadowExtCompat.IsLocal(apo);
        } catch (Exception ex)
        {
            SimplerPlugin.Error(ex);
            MeadowEnabled = false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLocal(this PhysicalObject obj) => IsLocal(obj.abstractPhysicalObject);
}
