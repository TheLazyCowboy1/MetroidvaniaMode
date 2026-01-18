using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MetroidvaniaMode;

[BepInDependency("com.dual.improved-input-config", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ddemile.fake_achievements", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.MetroidvaniaMode",
        MOD_NAME = "Metroidvania Mode",
        MOD_VERSION = "0.0.10";


    public static Plugin Instance;
    private static Options ConfigOptions;

    public static string PluginPath = "";

    #region Setup
    public Plugin()
    {
    }
    private void OnEnable()
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

        try
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;

            ApplyHooks();
        }
        catch (Exception ex) { Error(ex); }

        try
        {
            //Register ExtEnums
            Collectibles.CollectibleTokens.Register();
            Tools.EasyExtEnum.Register();

            //Bind keybinds
            Tools.Keybinds.Bind(); //Improved Input Config wants them bound here for some reason
        }
        catch (Exception ex) { Error(ex); }
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorld_OnModsInit;

        RemoveHooks();

        IsInit = false;
    }

    private bool IsInit;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;
            IsInit = true; //set IsInit first, in case there is an error

            //find the plugin path
            PluginPath = ModManager.ActiveMods.Find(m => m.id == MOD_ID).path;

            ImprovedInputEnabled = ModManager.ActiveMods.Any(m => m.id == "improved-input-config");
            FakeAchievementsEnabled = ModManager.ActiveMods.Any(m => m.id == "ddemile.fake_achievements");


            //Set up config menu
            MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);
            //ConfigOptions.SetValues();

            Tools.Assets.Load();

            Log($"Initialized MetroidvaniaMode config and assets. Mods enabled: ImprovedInput {ImprovedInputEnabled}, FakeAchievements {FakeAchievementsEnabled}", 0);
        }
        catch (Exception ex)
        {
            Error(ex);
            throw;
        }
    }

    #endregion

    #region Hooks

    private bool HooksApplied = false;
    public void ApplyHooks()
    {
        if (!HooksApplied)
        {
            //APPLY HOOKS

            //Keep config menu options up to date
            On.RainWorldGame.ctor += RainWorldGame_ctor;

            Tools.AutoConfigOptions.ApplyHooks();

            WorldChanges.FilePrefixModifier.ApplyHooks();
            WorldChanges.ArenaRoomFix.ApplyHooks();

            Abilities.MovementLimiter.ApplyHooks();
            Abilities.Dash.ApplyHooks();
            Abilities.DoubleJump.ApplyHooks();
            Abilities.Health.ApplyHooks();
            Abilities.Glide.ApplyHooks();
            Abilities.Shield.ApplyHooks();
            Abilities.StatAbilities.ApplyHooks();

            Items.CustomItems.ApplyHooks();
            Items.Inventory.ApplyHooks();

            Creatures.CustomCreatures.ApplyHooks();

            UI.Hooks.ApplyHooks();

            VFX.WarpNoiseBloom.ApplyHooks();

            SaveData.Hooks.ApplyHooks();
            Collectibles.Hooks.ApplyHooks();

            Log("Applied hooks", 0);
        }
        HooksApplied = true;
    }

    public void RemoveHooks()
    {
        if (HooksApplied)
        {
            On.RainWorldGame.ctor -= RainWorldGame_ctor;

            Tools.AutoConfigOptions.RemoveHooks();

            WorldChanges.FilePrefixModifier.RemoveHooks();
            WorldChanges.ArenaRoomFix.RemoveHooks();

            Abilities.MovementLimiter.RemoveHooks();
            Abilities.Dash.RemoveHooks();
            Abilities.DoubleJump.RemoveHooks();
            Abilities.Health.RemoveHooks();
            Abilities.Glide.RemoveHooks();
            Abilities.Shield.RemoveHooks();
            Abilities.StatAbilities.RemoveHooks();

            Items.CustomItems.RemoveHooks();
            Items.Inventory.RemoveHooks();

            Creatures.CustomCreatures.RemoveHooks();

            UI.Hooks.RemoveHooks();

            VFX.WarpNoiseBloom.RemoveHooks();

            SaveData.Hooks.RemoveHooks();
            Collectibles.Hooks.RemoveHooks();

            Log("Removed hooks", 0);
        }
        HooksApplied = false;
    }

    //Ensures everything is up to date for when the game starts
    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        ConfigOptions.SetValues(); //should no longer be necessary, but is here just in case
        WorldChanges.FilePrefixModifier.SetEnabled(manager);
        Tools.Keybinds.GameStarted(); //ensure the keybinds aren't totally unbound or something

        orig(self, manager);

        Abilities.CurrentAbilities.ResetAbilities(self);
        Abilities.StatAbilities.ApplyStaticStats();
        Items.CurrentItems.ResetItems(self);
        Items.CurrentItems.RestockItems();
    }

    #endregion


    #region ModCompat
    public static bool ImprovedInputEnabled = false;
    public static bool FakeAchievementsEnabled = false;
    #endregion


    #region Tools

    public static void Log(object o, int logLevel = 1, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
    {
        if (logLevel <= Options.LogLevel)
            Instance.Logger.LogDebug(logText(o, file, name, line));
    }

    public static void Error(object o, [CallerFilePath] string file = "", [CallerMemberName] string name = "", [CallerLineNumber] int line = -1)
        => Instance.Logger.LogError(logText(o, file, name, line));

    private static DateTime PluginStartTime = DateTime.Now;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string logText(object o, string file, string name, int line)
    {
        try
        {
            return $"[{DateTime.Now.Subtract(PluginStartTime)},{Path.GetFileName(file)}.{name}:{line}]: {o}";
        }
        catch (Exception ex)
        {
            Instance.Logger.LogError(ex);
        }
        return o.ToString();
    }

    #endregion

}
