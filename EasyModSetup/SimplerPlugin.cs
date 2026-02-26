using BepInEx;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EasyModSetup;

[BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("twofour2.rainReloader", BepInDependency.DependencyFlags.SoftDependency)]
public abstract class SimplerPlugin : BaseUnityPlugin
{
    #region VirtualMethods

    /// <summary>
    /// The higher this number is, the more logs will be shown.
    /// </summary>
    public virtual int LogLevel => 1;

    public virtual void Initialize() { }

    public abstract void ApplyHooks();
    public abstract void RemoveHooks();

    public virtual void ModsApplied() { }

    #endregion

    #region PluginData

    public static string MOD_ID = "error";
    public static string MOD_NAME = "error";
    public static string MOD_VERSION = "error";
    public static string PluginPath = "error";
    public static bool RainMeadowEnabled = false;

    public static SimplerPlugin Instance;

    public static OptionInterface ConfigOptions;

    public SimplerPlugin(OptionInterface options) : base()
    {
        Instance = this;
        ConfigOptions = options;

        var data = this.Info.Metadata;
        MOD_ID = data.GUID;
        MOD_NAME = data.Name;
        MOD_VERSION = data.Version.ToString();
        Log("Plugin created");
    }

    #endregion

    #region Setup

    public void Awake()
    {
        EasyExtEnum.Register();
        AutoSync.RegisterSyncedVars();

        Initialize();
        Log("Plugin awoken");
    }

    private bool hooksApplied = false; //a hopefully pointless fail-safe
    public void OnEnable()
    {
        //EasyExtEnum.Register(); //to reflect hot-reload changes?
        //AutoSync.RegisterSyncedVars();

        On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        //for using Rain Reloader (hot mod reloads), since it loads mods AFTER OnModsInit
        try
        {
            if (ModManager.ActiveMods.Any(m => m.id == MOD_ID))
            {
                if (ConfigOptions != null)
                {
                    MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);
                    MachineConnector.ReloadConfig(ConfigOptions);
                }
                SetMeadowEnabled();
                ModsApplied();
                _ApplyHooks();
            }
        }
        catch (Exception ex) { Error(ex); }
    }

    public void OnDisable()
    {
        if (!hooksApplied) return;

        On.RainWorld.OnModsInit -= RainWorld_OnModsInit;
        AutoConfigOptions.RemoveHooks();

        try
        {
            if (RainMeadowEnabled)
            {
                MeadowCompat.EasyResourceState.RemoveHooks();
                MeadowCompat.EasyEntityState.RemoveHooks();
            }
        } catch { }

        RemoveHooks();
        Log("Removed hooks");

        hooksApplied = false;
    }

    private void _ApplyHooks()
    {
        if (hooksApplied) return;

        try
        {
            if (RainMeadowEnabled)
            {
                MeadowCompat.EasyResourceState.ApplyHooks();
                MeadowCompat.EasyEntityState.ApplyHooks();
            }
        }
        catch (Exception ex) { Error("Rain Meadow is apparently inactive: " + ex); RainMeadowEnabled = false; }

        try
        {
            if (ConfigOptions is AutoConfigOptions)
                AutoConfigOptions.ApplyHooks();
        } catch (Exception ex) { Error(ex); }

        ApplyHooks();
        hooksApplied = true;
        Log("Applied hooks");
    }
    private void SetMeadowEnabled()
    {
        RainMeadowEnabled = ModManager.ActiveMods.Any(m => m.id == "henpemaz_rainmeadow");
        Log("Rain Meadow enabled: " + RainMeadowEnabled);
    }


    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        try
        {
            if (ConfigOptions != null)
                MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions); //register config menu

            PluginPath = ModManager.ActiveMods.Find(m => m.id == MOD_ID).path;
        }
        catch (Exception ex) { Error(ex); }

        SetMeadowEnabled();
        ModsApplied();
        _ApplyHooks();
    }

    #endregion

    #region Tools

    public static void Log(object o, int logLevel = 1, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
    {
        if (Instance != null && logLevel <= Instance.LogLevel)
            Instance.Logger.LogDebug(LogText(o, file, name, line));
    }

    public static void Error(object o, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
        => Instance?.Logger.LogError(LogText(o, file, name, line));

    //private static DateTime PluginStartTime = DateTime.Now;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string LogText(object o, string file, string name, int line)
    {
        try
        {
            return $"[{DateTime.Now.ToString("HH:mm:ss.ffffff")},{Path.GetFileName(file)}.{name}:{line}]: {o}";
        }
        catch (Exception ex)
        {
            Instance?.Logger.LogError(ex);
        }
        return o.ToString();
    }

    #endregion
}
