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
            ImprovedInputCompat.Register("MVM_Dash", "Dash", KeyCode.D, KeyCode.Joystick1Button12);
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


    public static bool IsDown(string id, int playerNum)
    {
        if (Plugin.ImprovedInputEnabled)
        {
            return ImprovedInputCompat.IsDown(id, playerNum);
        }

        //return RWCustom.Custom.rainWorld.options.controls[playerNum].GetButtonDown(idToAction[id]);
        return Input.GetButtonDown(idToKeyCode[id][playerNum]);
    }

    public static void GameStarted()
    {
        try
        {
            if (!Plugin.ImprovedInputEnabled)
            {
                int count = RWCustom.Custom.rainWorld.options.controls.Length + 1;


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
