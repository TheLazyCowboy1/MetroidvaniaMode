using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using System.Runtime.CompilerServices;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MetroidvaniaMode;

[BepInDependency("com.dual.improved-input-config", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ddemile.fake_achievements", BepInDependency.DependencyFlags.SoftDependency)]
//[BepInDependency("twofour2.rainReloader", BepInDependency.DependencyFlags.SoftDependency)]

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
            //MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);
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

            //Init(); //try to load assets here, because Rain Reloader doesn't let us hook OnModsInit or PostModsInit
        }
        catch (Exception ex) { Error(ex); }
    }
    private void OnDisable()
    {
        On.RainWorld.OnModsInit -= RainWorld_OnModsInit;

        RemoveHooks();

        IsInit = false;
    }

    private bool IsInit = false;
    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            Init();
        }
        catch (Exception ex)
        {
            Error(ex);
            throw;
        }
    }
    private void Init()
    {
        if (IsInit) return;
        if (ModManager.ActiveMods == null || !ModManager.ActiveMods.Any(m => m.id == MOD_ID)) return; //this mod MUST be loaded
        IsInit = true; //set IsInit first, in case there is an error

        //find the plugin path
        PluginPath = ModManager.ActiveMods.Find(m => m.id == MOD_ID).path;

        ImprovedInputEnabled = ModManager.ActiveMods.Any(m => m.id == "improved-input-config");
        FakeAchievementsEnabled = ModManager.ActiveMods.Any(m => m.id == "ddemile.fake_achievements");


        //Set up config menu
        MachineConnector.SetRegisteredOI(MOD_ID, ConfigOptions);
        //ConfigOptions.SetValues(); //for good measure; why not...

        //Load assets
        Tools.Assets.Load();

        Log($"Initialized MetroidvaniaMode config and assets. Mods enabled: ImprovedInput {ImprovedInputEnabled}, FakeAchievements {FakeAchievementsEnabled}", 0);
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

            AI.AIHooks.ApplyHooks();

            UI.Hooks.ApplyHooks();

            VFX.WarpNoiseBloom.ApplyHooks();

            SaveData.Hooks.ApplyHooks();
            Collectibles.Hooks.ApplyHooks();

            On.RoomCamera.ApplyPositionChange += RoomCamera_ApplyPositionChange;

            Log("Applied hooks", 0);
        }
        HooksApplied = true;
    }

    private int DestructionLevel = 40;
    private string PoleMapRoom = "";
    private Texture2D PoleMap = null;

    private void RoomCamera_ApplyPositionChange(On.RoomCamera.orig_ApplyPositionChange orig, RoomCamera self)
    {
        orig(self);

        try
        {
            if (Input.GetKey(KeyCode.X)) return; //don't do this if holding x
            if (Input.GetKey(KeyCode.C)) DestructionLevel -= 10;
            if (Input.GetKey(KeyCode.V)) DestructionLevel += 10; //for changing destruction level
            if (Input.GetKey(KeyCode.LeftShift)) DestructionLevel = 40; //reset to default
            Shader.SetGlobalFloat("TheLazyCowboy1_DestructionStrength", DestructionLevel);

            //create pole map
            Room.Tile[,] tiles = self.room.Tiles;
            int tileWidth = tiles.GetLength(0), tileHeight = tiles.GetLength(1);

            if (self.room.abstractRoom.name != PoleMapRoom || PoleMap == null) //don't generate more than is necessary
            {
                PoleMapRoom = self.room.abstractRoom.name;

                //5 pixels per tile, because poles are 4 pixels thick
                int pixelWidth = tileWidth * 5, pixelHeight = tileHeight * 5;
                if (PoleMap == null || PoleMap.width != pixelWidth || PoleMap.height != pixelHeight) //reset only if necessary
                    PoleMap = new(pixelWidth, pixelHeight, TextureFormat.R8, false) { filterMode = 0 }; //ONLY encodes red
                Color[] colors = PoleMap.GetPixels();

                for (int i = 0; i < tileWidth; i++)
                {
                    for (int j = 0; j < tileHeight; j++)
                    {
                        Room.Tile tile = tiles[i, j];
                        //fill this tile with black
                        for (int b = 0; b < 5; b++)
                        {
                            for (int a = 0; a < 5; a++)
                            {
                                colors[(i*5 + a) + pixelWidth*(j*5 + b)].r = (tile.horizontalBeam && b == 2 || tile.verticalBeam && a == 2) ? 1 : 0;
                            }
                        }
                        /*
                        if (tile.horizontalBeam)
                        {
                            int b = 2; //middle y level
                            for (int a = 0; a < 5; a++)
                            {
                                colors[(i*5 + a) + pixelWidth*(j*5 + b)].r = 1;
                            }
                        }
                        if (tile.verticalBeam)
                        {
                            int a = 2; //middle x level
                            for (int b = 0; b < 5; b++)
                            {
                                colors[(i*5 + a) + pixelWidth*(j*5 + b)].r = 1;
                            }
                        }
                        */
                    }
                }
                PoleMap.SetPixels(colors);
                PoleMap.Apply();
                Shader.SetGlobalTexture("TheLazyCowboy1_PoleMap", PoleMap);

                File.WriteAllBytes(AssetManager.ResolveFilePath("testPoleMap.png"), PoleMap.EncodeToPNG()); //as a temporary debug measure
            }

            //poleMap.xy = i.uv * map.zw + map.xy
            Vector2 camPos = self.CamPos(self.currentCameraPosition);
            //int w = self.levelTexture.width, h = self.levelTexture.height;
            float w = tileWidth * 20, h = tileHeight * 20;
            Vector4 poleMapPos = new(camPos.x / w, camPos.y / h, self.levelTexture.width / w, self.levelTexture.height / h);
            Shader.SetGlobalVector("TheLazyCowboy1_PoleMapPos", poleMapPos);

            /*
            int imgWidth = self.levelTexture.width, imgHeight = self.levelTexture.height;
            Texture2D poleMap = new(imgWidth, imgHeight, TextureFormat.R8, false) { filterMode = 0 }; //ONLY encodes red
            Color[] imgData = poleMap.GetPixels();
            for (int i = 0; i < imgData.Length; i++) imgData[i] = new(0, 0, 0);
            poleMap.SetPixels(imgData); //turn the texture black. this is probably horribly inefficient or something

            Vector2 camPos = self.CamPos(self.currentCameraPosition);
            int xOff = -Mathf.FloorToInt(camPos.x), yOff = -Mathf.FloorToInt(camPos.y);
            camPos *= 0.05f; //convert to tiles

            int minX = Mathf.Max(0, Mathf.FloorToInt(camPos.x)), minY = Mathf.Max(0, Mathf.FloorToInt(camPos.y));
            int maxX = Mathf.Min(self.room.Tiles.GetLength(0)-1, Mathf.CeilToInt(camPos.x + imgWidth*0.05f)),
                maxY = Mathf.Min(self.room.Tiles.GetLength(1)-1, Mathf.CeilToInt(camPos.y + imgHeight*0.05f)); //1400x800 / 20 = 70x40
            //int xOff = -Mathf.FloorToInt(20 * (camPos.x - minX)), yOff = -Mathf.FloorToInt(20 * (camPos.y - minY)); //this is too confusing to explain
            for (int i = minX; i <= maxX; i++)
            {
                for (int j = minY; j <= maxY; j++)
                {
                    Room.Tile tile = self.room.Tiles[i, j];
                    if (tile.horizontalBeam)
                    {
                        int posX = i * 20 + xOff, posY = j * 20 + yOff;
                        int fromX = Mathf.Max(0, posX + 0), fromY = Mathf.Max(0, posY + 8);
                        const int width = 20, height = 4; //poles are 4 pixels thick, right...?
                        Color[] colors = new Color[width * height];
                        for (int k = 0; k < colors.Length; k++) colors[k] = new(1, 0, 0); //red
                        poleMap.SetPixels(fromX, fromY, width, height, colors);
                    }
                    if (tile.verticalBeam)
                    {
                        int posX = i * 20 + xOff, posY = j * 20 + yOff;
                        int fromX = Mathf.Max(0, posX + 8), fromY = Mathf.Max(0, posY + 0);
                        const int width = 4, height = 20; //poles are 4 pixels thick, right...?
                        Color[] colors = new Color[width * height];
                        for (int k = 0; k < colors.Length; k++) colors[k] = new(1, 0, 0); //red
                        poleMap.SetPixels(fromX, fromY, width, height, colors);
                    }
                }
            }
            poleMap.Apply();
            Shader.SetGlobalTexture("TheLazyCowboy1_PoleMap", poleMap);

            File.WriteAllBytes(AssetManager.ResolveFilePath("testPoleMap.png"), poleMap.EncodeToPNG()); //as a temporary debug measure
            */

            CommandBuffer buff = new();
            RenderTexture tempTex = new(self.levelTexture.width, self.levelTexture.height, 0, DefaultFormat.LDR) { filterMode = 0 };
            //RenderTexture tempTex2 = new(self.levelTexture.width, self.levelTexture.height, 0, DefaultFormat.LDR) { filterMode = 0 };
            buff.Blit(self.levelTexture, tempTex, Tools.Assets.DestructionMat); //no longer transparent
            buff.CopyTexture(tempTex, self.levelTexture);
            //buff.Blit(tempTex, tempTex2, Tools.Assets.DestructionPilerMat);
            //buff.CopyTexture(tempTex2, self.levelTexture);
            Graphics.ExecuteCommandBufferAsync(buff, ComputeQueueType.Default); //make this process async!
        }
        catch (Exception ex) { Error(ex); }
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

            AI.AIHooks.RemoveHooks();

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
