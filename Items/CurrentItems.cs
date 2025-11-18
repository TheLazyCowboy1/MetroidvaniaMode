using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using MetroidvaniaMode.Collectibles;
using System;
using System.Collections.Generic;
using static MultiplayerUnlocks;
using UnityEngine;

namespace MetroidvaniaMode.Items;

public static class CurrentItems
{

    public static Dictionary<AbstractPhysicalObject.AbstractObjectType, ItemInfo> ItemInfos = new(new KeyValuePair<AbstractPhysicalObject.AbstractObjectType, ItemInfo>[]
    {
        new(AbstractPhysicalObject.AbstractObjectType.Spear, new(() => CollectibleTokens.SpearItem)),
        new(AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, new(() => CollectibleTokens.GrenadeItem)),
        new(AbstractPhysicalObject.AbstractObjectType.BubbleGrass, new(() => CollectibleTokens.BubbleWeedItem)),
        new(AbstractPhysicalObject.AbstractObjectType.FlareBomb, new(() => CollectibleTokens.FlashbangItem)),
        new(CustomItems.AbHealFruit, new(() => CollectibleTokens.HealFruitItem)),
    });

    public class ItemInfo
    {
        public int count = 0, max = 0;
        public Func<LevelUnlockID[]> Collectible;
        public ItemInfo(Func<LevelUnlockID[]> collectible)
        {
            Collectible = collectible;
        }
    }


    public static void RestockItems()
    {
        foreach (ItemInfo info in ItemInfos.Values)
            info.count = info.max;
    }

    public static void ResetItems(RainWorldGame game)
    {
        try
        {
            if (!game.IsStorySession)
            {
                OptionsItems();
                return;
            }

            BaseItems(game.StoryCharacter);

            if (game.IsStorySession && Abilities.CurrentAbilities.CountCollectibles(game.StoryCharacter))
            {
                WorldSaveData data = game.GetStorySession.saveState.miscWorldSaveData.GetData();

                string[] g = data.UnlockedGoldTokens.Split(';');

                foreach (ItemInfo info in ItemInfos.Values)
                {
                    //ItemInfo info = ItemInfos[i];

                    int oldMax = info.max;
                    info.max += CollectibleTokens.UnlockedCount(g, info.Collectible());
                    info.count += info.max - oldMax; //increase/decrease count with max
                    if (info.count < 0) info.count = 0; //don't go negative, though!

                    //ItemInfos[i] = info;
                }
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void BaseItems(SlugcatStats.Name slugcat)
    {
        if (slugcat == SlugcatStats.Name.White)
        {
            //just reset everything to 0
            foreach (ItemInfo info in ItemInfos.Values)
            {
                info.count = 0;
                info.max = 0;
            }
        }
        else
            OptionsItems();
    }

    private static void OptionsItems()
    {
        if (Options.UnlockAllInventoryItems)
        {
            foreach (ItemInfo info in ItemInfos.Values)
            {
                info.max = info.Collectible().Length; //set everything to the max
            }
        }
    }
}
