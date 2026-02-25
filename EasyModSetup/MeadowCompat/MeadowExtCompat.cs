using RainMeadow;

namespace EasyModSetup.MeadowCompat;

public static class MeadowExtCompat
{
    //public static bool IsLocal(AbstractPhysicalObject apo) => apo.IsLocal();
    public static bool IsOnline(AbstractPhysicalObject apo)
        => OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(apo, out OnlinePhysicalObject opo) && !opo.owner.isMe;
}
