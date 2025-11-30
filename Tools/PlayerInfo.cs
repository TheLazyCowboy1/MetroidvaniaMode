using System.Runtime.CompilerServices;

namespace MetroidvaniaMode;

public class PlayerInfo
{
    //public bool DashedSincePress = false;
    public bool DashHeld = false;
    public int WantToDash = 0;
    public int DashesLeft = 0;
    public int DashCooldown = 0;

    public int ExtraJumpsLeft = 0;

    public bool Gliding = false;

    public float ShieldStrength = 0;
    public float ShieldCounter = 0;
    public Abilities.Shield.ShieldSprite Shield = null;

    public bool ReleaseQueued = false;
    public int iFrames = 0;
    public int maxIFrames = 0;

    public int HoldGrabTime = 0;
    public UI.InventoryWheel InventoryWheel = null;


    private static ConditionalWeakTable<Player, PlayerInfo> playerInfos = new();
    public static PlayerInfo GetPlayerInfo(Player player) => playerInfos.GetValue(player, p => new PlayerInfo());
    //public static void Clear() => playerInfos. //I would like a clear function, but CWTs don't seem to have that. Memory leaks can't be that bad, right?
}

public static class PlayerInfoExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlayerInfo GetInfo(this Player player) => PlayerInfo.GetPlayerInfo(player);
}