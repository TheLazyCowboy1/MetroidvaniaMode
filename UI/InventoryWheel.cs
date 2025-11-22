using HUD;
using MetroidvaniaMode.Items;
using RWCustom;
using System;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class InventoryWheel : HudPart
{
    //private static InventoryWheel LastInstance;

    public float lastAlpha = 0;
    public float alpha = 0;
    public float drawnAlpha = 0;
    public const int OpenTime = 4; //4 ticks = 0.1 second
    private const float AlphaStep = 1f / OpenTime;
    public bool visible = false;

    public bool anyItems = false;

    public int selection = -1;
    public int notSelectedTimer = 0;
    private static int Stickiness => Options.InventoryWheelStickiness;

    private const float WheelRadius = 90f;

    private InventorySlot[] slots;

    public InventoryWheel(HUD.HUD hud) : base(hud)
    {
        slots = new InventorySlot[8];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new(hud.fContainers[1]);
        }
    }

    public override void Update()
    {
        base.Update();

        //update target alpha for wheel
        lastAlpha = alpha;
        if (visible && alpha < 1)
            alpha = Mathf.Clamp01(alpha + AlphaStep);
        else if (!visible && alpha > 0)
            alpha = Mathf.Clamp01(alpha - AlphaStep);
    }
    public override void Draw(float timeStacker)
    {
        base.Draw(timeStacker);

        if (alpha > 0 || lastAlpha > 0 || drawnAlpha > 0)
        {
            drawnAlpha = Mathf.LerpUnclamped(lastAlpha, alpha, timeStacker);
            //apply alpha to sprites
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].selected = selection == i;
                slots[i].SetAlpha(drawnAlpha);
            }
        }
    }

    public override void ClearSprites()
    {
        base.ClearSprites();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].ClearSprites();
        }
    }


    public void MoveTo(Vector2 pos)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            try
            {
                //Vector2 offset = new((i == 0 || i == 4) ? 0 : (i > 4 ? -1 : 1), (i == 2 || i == 6) ? 0 : ((i < 2 || i > 6) ? -1 : 1));
                Vector2 offset = IntVecs[i].ToVector2();
                if ((i & 1) == 1) offset *= 0.70710678118654752440084436210485f; //* sqrt(2)/2 to normalize
                offset *= WheelRadius;

                slots[i].SetPosition(pos + offset);
            }
            catch (Exception ex) { Plugin.Error(ex); }
        }
    }
    public void SetItemSprites()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetItemSprite(CurrentItems.WheelItems[i]);
        }
    }
    public void SetVisible(bool vis, Vector2 pos)
    {
        /*if (LastInstance == null)
        {
            Plugin.Error("Inventory Wheel does not exist!");
            return;
        }*/
        if (!visible && vis)
        {
            SetItemSprites();
        }
        visible = vis;

        if (vis)
        {
            MoveTo(pos);
        }
        else
        {
            selection = -1;
        }
    }

    public void SetSelection(IntVector2 direction)
    {
        if (selection < 0) //if we currently have nothing selected, select the new direction regardless!
        {
            selection = Array.IndexOf(IntVecs, direction);
            notSelectedTimer = 0;
            if (selection >= 0)
                hud.PlaySound(SoundID.MENU_Button_Select_Gamepad_Or_Keyboard);
            return;
        }

        //we already have something selected
        IntVector2 curDir = IntVecs[selection];

        if (curDir == direction) //we're not changing our selection
        {
            notSelectedTimer = 0;
            return;
        }

        //we want to change our selection
        IntVector2 newDir = direction;

        //only set x or y to 0 after waiting Stickiness
        if (direction.x == 0 && notSelectedTimer < Stickiness)
            newDir.x = curDir.x;
        if (direction.y == 0 && notSelectedTimer < Stickiness)
            newDir.y = curDir.y;

        if (newDir != curDir) //we're changing selection
        {
            selection = Array.IndexOf(IntVecs, newDir);
            notSelectedTimer = 0;
            if (selection >= 0)
                hud.PlaySound(SoundID.MENU_Button_Select_Gamepad_Or_Keyboard);
        }
        else //we want to change selection, but we can't, so increase the timer
            notSelectedTimer++;
    }
    public static IntVector2[] IntVecs => new IntVector2[] { new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(-1, -1), new(-1, 0), new(-1, 1) };

}
