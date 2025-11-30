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

    //0 = LSx, 1 = LSy, 2 = RSx, 3 = RSy, 4 = LT, 5 = RT
    public const int LEFT_TRIGGER_AXIS = 4;

    private static int[] axes = new int[] { LEFT_TRIGGER_AXIS };


    private static Dictionary<string, KeyCode[]> idToKeyCode = new(ids.Length+axes.Length);


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

    private static int AxisToAction(int axis) {
        return axis switch
        { //0 and 1 are left stick horizontal/vertical
            //new(2, RewiredConsts.Action.UIHorizontal), //right stick left/right
            //new(3, RewiredConsts.Action.UIVertical), //right up up/down
            LEFT_TRIGGER_AXIS => RewiredConsts.Action.UICheatHoldLeft, //left trigger
            //new(5, RewiredConsts.Action.UICheatHoldRight) //right trigger
            _ => -1
        };
    }


    public static void ApplyHooks()
    {
        On.Options.ControlSetup.UpdateActiveController_Controller_int_bool += ControlSetup_UpdateActiveController_Controller_int_bool;
    }

    public static void RemoveHooks()
    {
        On.Options.ControlSetup.UpdateActiveController_Controller_int_bool -= ControlSetup_UpdateActiveController_Controller_int_bool;
    }

    private static void ControlSetup_UpdateActiveController_Controller_int_bool(On.Options.ControlSetup.orig_UpdateActiveController_Controller_int_bool orig, global::Options.ControlSetup self, Rewired.Controller newController, int controllerIndex, bool forceUpdate)
    {
        var oldController = self.recentController;

        orig(self, newController, controllerIndex, forceUpdate);

        try
        {
            if (self.recentController == null || oldController?.type == self.recentController.type)
                return; //must changing controller type
            //re-assign keycodes
            if (!Plugin.ImprovedInputEnabled)
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

            //re-assign axes
            /*if (self.recentController.type == Rewired.ControllerType.Joystick && self.gameControlMap != null)
            {
                AssignAxesToControlMap(self.gameControlMap);
            }*/

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
            return controlSetup.GetAxis(AxisToAction(axis));

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

                //assign axis buttons for keyboard
                foreach (int axis in axes)
                {
                    string id = AxisToId(axis);
                    KeyCode[] codes = new KeyCode[controls.Length];
                    KeyCode code = IdToKeyCode(id);
                    for (int i = 0; i < codes.Length; i++) codes[i] = code; //just make every player use the same keycode for it!
                    idToKeyCode[id] = codes;
                }

                //assign axes for THIS map
                foreach (var control in RWCustom.Custom.rainWorld.options.controls)
                {
                    if (control.player == null)
                    {
                        Plugin.Error("Cannot map axes because player is null!");
                        continue;
                    }

                    var maps = control.player.controllers.maps.GetAllMaps();
                    foreach (var map in maps)
                    {
                        if (map.categoryId == RewiredConsts.Category.Default
                            && map.controllerType == Rewired.ControllerType.Joystick)
                        {
                            AssignAxesToControlMap(map);
                        }
                    }

                    //AssignAxesToControlMap(control.gameControlMap);
                }

            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void AssignAxesToControlMap(Rewired.ControllerMap map)
    {
        foreach (int axis in axes)
        {
            int action = AxisToAction(axis);
            //remove previous binding
            map.DeleteElementMapsWithAction(action);

            //bind axis KEY to action VALUE
            map.ReplaceOrCreateElementMap(new(Rewired.ControllerType.Joystick, Rewired.ControllerElementType.Axis, axis, Rewired.AxisRange.Full, KeyCode.None, Rewired.ModifierKeyFlags.None, action, Rewired.Pole.Positive, false));
            Plugin.Log($"Mapped controller axis {axis} to action {action}", 2);
        }
    }

    private static KeyCode GetControllerCode(KeyCode code, int controllerNum)
    {
        string s = code.ToString();
        if (!s.Contains("Button")) return KeyCode.None; //just in case we have a bad keybind

        string trimmedCode = s.Substring(s.IndexOf("Button"));
        return (KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + (controllerNum + 1) + trimmedCode);
    }

}
