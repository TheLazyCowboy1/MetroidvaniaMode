using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Collectibles;

public static class Hooks
{
    public static void ApplyHooks()
    {
        On.CollectToken.Pop += CollectToken_Pop;
        On.SaveState.LoadGame += SaveState_LoadGame;
    }

    public static void RemoveHooks()
    {
        On.CollectToken.Pop -= CollectToken_Pop;
        On.SaveState.LoadGame -= SaveState_LoadGame;
    }


    //Correctly save when a token is grabbed
    private static void CollectToken_Pop(On.CollectToken.orig_Pop orig, CollectToken self, Player player)
    {
        orig(self, player);

        try
        {
            WorldSaveData saveData = self.room.game.GetStorySession.saveState.miscWorldSaveData.GetData();
            CollectToken.CollectTokenData data = self.placedObj.data as CollectToken.CollectTokenData;

            if (data.isBlue)
            {
                if (!saveData.UnlockedBlueTokens.Split(';').Contains(data.SandboxUnlock.ToString()))
                    saveData.UnlockedBlueTokens += data.SandboxUnlock.ToString() + ";";
            }

            Abilities.CurrentAbilities.ResetAbilities(self.room.game);
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

    //Correctly load in which tokens are actually unlocked
    private static void SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
    {
        orig(self, str, game);

        try
        {
            //clear out any of my unlocks that shouldn't be there yet
            Collectibles.FixProgressionData(self.progression.miscProgressionData);

            WorldSaveData data = self.miscWorldSaveData.GetData();

            //Set blues
            foreach (string s in data.UnlockedBlueTokens.Split(';'))
            {
                if (s.Length > 0)
                    self.progression.miscProgressionData.levelTokens.Add(new(s));
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
