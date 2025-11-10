using System;
using System.IO;

namespace MetroidvaniaMode;

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
    }

    public static void RemoveHooks()
    {
        On.AssetManager.ResolveFilePath_string_bool_bool -= AssetManager_ResolveFilePath;
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
}
