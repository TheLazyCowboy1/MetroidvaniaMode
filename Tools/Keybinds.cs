using MetroidvaniaMode.ModCompat;
using Rewired.Dev;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MetroidvaniaMode.Tools;

public static class Keybinds
{
    public const string DASH_ID = "MVM_Dash";

    //private static Dictionary<string, int> idToAction;
    private static string[] ids;
    private static Dictionary<string, KeyCode[]> idToKeyCode;

    public static void Bind()
    {
        if (Plugin.ImprovedInputEnabled)
        {
            ImprovedInputCompat.Register("MVM_Dash", "Dash", KeyCode.D, KeyCode.Joystick1Button4);
        }
        else
        {
            ids = new string[] { DASH_ID };
            idToKeyCode = new();
            /*idToAction = new(new KeyValuePair<string, int>[] {
                new(DASH_ID, 20)
            });*/
            //do nothing!
        }
    }


    private static KeyCode IdToKeyCode(string id)
    {
        return id switch
        {
            DASH_ID => Options.DashKeyCode,
            _ => KeyCode.D,
        };
    }
    private static KeyCode IdToControllerKeyCode(string id)
    {
        return id switch
        {
            DASH_ID => Options.DashControllerKeyCode,
            _ => KeyCode.D,
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
        orig(self, newController, controllerIndex, forceUpdate);

        try
        {
            if (!Plugin.ImprovedInputEnabled)
            {
                //ensure the keycode is updated for this player!
                foreach (string id in ids)
                {
                    //idToKeyCode[id][self.index] = 
                    if (newController.type == Rewired.ControllerType.Joystick)
                    {
                        KeyCode code = IdToControllerKeyCode(id);
                        string trimmedCode = code.ToString().Substring(code.ToString().IndexOf("Button"));
                        idToKeyCode[id][self.index] = (KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + (controllerIndex + 1) + trimmedCode);
                    }
                    else
                    {
                        idToKeyCode[id][self.index] = IdToKeyCode(id);
                    }
                    Plugin.Log($"Reassigned keycode for {id}:{self.index} = {idToKeyCode[id][self.index]}");
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

        //return RWCustom.Custom.rainWorld.options.controls[playerNum].GetButtonDown(idToAction[id]);
        return Input.GetKey(idToKeyCode[id][playerNum]);
    }

    public static void GameStarted()
    {
        try
        {
            if (!Plugin.ImprovedInputEnabled)
            {
                var controls = RWCustom.Custom.rainWorld.options.controls;
                int count = controls.Length;
                foreach (string id in ids)
                {
                    KeyCode[] arr = new KeyCode[count];
                    KeyCode keyboardCode = IdToKeyCode(id);
                    KeyCode controllerCode = IdToControllerKeyCode(id);
                    string trimmedCode = controllerCode.ToString().Substring(controllerCode.ToString().IndexOf("Button"));

                    for (int i = 0; i < count; i++)
                    {
                        if (controls[i].gamePad)
                        {
                            arr[i] = (KeyCode)Enum.Parse(typeof(KeyCode), "Joystick" + (controls[i].gamePadNumber + 1) + trimmedCode);
                            Plugin.Log("Controller code: " + arr[i].ToString());
                        }
                        else
                        {
                            arr[i] = keyboardCode;
                        }
                    }

                    idToKeyCode[id] = arr;
                }

                /*foreach (var control in RWCustom.Custom.rainWorld.options.controls)
                {
                    if (control?.gameControlMap == null)
                        continue;
                    foreach (var kvp in idToAction)
                    {
                        //Rewired.ActionElementMap prevMap = control.gameControlMap.ButtonMaps.FirstOrDefault(m => m.actionId == kvp.Value);
                        //if (prevMap != null)
                        //control.gameControlMap.ButtonMaps
                        //if (!control.gameControlMap.ContainsAction(kvp.Value))
                        control.gameControlMap.DeleteElementMapsWithAction(kvp.Value);
                        control.gameControlMap.CreateElementMap(kvp.Value, Rewired.Pole.Positive, IdToKeyCode(kvp.Key, control.gamePad), Rewired.ModifierKeyFlags.None);
                        //control.gameControlMap.ReplaceOrCreateElementMap(kvp.Value, Rewired.Pole.Positive, Options.DashKeyCode, Rewired.ModifierKeyFlags.None);
                        //control.gameControlMap.ReplaceOrCreateElementMap(new(IdToKeyCode(kvp.Key, control.gamePad), Rewired.ModifierKeyFlags.None, kvp.Value, Rewired.Pole.Positive));

                        Plugin.Log("Created action keybind: " + kvp.Key, 2);
                    }
                }*/
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
