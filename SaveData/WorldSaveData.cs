using MetroidvaniaMode.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.SaveData;

public class WorldSaveData : SaveManager
{
    public static WorldSaveData CurrentInstance;

    public MiscWorldSaveData Data;

    [SaveKey("UnlockedBlueTokens")]
    public string UnlockedBlueTokens = "";
    [SaveKey("UnlockedGoldTokens")]
    public string UnlockedGoldTokens = "";
    [SaveKey("UnlockedRedTokens")]
    public string UnlockedRedTokens = "";
    [SaveKey("UnlockedGreenTokens")]
    public string UnlockedGreenTokens = "";

    public string[] CollectibleSplitSaveString => (UnlockedBlueTokens + UnlockedGoldTokens + UnlockedGreenTokens + UnlockedRedTokens).Split(';');

    public WorldSaveData(MiscWorldSaveData data)
    {
        this.Data = data;

        LoadData(data.unrecognizedSaveStrings);
        Plugin.Log("Loaded WorldSaveData. UnlockedBlueTokens = " + UnlockedBlueTokens);

        CurrentInstance = this;
    }
}
