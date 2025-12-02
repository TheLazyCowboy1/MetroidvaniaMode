using MetroidvaniaMode.ModCompat;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode.Tools;

public static class Keybinds
{
    public const string DASH_ID = "MVM_Dash",
        SHIELD_ID = "MVM_Shield";

    //which ids should be registered
    private static string[] ids = new string[] { DASH_ID, SHIELD_ID };

    //0 = LSx, 1 = LSy, 2 = RSx, 3 = RSy, 4 = LT, 5 = RT
    public const int LEFT_TRIGGER_AXIS = 4,
        RIGHT_TRIGGER_AXIS = 5;

    //which axes should be bound in the controlMap
    private static int[] axes = new int[] { LEFT_TRIGGER_AXIS, RIGHT_TRIGGER_AXIS };

    //used for button binds for controllers
    private static Dictionary<string, KeyCode[]> idToControllerCode = new(ids.Length);


    public static void Bind()
    {
        //if (Plugin.ImprovedInputEnabled) //this doesn't even work, since we don't know if it's enabled yet!
        try
        {
            ImprovedInputCompat.Register(DASH_ID, "Dash", Options.DashKeyCode, Options.DashControllerKeyCode);
            ImprovedInputCompat.Register(SHIELD_ID, "Shield", Options.ShieldKeyCode, Options.ShieldControllerKeyCode);
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
    private static KeyCode IdToBaseControllerKeyCode(string id)
    {
        return id switch
        {
            DASH_ID => Options.DashControllerKeyCode,
            SHIELD_ID => Options.ShieldControllerKeyCode,
            _ => KeyCode.None,
        };
    }

    private static int IdToAxis(string id)
    {
        return id switch
        {
            DASH_ID => InputTypeToAxis(Options.DashInputType),
            SHIELD_ID => InputTypeToAxis(Options.ShieldInputType),
            _ => -1
        };
    }
    private static int InputTypeToAxis(string inputType)
    {
        return inputType switch
        {
            "LT" => LEFT_TRIGGER_AXIS,
            "RT" => RIGHT_TRIGGER_AXIS,
            _ => -1
        };
    }

    private static int AxisToAction(int axis) {
        return axis switch
        {
            //2 => RewiredConsts.Action.UIHorizontal, //right stick left/right //I am unsure if these actions are actually used
            //3 => RewiredConsts.Action.UIVertical, //right stick up/down
            LEFT_TRIGGER_AXIS => RewiredConsts.Action.UICheatHoldLeft, //left trigger
            RIGHT_TRIGGER_AXIS => RewiredConsts.Action.UIAlternate, //right trigger //UICheatHoldRight is used in the region menu and remix menu
            _ => -1
        };
    }


    /// <summary>
    /// Whether the player is currently pressing a certain input button
    /// </summary>
    /// <param name="id">The input to search for. Using a non-existent ID may throw an error.</param>
    /// <param name="playerNum">The player number: PlayerState.playerNumber</param>
    /// <returns>True if the button is currently down; otherwise, false</returns>
    public static bool IsPressed(string id, int playerNum)
    {
        var controlSetup = RWCustom.Custom.rainWorld.options.controls[playerNum];

        if (controlSetup.gamePad)
        {
            int axis = IdToAxis(id);
            if (axis >= 0)
            {
                return controlSetup.player.GetAxisRaw(AxisToAction(axis)) > 0.2f; //axis
            }
        }

        if (Plugin.ImprovedInputEnabled)
        {
            return ImprovedInputCompat.IsPressed(id, playerNum); //improved input config
        }
        if (controlSetup.gamePad)
        {
            return Input.GetKey(idToControllerCode[id][controlSetup.gamePadNumber]); //controller key bind
        }
        return Input.GetKey(IdToKeyCode(id)); //keyboard
    }

    /// <summary>
    /// How much the player is currently pressing a certain input
    /// </summary>
    /// <param name="id">The input to search for. Using a non-existent ID may throw an error.</param>
    /// <param name="playerNum">The player number: PlayerState.playerNumber</param>
    /// <returns>How depressed the axis is from 0 to 1 (or -1 to 1 if applicable). If the input is mapped to a button, returns 1 if pressed; else 0.</returns>
    public static float GetAxis(string id, int playerNum)
    {
        var controlSetup = RWCustom.Custom.rainWorld.options.controls[playerNum];
        if (controlSetup.gamePad)
        {
            int axis = IdToAxis(id);
            if (axis >= 0) //only return the axis if the input is actually bound to this axis
            {
                return controlSetup.player.GetAxisRaw(AxisToAction(axis));
            }
        }

        //for keyboard, use the key press
        return IsPressed(id, playerNum) ? 1f : 0f;
    }

    /// <summary>
    /// Maps the axes (e.g: left and right trigger) to their actions, and maps the gamepad buttons to keycodes
    /// </summary>
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
                    KeyCode[] codes = new KeyCode[8];
                    //KeyCode keyboardCode = IdToKeyCode(id);
                    KeyCode controllerCode = IdToBaseControllerKeyCode(id);

                    for (int i = 0; i < codes.Length; i++)
                    {
                        codes[i] = GetControllerCode(controllerCode, i);
                        if (i == 0)
                            Plugin.Log("Controller code: " + codes[i].ToString(), 2);
                    }

                    idToControllerCode[id] = codes;
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
                        if (map.controllerType != Rewired.ControllerType.Joystick)// || map.categoryId != RewiredConsts.Category.UI)
                            continue; //only applies to Joysticks

                        foreach (int axis in axes)
                        {
                            int action = AxisToAction(axis);

                            //remove previous binding
                            if (map.DeleteElementMapsWithAction(action)) Plugin.Log("Deleted element maps for action " + action, 2);

                            //bind axis to action
                            if (map.categoryId == RewiredConsts.Category.Default)
                            {
                                map.ReplaceOrCreateElementMap(new(Rewired.ControllerType.Joystick, Rewired.ControllerElementType.Axis, axis, Rewired.AxisRange.Full, KeyCode.None, Rewired.ModifierKeyFlags.None, action, Rewired.Pole.Positive, false));
                                Plugin.Log($"Mapped controller axis {axis} to action {action}");
                            }
                        }
                    }
                }

            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static KeyCode GetControllerCode(KeyCode code, int controllerNum)
    {
        string s = code.ToString();
        if (!s.Contains("Button")) return KeyCode.None; //just in case we have a bad keybind

        string trimmedCode = s.Substring(s.IndexOf("Button"));
        return (KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + (controllerNum + 1) + trimmedCode);
    }

}
