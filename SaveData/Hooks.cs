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
        //On.SaveState.BringUpToDate += SaveState_BringUpToDate;
        On.PlayerProgression.SaveToDisk += PlayerProgression_SaveToDisk;
        On.PlayerProgression.SaveDeathPersistentDataOfCurrentState += PlayerProgression_SaveDeathPersistentDataOfCurrentState;
        On.SaveState.LoadGame += SaveState_LoadGame;
    }

    public static void RemoveHooks()
    {
        //On.PlayerProgression.SaveWorldStateAndProgression -= PlayerProgression_SaveWorldStateAndProgression;
        //On.SaveState.BringUpToDate -= SaveState_BringUpToDate;
        On.PlayerProgression.SaveToDisk -= PlayerProgression_SaveToDisk;
        On.PlayerProgression.SaveDeathPersistentDataOfCurrentState -= PlayerProgression_SaveDeathPersistentDataOfCurrentState;
        On.SaveState.LoadGame -= SaveState_LoadGame;
    }


    //Save under normal circumstances
    private static bool PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
    {
        try
        {
            SaveState save = self.currentSaveState;
            if (save != null)
            {
                if (saveCurrentState)
                {
                    //save WorldSaveData
                    if (WorldSaveData.CurrentInstance?.Data == save.miscWorldSaveData)
                    {
                        save.miscWorldSaveData.unrecognizedSaveStrings.RemoveAll(str => str.StartsWith(WorldSaveData.PREFIX));
                        string s = WorldSaveData.CurrentInstance.Save();
                        save.miscWorldSaveData.unrecognizedSaveStrings.Add(s);
                        Plugin.Log("WorldSaveData string: " + s);
                    }
                }

                //save DeathSaveData
                SaveDeathPersistentData(save);
            }

        }
        catch (Exception ex) { Plugin.Error(ex); }

        return orig(self, saveCurrentState, saveMaps, saveMiscProg);
    }

    //Ensure deathPersistentData gets saved
    private static void PlayerProgression_SaveDeathPersistentDataOfCurrentState(On.PlayerProgression.orig_SaveDeathPersistentDataOfCurrentState orig, PlayerProgression self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
    {
        try
        {
            if (self.currentSaveState != null)
                SaveDeathPersistentData(self.currentSaveState);

        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
    }

    private static void SaveDeathPersistentData(SaveState save)
    {
        if (DeathSaveData.CurrentInstance?.Data == save.deathPersistentSaveData)
        {
            save.deathPersistentSaveData.unrecognizedSaveStrings.RemoveAll(str => str.StartsWith(DeathSaveData.PREFIX));
            string s = DeathSaveData.CurrentInstance.Save();
            save.deathPersistentSaveData.unrecognizedSaveStrings.Add(s);
            Plugin.Log("DeathSaveData string: " + s);
        }
    }


    //Clear current save data; new data is being loaded in
    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        WorldSaveData.CurrentInstance = null;
        DeathSaveData.CurrentInstance = null;

        orig(self, str, game);
    }
}
