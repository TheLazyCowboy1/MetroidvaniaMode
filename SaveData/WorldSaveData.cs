using MetroidvaniaMode.Tools;
using System;
using System.Collections.Generic;

namespace MetroidvaniaMode.SaveData;

public class WorldSaveData : SaveManager
{
    public static WorldSaveData CurrentInstance;

    public MiscWorldSaveData Data;

    [SaveKey("UnlockedBlueTokens")]
    public StringList UnlockedBlueTokens = new();
    [SaveKey("UnlockedGoldTokens")]
    public StringList UnlockedGoldTokens = new();
    [SaveKey("UnlockedRedTokens")]
    public StringList UnlockedRedTokens = new();
    [SaveKey("UnlockedGreenTokens")]
    public StringList UnlockedGreenTokens = new();

    public IEnumerable<string> CollectibleSplitSaveString => UnlockedBlueTokens + UnlockedGoldTokens + UnlockedGreenTokens + UnlockedRedTokens;

    public WorldSaveData(MiscWorldSaveData data)
    {
        this.Data = data;

        LoadData(data.unrecognizedSaveStrings);
        Plugin.Log("Loaded WorldSaveData. UnlockedRedTokens = " + UnlockedRedTokens);

        CurrentInstance = this;
    }
}
