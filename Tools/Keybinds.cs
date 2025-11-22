using ImprovedInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Tools;

public static class Keybinds
{
    public static PlayerKeybind Dash;

    public static void Bind()
    {
        Dash = PlayerKeybind.Register("Dash", Plugin.MOD_ID, "Dash", UnityEngine.KeyCode.D, UnityEngine.KeyCode.Joystick1Button12);
    }
}
