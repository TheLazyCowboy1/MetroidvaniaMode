using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.SaveData;

public static class Hooks
{
    public static void ApplyHooks()
    {
        //On.PlayerProgression.SaveWorldStateAndProgression += PlayerProgression_SaveWorldStateAndProgression;
        On.SaveState.BringUpToDate += SaveState_BringUpToDate;
        On.SaveState.LoadGame += SaveState_LoadGame;
    }

    public static void RemoveHooks()
    {
        //On.PlayerProgression.SaveWorldStateAndProgression -= PlayerProgression_SaveWorldStateAndProgression;
        On.SaveState.BringUpToDate -= SaveState_BringUpToDate;
        On.SaveState.LoadGame -= SaveState_LoadGame;
    }



    private static void SaveState_BringUpToDate(On.SaveState.orig_BringUpToDate orig, SaveState self, RainWorldGame game)
    {
        try
        {
            //save WorldSaveData
            if (WorldSaveData.CurrentInstance?.Data == self.miscWorldSaveData)
            {
                self.miscWorldSaveData.unrecognizedSaveStrings.RemoveAll(str => str.StartsWith(WorldSaveData.PREFIX));
                string s = WorldSaveData.CurrentInstance.SaveData();
                self.miscWorldSaveData.unrecognizedSaveStrings.Add(s);
                Plugin.Log("WorldSaveData string: " + s);
            }

        }
        catch (Exception ex) { Plugin.Error(ex); }

        orig(self, game);
    }

    //Clear current save data; new data is being loaded in
    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        WorldSaveData.CurrentInstance = null;

        orig(self, str, game);
    }
}
