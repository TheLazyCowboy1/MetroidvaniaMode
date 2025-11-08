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
        On.MoreSlugcats.CollectiblesTracker.ctor += CollectiblesTracker_ctor;
    }

    public static void RemoveHooks()
    {
        On.CollectToken.Pop -= CollectToken_Pop;
        On.SaveState.LoadGame -= SaveState_LoadGame;
        On.MoreSlugcats.CollectiblesTracker.ctor -= CollectiblesTracker_ctor;
    }


    //Correctly save when a token is grabbed
    private static void CollectToken_Pop(On.CollectToken.orig_Pop orig, CollectToken self, Player player)
    {
        orig(self, player);

        try
        {
            CollectToken.CollectTokenData data = self.placedObj.data as CollectToken.CollectTokenData;

            if (data.isBlue && Collectibles.AllCollectibles.Contains(data.SandboxUnlock))
            {
                WorldSaveData saveData = self.room.game.GetStorySession.saveState.miscWorldSaveData.GetData();
                if (!saveData.UnlockedBlueTokens.Split(';').Contains(data.SandboxUnlock.ToString()))
                    saveData.UnlockedBlueTokens += data.SandboxUnlock.ToString() + ";";

                Abilities.CurrentAbilities.ResetAbilities(self.room.game);

                self.anythingUnlocked = false;
                self.room.game.cameras[0].hud.textPrompt.AddMessage(
                    RWCustom.Custom.rainWorld.inGameTranslator.Translate("Unlocked new cool ability. I'm not sure what."),
                    20, 160, true, true);
            }
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
                    self.progression.miscProgressionData.sandboxTokens.Add(new(s));
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

    //Make collectibles tracker ONLY show our collectibles
    private static void CollectiblesTracker_ctor(On.MoreSlugcats.CollectiblesTracker.orig_ctor orig, MoreSlugcats.CollectiblesTracker self, Menu.Menu menu, Menu.MenuObject owner, UnityEngine.Vector2 pos, FContainer container, SlugcatStats.Name saveSlot)
    {
        try
        {
            RainWorld rw = menu.manager.rainWorld;
            foreach (string reg in SlugcatStats.SlugcatStoryRegions(saveSlot).Union(SlugcatStats.SlugcatOptionalRegions(saveSlot)))
            {
                string r = reg.ToLowerInvariant();
                if (!rw.regionBlueTokens.ContainsKey(r))
                    continue;
                for (int i = rw.regionBlueTokens[r].Count - 1; i >= 0; i--)
                {
                    if (!Collectibles.AllCollectibles.Contains(rw.regionBlueTokens[r][i]))
                    {
                        rw.regionBlueTokens[r].RemoveAt(i);
                        rw.regionBlueTokensAccessibility[r].RemoveAt(i);
                    }
                }
                for (int i = rw.regionGoldTokens[r].Count - 1; i >= 0; i--)
                {
                    if (!Collectibles.AllCollectibles.Contains(rw.regionGoldTokens[r][i]))
                    {
                        rw.regionGoldTokens[r].RemoveAt(i);
                        rw.regionGoldTokensAccessibility[r].RemoveAt(i);
                    }
                }
                for (int i = rw.regionRedTokens[r].Count - 1; i >= 0; i--)
                {
                    if (!Collectibles.AllCollectibles.Contains(rw.regionRedTokens[r][i]))
                    {
                        rw.regionRedTokens[r].RemoveAt(i);
                        rw.regionRedTokensAccessibility[r].RemoveAt(i);
                    }
                }
                for (int i = rw.regionGreenTokens[r].Count - 1; i >= 0; i--)
                {
                    if (!Collectibles.AllCollectibles.Contains(rw.regionGreenTokens[r][i]))
                    {
                        rw.regionGreenTokens[r].RemoveAt(i);
                        rw.regionGreenTokensAccessibility[r].RemoveAt(i);
                    }
                }

                Plugin.Log("Trimmed down collectibles in collectible tracker");
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, menu, owner, pos, container, saveSlot);
    }
}
