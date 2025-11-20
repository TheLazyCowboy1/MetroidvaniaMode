using MetroidvaniaMode.Tools;
using System;

namespace MetroidvaniaMode.SaveData;

public class DeathSaveData : SimpleSaveData
{
    public static DeathSaveData CurrentInstance;

    public DeathPersistentSaveData Data;

    [SaveKey("WheelItems")]
    public StringList WheelItems = new();

    public DeathSaveData(DeathPersistentSaveData data)
    {
        this.Data = data;

        Load(data.unrecognizedSaveStrings);
        //Plugin.Log("Loaded WorldSaveData. UnlockedRedTokens = " + UnlockedRedTokens);

        CurrentInstance = this;
    }
}
