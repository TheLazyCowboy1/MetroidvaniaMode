using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using System.Linq;
using EasyModSetup;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MetroidvaniaMode;

[BepInDependency("com.dual.improved-input-config", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ddemile.fake_achievements", BepInDependency.DependencyFlags.SoftDependency)]
//[BepInDependency("twofour2.rainReloader", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin("LazyCowboy.MetroidvaniaMode", "Metroidvania Mode", "0.0.10")]
public class Plugin : SimplerPlugin
{

    #region Setup
    public override int LogLevel => Options.LogLevel;

    public Plugin() : base(new Options())
    {
    }
    public override void Initialize()
    {
        try
        {
            //Register ExtEnums
            Collectibles.CollectibleTokens.Register();
        }
        catch (Exception ex) { Error(ex); }
    }

    private bool IsInit = false;
    public override void ModsApplied()
    {
        if (IsInit) return;
        if (ModManager.ActiveMods == null || !ModManager.ActiveMods.Any(m => m.id == MOD_ID)) return; //this mod MUST be loaded
        IsInit = true; //set IsInit first, in case there is an error

        ImprovedInputEnabled = ModManager.ActiveMods.Any(m => m.id == "improved-input-config");
        FakeAchievementsEnabled = ModManager.ActiveMods.Any(m => m.id == "ddemile.fake_achievements");

        //Load assets
        Tools.Assets.Load();

        Log($"Initialized MetroidvaniaMode config and assets. Mods enabled: ImprovedInput {ImprovedInputEnabled}, FakeAchievements {FakeAchievementsEnabled}", 0);
    }


    #endregion

    #region Hooks

    public override void ApplyHooks()
    {
        On.RainWorldGame.ctor += RainWorldGame_ctor;

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

        AI.AIHooks.ApplyHooks();

        UI.Hooks.ApplyHooks();

        VFX.WarpNoiseBloom.ApplyHooks();

        SaveData.Hooks.ApplyHooks();
        Collectibles.Hooks.ApplyHooks();

        Log("Applied hooks", 0);
    }

    public override void RemoveHooks()
    {
        On.RainWorldGame.ctor -= RainWorldGame_ctor;

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

        AI.AIHooks.RemoveHooks();

        UI.Hooks.RemoveHooks();

        VFX.WarpNoiseBloom.RemoveHooks();

        SaveData.Hooks.RemoveHooks();
        Collectibles.Hooks.RemoveHooks();

        Log("Removed hooks", 0);
    }

    //Ensures everything is up to date for when the game starts
    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        //ConfigOptions.SetValues(); //should no longer be necessary, but is here just in case
        WorldChanges.FilePrefixModifier.SetEnabled(manager);
        Tools.Keybinds.GameStarted(); //ensure the keybinds aren't totally unbound or something

        AI.WorldAI.ClearStaticData();

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

}
