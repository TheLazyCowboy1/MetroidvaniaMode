using System;
using System.Runtime.CompilerServices;

namespace EasyModSetup;

public static class MeadowExt
{
    public static bool MeadowEnabled = true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnline(this AbstractPhysicalObject apo)
    {
        if (!MeadowEnabled) return false; //don't even try if it won't work
        try
        {
            return MeadowCompat.MeadowExtCompat.IsOnline(apo);
        } catch (Exception ex)
        {
            SimplerPlugin.Error(ex);
            MeadowEnabled = false;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnline(this PhysicalObject obj) => IsOnline(obj.abstractPhysicalObject);
}
