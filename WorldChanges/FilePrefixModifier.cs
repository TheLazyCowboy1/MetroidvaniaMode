using System;
using System.IO;

namespace MetroidvaniaMode.WorldChanges;

public static class FilePrefixModifier
{
    public static void SetEnabled(ProcessManager manager)
    {
        try
        {
            if (manager.arenaSitting != null)
                Enabled = false;
            else
            {
                SlugcatStats.Name slugcat = manager.rainWorld.progression.PlayingAsSlugcat;
                Enabled = slugcat == SlugcatStats.Name.White;
            }
        } catch (Exception ex) { Plugin.Error(ex); Enabled = false; }
    }

    public static bool Enabled = false;
    public static string PREFIX = "MVM-";

    public static void ApplyHooks()
    {
        On.AssetManager.ResolveFilePath_string_bool_bool += AssetManager_ResolveFilePath;
        On.RoomSettings.Save += RoomSettings_Save;
        On.RoomSettings.Save_Timeline += RoomSettings_Save_Timeline;
    }

    public static void RemoveHooks()
    {
        On.AssetManager.ResolveFilePath_string_bool_bool -= AssetManager_ResolveFilePath;
        On.RoomSettings.Save -= RoomSettings_Save;
        On.RoomSettings.Save_Timeline -= RoomSettings_Save_Timeline;
    }


    //First try to find a file with PREFIX. If not found, then default behavior
    private static string AssetManager_ResolveFilePath(On.AssetManager.orig_ResolveFilePath_string_bool_bool orig, string path, bool skipMergedMods, bool skipConsoleFiles)
    {
        try
        {
            if (Enabled)
            {
                //add PREFIX to file name
                int idx = path.LastIndexOfAny(new char[] { '/', Path.DirectorySeparatorChar });
                if (idx >= 0)
                {
                    string path2 = orig(path.Insert(idx + 1, PREFIX), skipMergedMods, skipConsoleFiles); //find a file with PREFIX
                    Plugin.Log("Looking for file: " + path2, 3);
                    if (File.Exists(path2))
                        return path2; //return this file, if it actually exists
                }
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        return orig(path, skipMergedMods, skipConsoleFiles);
    }

    
    //Manipulate saving room settings to always save in the mod folder
    private static void RoomSettings_Save(On.RoomSettings.orig_Save orig, RoomSettings self)
    {
        if (Enabled)
        {
            string path = Path.Combine(Plugin.PluginPath, "world", $"{self.room.world.name}-rooms", $"{PREFIX}{self.name}_settings.txt");
            Plugin.Log("Saving RoomSettings: " + path, 2);
            self.Save(path, false);
        }
        else
            orig(self);
    }
    private static void RoomSettings_Save_Timeline(On.RoomSettings.orig_Save_Timeline orig, RoomSettings self, SlugcatStats.Timeline time)
    {
        if (Enabled)
        {
            string path = Path.Combine(Plugin.PluginPath, "world", $"{self.room.world.name}-rooms", $"{PREFIX}{self.name}_settings-{time.value}.txt");
            Plugin.Log("Saving specific RoomSettings: " + path, 2);
            self.Save(path, false);
        }
        else
            orig(self, time);
    }

}
