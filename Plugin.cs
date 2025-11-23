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
        MOD_VERSION = "0.0.7";


    public static Plugin Instance;
    private static Options ConfigOptions;

    public static string PluginPath = "";

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

        try
        {
            Tools.Keybinds.Bind(); //Improved Input Config wants them bound here for some reason
        }
        catch (Exception ex) { Error(ex); }
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorldOnOnModsInit;
        if (IsInit)
        {
            On.RainWorldGame.ctor -= RainWorldGame_ctor;

            FilePrefixModifier.RemoveHooks();
            ArenaRoomFix.RemoveHooks();

            Abilities.MovementLimiter.RemoveHooks();
            Abilities.Dash.RemoveHooks();
            Abilities.DoubleJump.RemoveHooks();
            Abilities.Health.RemoveHooks();
            Abilities.Glide.RemoveHooks();

            Items.Inventory.RemoveHooks();
            Items.CustomItems.RemoveHooks();

            UI.Hooks.RemoveHooks();

            SaveData.Hooks.RemoveHooks();
            Collectibles.Hooks.RemoveHooks();
            Tools.Keybinds.RemoveHooks();

            //Collectibles.CollectibleTokens.Unregister();

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
            IsInit = true; //set IsInit first, in case there is an error

            //find the plugin path
            PluginPath = ModManager.ActiveMods.Find(m => m.id == MOD_ID).path;

            ImprovedInputEnabled = ModManager.ActiveMods.Any(m => m.id == "improved-input-config");
            FakeAchievementsEnabled = ModManager.ActiveMods.Any(m => m.id == "ddemile.fake_achievements");

            //Register ExtEnums
            Collectibles.CollectibleTokens.Register();
            Items.CustomItems.Register();


            //Set up config menu
            MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);
            ConfigOptions.SetValues();

            //Keep config menu options up to date
            On.RainWorldGame.ctor += RainWorldGame_ctor;

            //APPLY HOOKS
            FilePrefixModifier.ApplyHooks();
            ArenaRoomFix.ApplyHooks();

            Abilities.MovementLimiter.ApplyHooks();
            Abilities.Dash.ApplyHooks();
            Abilities.DoubleJump.ApplyHooks();
            Abilities.Health.ApplyHooks();
            Abilities.Glide.ApplyHooks();

            Items.Inventory.ApplyHooks();
            Items.CustomItems.ApplyHooks();

            UI.Hooks.ApplyHooks();

            SaveData.Hooks.ApplyHooks();
            Collectibles.Hooks.ApplyHooks();
            Tools.Keybinds.ApplyHooks();

            Log($"Applied hooks. Mods enabled: ImprovedInput {ImprovedInputEnabled}, FakeAchievements {FakeAchievementsEnabled}", 0);
        }
        catch (Exception ex)
        {
            Error(ex);
            throw;
        }
    }

    //Ensures config values are up to date when the game starts
    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        ConfigOptions.SetValues();
        FilePrefixModifier.SetEnabled(manager);
        Tools.Keybinds.GameStarted();

        orig(self, manager);

        Abilities.CurrentAbilities.ResetAbilities(self);
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
