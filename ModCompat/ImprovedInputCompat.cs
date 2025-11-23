using ImprovedInput;
using UnityEngine;

namespace MetroidvaniaMode.ModCompat;

public static class ImprovedInputCompat
{
    public static void Register(string id, string name, KeyCode keyboardDefault, KeyCode controllerDefault) => PlayerKeybind.Register(id, Plugin.MOD_ID, name, keyboardDefault, controllerDefault);

    public static bool IsDown(string id, int playerNum) => PlayerKeybind.Get(id).CheckRawPressed(playerNum);
}
