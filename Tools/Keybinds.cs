using MetroidvaniaMode.ModCompat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode.Tools;

public static class Keybinds
{
    public const string DASH_ID = "MVM_Dash",
        SHIELD_ID = "MVM_Shield";

    private static string[] ids = new string[] { DASH_ID, SHIELD_ID };

    public const int LEFT_TRIGGER_AXIS = 6;

    private static Dictionary<string, KeyCode[]> idToKeyCode = new(ids.Length);

    private static Dictionary<int, int> axisToAction = new(new KeyValuePair<int, int>[]
    { //0 and 1 are left stick horizontal/vertical
        //new(2, RewiredConsts.Action.UIHorizontal), //right stick left/right
        //new(3, RewiredConsts.Action.UIVertical), //right up up/down
        new(6, RewiredConsts.Action.UICheatHoldLeft) //left trigger
        //new(5, RewiredConsts.Action.UICheatHoldRight) //right trigger
    });


    public static void Bind()
    {
        //if (Plugin.ImprovedInputEnabled) //this doesn't even work, since we don't know if it's enabled yet!
        try
        {
            ImprovedInputCompat.Register(DASH_ID, "Dash", KeyCode.D, KeyCode.JoystickButton4);
            ImprovedInputCompat.Register(SHIELD_ID, "Shield", KeyCode.S, KeyCode.None);
            Plugin.Log("Successfully bound keybinds with Improved Input Config!");
        } catch { }
    }


    private static KeyCode IdToKeyCode(string id)
    {
        return id switch
        {
            DASH_ID => Options.DashKeyCode,
            SHIELD_ID => Options.ShieldKeyCode,
            _ => KeyCode.None,
        };
    }
    private static KeyCode IdToControllerKeyCode(string id)
    {
        return id switch
        {
            DASH_ID => Options.DashControllerKeyCode,
            _ => KeyCode.None,
        };
    }

    private static string AxisToId(int axis)
    {
        return axis switch
        {
            LEFT_TRIGGER_AXIS => SHIELD_ID,
            _ => null
        };
    }


    public static void ApplyHooks()
    {
        if (Plugin.ImprovedInputEnabled) //don't bother adding the hook if it's not needed
            On.Options.ControlSetup.UpdateActiveController_Controller_int_bool += ControlSetup_UpdateActiveController_Controller_int_bool;
    }

    public static void RemoveHooks()
    {
        if (Plugin.ImprovedInputEnabled)
            On.Options.ControlSetup.UpdateActiveController_Controller_int_bool -= ControlSetup_UpdateActiveController_Controller_int_bool;
    }

    private static void ControlSetup_UpdateActiveController_Controller_int_bool(On.Options.ControlSetup.orig_UpdateActiveController_Controller_int_bool orig, global::Options.ControlSetup self, Rewired.Controller newController, int controllerIndex, bool forceUpdate)
    {
        var oldController = self.recentController;

        orig(self, newController, controllerIndex, forceUpdate);

        try
        {
            if (!Plugin.ImprovedInputEnabled && ids != null && oldController?.type != self.recentController?.type) //changing controller type
            {
                //ensure the keycode is updated for this player!
                foreach (string id in ids)
                {
                    if (!idToKeyCode.ContainsKey(id) || self.index >= idToKeyCode[id].Length) //don't cause annoying errors pls
                        continue;
                    //idToKeyCode[id][self.index] = 
                    if (newController.type == Rewired.ControllerType.Joystick)
                    {
                        idToKeyCode[id][self.index] = GetControllerCode(IdToControllerKeyCode(id), self.gamePadNumber);
                    }
                    else
                    {
                        idToKeyCode[id][self.index] = IdToKeyCode(id);
                    }
                    Plugin.Log($"Reassigned keycode for {id}:{self.index} = {idToKeyCode[id][self.index]}", 2);
                }
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }


    public static bool IsPressed(string id, int playerNum)
    {
        if (Plugin.ImprovedInputEnabled)
        {
            return ImprovedInputCompat.IsPressed(id, playerNum);
        }

        return Input.GetKey(idToKeyCode[id][playerNum]);
    }

    public static float GetAxis(int axis, int playerNum)
    {
        var controlSetup = RWCustom.Custom.rainWorld.options.controls[playerNum];
        if (controlSetup.gamePad)
            return controlSetup.GetAxis(axisToAction[axis]);

        //for keyboard, use the key press
        return IsPressed(AxisToId(axis), playerNum) ? 1f : 0f;
    }

    public static void GameStarted()
    {
        try
        {
            if (!Plugin.ImprovedInputEnabled)
            {
                var controls = RWCustom.Custom.rainWorld.options.controls;

                //assign keybinds
                foreach (string id in ids)
                {
                    KeyCode[] codes = new KeyCode[controls.Length];
                    KeyCode keyboardCode = IdToKeyCode(id);
                    KeyCode controllerCode = IdToControllerKeyCode(id);

                    for (int i = 0; i < controls.Length; i++)
                    {
                        if (controls[i].gamePad)
                        {
                            codes[i] = GetControllerCode(controllerCode, controls[i].gamePadNumber);
                            Plugin.Log("Controller code: " + codes[i].ToString());
                        }
                        else
                        {
                            codes[i] = keyboardCode;
                        }
                    }

                    idToKeyCode[id] = codes;
                }

                //assign axes
                foreach (var control in RWCustom.Custom.rainWorld.options.controls)
                {
                    if (control?.gameControlMap == null)
                        continue;

                    foreach (var kvp in axisToAction)
                    {
                        //remove previous binding
                        control.gameControlMap.DeleteElementMapsWithAction(kvp.Value);

                        //bind axis KEY to action VALUE
                        control.gameControlMap.ReplaceOrCreateElementMap(new(Rewired.ControllerType.Joystick, Rewired.ControllerElementType.Axis, kvp.Key, Rewired.AxisRange.Full, KeyCode.None, Rewired.ModifierKeyFlags.None, kvp.Value, Rewired.Pole.Positive, false));
                        Plugin.Log($"Mapped controller axis {kvp.Key} to action {kvp.Value}");
                    }
                }

                //assign axis buttons for keyboard
                foreach (int axis in axisToAction.Keys)
                {
                    string id = AxisToId(axis);
                    KeyCode[] codes = new KeyCode[controls.Length];
                    KeyCode code = IdToKeyCode(id);
                    for (int i = 0; i < codes.Length; i++) codes[i] = code; //just make every player use the same keycode for it!
                    idToKeyCode[id] = codes;
                }

            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static KeyCode GetControllerCode(KeyCode code, int controllerNum)
    {
        string s = code.ToString();
        string trimmedCode = s.Substring(s.IndexOf("Button"));
        return (KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + (controllerNum + 1) + trimmedCode);
    }

}
