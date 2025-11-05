using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using Watcher;
using BepInEx.Logging;
using SlugBase.SaveData;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using Menu;
using RWCustom;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MetroidvaniaMode;

[BepInDependency("ddemile.fake_achievements", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.MetroidvaniaMode",
        MOD_NAME = "Metroidvania Mode",
        MOD_VERSION = "0.0.1";


    public static Plugin Instance;
    private static Options ConfigOptions;

    #region Setup
    public Plugin()
    {
        try
        {
            Instance = this;
            ConfigOptions = new Options();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorldOnOnModsInit;
        if (IsInit)
        {
            On.RainWorldGame.ctor -= RainWorldGame_ctor;

            On.Player.Jump -= Player_Jump;
            On.Player.WallJump -= Player_WallJump;
            On.Player.UpdateBodyMode -= Player_UpdateBodyMode;
            On.Player.UpdateAnimation -= Player_UpdateAnimation;

            IsInit = false;
        }
    }

    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            On.RainWorldGame.ctor += RainWorldGame_ctor;

            On.Player.Jump += Player_Jump;
            On.Player.WallJump += Player_WallJump;
            On.Player.UpdateBodyMode += Player_UpdateBodyMode;
            On.Player.UpdateAnimation += Player_UpdateAnimation;
            On.Player.MovementUpdate += Player_MovementUpdate;
            //On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
            On.Player.Update += Player_Update;
            
            //Set up config menu
            MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);

            IsInit = true;
            Log("Applied hooks");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        try
        {
            if (!Options.CanUseShortcuts && self.enteringShortCut != null && self.Consious
                && self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.Normal)
            {
                self.enteringShortCut = null; //nope; no using shortcuts for you!
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self, eu);
    }

    //Use the noGrabCounter to prevent grabbing poles entirely
    private void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        try
        {
            if (!Options.CanGrabPoles && self.noGrabCounter <= 0)
                self.noGrabCounter = 1;
        }
        catch (Exception ex) { Error(ex); }

        orig(self, eu);
    }

    //Make the game think that the player isn't pressing up when climbing vertical poles (or hanging underneath a vertical one)
    private void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
    {
        try
        {
            if (!Options.ClimbVerticalPoles
                && (self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                && self.input[0].y > 0)
            {
                self.input[0].y = 0;
                orig(self);
                self.input[0].y = 1;
                return;
            }
        }
        catch (Exception ex) { Error(ex); }

        orig(self);
    }

    //Make the game think that the player isn't pressing up when climbing corridors and running the corridor movement code
    private void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
    {
        try
        {
            if (!Options.ClimbVerticalCorridors && self.bodyMode == Player.BodyModeIndex.CorridorClimb && self.input[0].y > 0
                && !self.IsTileSolid(0, 0, -1) && !self.room.GetTile(self.bodyChunks[1].pos + new Vector2(0,-13)).IsSolid() //can still climb up if there is terrain beneath
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0,10)).shortCutType == ShortcutData.Type.DeadEnd //can climb if shortcut above
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0,-20)).shortCutType == ShortcutData.Type.DeadEnd) //can climb if shortcut below
            {
                self.input[0].y = 0;
                orig(self);
                self.input[0].y = 1;
                return;
            }
        } catch (Exception ex) { Error(ex); }

        orig(self);
    }

    private void Player_WallJump(On.Player.orig_WallJump orig, Player self, int direction)
    {
        if (Options.CanWallJump)
        {
            orig(self, direction);

            if (self.jumpBoost <= 0 && Options.JumpBoost > 1)
                self.jumpBoost += 10f * (Options.JumpBoost - 1);
            else
                self.jumpBoost *= Options.JumpBoost;
        }
    }

    private void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        orig(self);

        if (self.jumpBoost <= 0 && Options.JumpBoost > 1)
            self.jumpBoost += 10f * (Options.JumpBoost - 1);
        else
            self.jumpBoost *= Options.JumpBoost;
    }

    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        ConfigOptions.SetValues();

        orig(self, manager);
    }

    #endregion


    #region Tools

    public static void Log(object o, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
        => Instance.Logger.LogDebug(logText(o, file, name, line));

    public static void Error(object o, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
        => Instance.Logger.LogError(logText(o, file, name, line));

    private static string logText(object o, string file, string name, int line)
    {
        try
        {
            return $"[{Path.GetFileName(file)}.{name}:{line}]: {o}";
        }
        catch (Exception ex)
        {
            Instance.Logger.LogError(ex);
        }
        return o.ToString();
    }

    #endregion

}
