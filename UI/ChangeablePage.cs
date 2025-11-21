using Menu;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class ChangeablePage : Page
{
    private Vector2 defaultPos;
    private Vector2 targetPos;

    private bool moving = false;
    private bool movingOut = true;
    private bool pageInactive = true;

    public ChangeablePage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index)
    {
        defaultPos = pos;
        targetPos = pos;

        if (extraSelectables != null)
            selectables.AddRange(extraSelectables);
    }

    public override void Update()
    {
        base.Update();

        if (moving)
        {
            pos.x = Custom.LerpAndTick(pos.x, targetPos.x, 0.02f, 25f); //move quickly towards target, then slow to 25/tick
            moving = pos.x != targetPos.x;
            if (!moving)
            {
                pageInactive = movingOut;
            }
        }

        foreach (MenuObject obj in subObjects)
            obj.inactive = pageInactive;
    }

    public void ChangePage(bool moveOut, float dir)
    {
        moving = true;
        movingOut = moveOut;
        pageInactive = movingOut;

        float sSize = this.menu.manager.rainWorld.screenSize.x;
        if (moveOut)
        {
            pos = defaultPos;
            targetPos = defaultPos + new Vector2(sSize * dir, 0);
        }
        else
        {
            pos = defaultPos - new Vector2(sSize * dir, 0);
            targetPos = defaultPos;
        }
        Plugin.Log($"Page moving: pos: {pos}, target: {targetPos}", 2);
    }
}
