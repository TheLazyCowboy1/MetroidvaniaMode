using MonoMod.RuntimeDetour;
using RainMeadow;
using System;

namespace EasyModSetup.MeadowCompat;

/// <summary>
/// Just an event and a hook to Rain Meadow in order to automatically update AutoConfigOptions if it gets changed by the AutoSync.
/// This is a separate file because it references RainMeadow, so it's safer for soft-compatibility to make it a separate file from everything else.
/// </summary>
public static class AutoConfigLobbyHooks
{
    private static Hook LeaveLobbyHook;
    public static void ApplyHooks()
    {
        Lobby.OnNewOwner += Lobby_OnNewOwner;
        LeaveLobbyHook = new(typeof(OnlineManager).GetMethod(nameof(OnlineManager.LeaveLobby)), (Action orig) =>
        {
            orig();
            (SimplerPlugin.ConfigOptions as AutoConfigOptions).SetValues(); //reload configs
        });
    }

    public static void RemoveHooks()
    {
        Lobby.OnNewOwner -= Lobby_OnNewOwner;
    }

    private static void Lobby_OnNewOwner(OnlineResource resource, OnlinePlayer player)
    {
        if (player.isMe && resource is Lobby)
            (SimplerPlugin.ConfigOptions as AutoConfigOptions).SetValues(); //reload configs
    }
}
