using System;
using System.Runtime.CompilerServices;

namespace MetroidvaniaMode;

public class PlayerInfo
{
    public bool DashCooldown = false;

    public bool ReleaseQueued = false;
    public int iFrames = 0;


    private static ConditionalWeakTable<Player, PlayerInfo> playerInfos = new();
    public static PlayerInfo GetPlayerInfo(Player player) => playerInfos.GetValue(player, p => new PlayerInfo());
    //public static void Clear() => playerInfos. //I would like a clear function, but CWTs don't seem to have that. Memory leaks can't be that bad, right?
}

public static class PlayerInfoExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PlayerInfo GetInfo(this Player player) => PlayerInfo.GetPlayerInfo(player);
}