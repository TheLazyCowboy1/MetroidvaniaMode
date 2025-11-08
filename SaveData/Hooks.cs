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
        On.PlayerProgression.SaveWorldStateAndProgression += PlayerProgression_SaveWorldStateAndProgression;
        On.SaveState.LoadGame += SaveState_LoadGame;
    }

    public static void RemoveHooks()
    {
        On.PlayerProgression.SaveWorldStateAndProgression -= PlayerProgression_SaveWorldStateAndProgression;
        On.SaveState.LoadGame -= SaveState_LoadGame;
    }


    private static bool PlayerProgression_SaveWorldStateAndProgression(On.PlayerProgression.orig_SaveWorldStateAndProgression orig, PlayerProgression self, bool malnourished)
    {
        try
        {
            //save WorldSaveData
            if (WorldSaveData.CurrentInstance != null && WorldSaveData.CurrentInstance.Data == self.currentSaveState?.miscWorldSaveData)
            {
                self.currentSaveState.miscWorldSaveData.unrecognizedSaveStrings.RemoveAll(str => str.StartsWith(WorldSaveData.PREFIX));
                self.currentSaveState.miscWorldSaveData.unrecognizedSaveStrings.Add(WorldSaveData.CurrentInstance.SaveData());
            }

        } catch (Exception ex) { Plugin.Error(ex); }

        return orig(self, malnourished);
    }

    //Clear current save data; new data is being loaded in
    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        WorldSaveData.CurrentInstance = null;

        orig(self, str, game);
    }
}
