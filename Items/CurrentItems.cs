using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using MetroidvaniaMode.Collectibles;
using System;
using System.Collections.Generic;
using static MultiplayerUnlocks;
using System.Linq;

namespace MetroidvaniaMode.Items;

public static class CurrentItems
{
    //which items are in the item wheel, in order
    public static AbstractPhysicalObject.AbstractObjectType[] WheelItems = new AbstractPhysicalObject.AbstractObjectType[8];

    //counts of each item in the inventory, regardless of whether it is in the wheel
    public static Dictionary<AbstractPhysicalObject.AbstractObjectType, ItemInfo> ItemInfos = new(new KeyValuePair<AbstractPhysicalObject.AbstractObjectType, ItemInfo>[]
    {
        new(AbstractPhysicalObject.AbstractObjectType.Spear, new(() => CollectibleTokens.SpearItem)),
        new(AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, new(() => CollectibleTokens.GrenadeItem)),
        new(AbstractPhysicalObject.AbstractObjectType.BubbleGrass, new(() => CollectibleTokens.BubbleWeedItem)),
        new(AbstractPhysicalObject.AbstractObjectType.FlareBomb, new(() => CollectibleTokens.FlashbangItem)),
        new(CustomItems.HealFruit, new(() => CollectibleTokens.HealFruitItem)),
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

    public static void ResetItems(RainWorldGame game, bool resetToBase = true)
    {
        try
        {
            if (!game.IsStorySession)
            {
                OptionsItems();
                return;
            }

            if (resetToBase)
                BaseItems(game.StoryCharacter);

            if (game.IsStorySession && Abilities.CurrentAbilities.CountCollectibles(game.StoryCharacter))
            {
                //set wheel items
                DeathSaveData deathData = game.GetStorySession.saveState.deathPersistentSaveData.GetData();
                for (int i = 0; i < deathData.WheelItems.Array.Length; i++)
                {
                    string s = deathData.WheelItems.Get(i);
                    if (s == null || s.Length < 1)
                        WheelItems[i] = null;
                    else
                        WheelItems[i] = new(s);
                }

                //set what is unlocked
                WorldSaveData worldData = game.GetStorySession.saveState.miscWorldSaveData.GetData();

                foreach (var kvp in ItemInfos)
                {
                    ItemInfo info = kvp.Value;
                    int oldMax = info.max;
                    info.max = CollectibleTokens.UnlockedCount(worldData.UnlockedGoldTokens, info.Collectible());
                    if (resetToBase) info.max += oldMax; //if we reset to base, then maybe we wanted max to start above 0

                    info.count += info.max - oldMax; //increase/decrease count with max
                    if (info.count < 0) info.count = 0; //don't go negative, though!

                    //add to wheel...?
                    if (!resetToBase && oldMax < info.max && !WheelItems.Contains(kvp.Key))
                    {
                        for (int i = 0; i < WheelItems.Length; i++)
                        {
                            if (WheelItems[i] == null) //set the first null WheelItem to the unlocked item
                            {
                                WheelItems[i] = kvp.Key;
                                break;
                            }
                        }
                    }
                }

                //save any changes to WheelItems
                for (int i = 0; i < WheelItems.Length; i++)
                    deathData.WheelItems.Set(WheelItems[i]?.ToString(), i);
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
        //TEMPORARILY SET WHEEL ITEMS
        WheelItems[0] = AbstractPhysicalObject.AbstractObjectType.Spear;
        WheelItems[1] = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;
        WheelItems[2] = AbstractPhysicalObject.AbstractObjectType.BubbleGrass;
        WheelItems[3] = AbstractPhysicalObject.AbstractObjectType.FlareBomb;
        WheelItems[7] = CustomItems.HealFruit;

        if (Options.UnlockAllInventoryItems)
        {
            foreach (ItemInfo info in ItemInfos.Values)
            {
                info.max = info.Collectible().Length; //set everything to the max
            }
        }
    }
}
