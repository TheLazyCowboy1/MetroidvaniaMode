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

            MovementLimiter.RemoveHooks();

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

            //Keep config menu options up to date
            On.RainWorldGame.ctor += RainWorldGame_ctor;

            MovementLimiter.ApplyHooks();

            
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

    //Ensures config values are up to date when the game starts
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
