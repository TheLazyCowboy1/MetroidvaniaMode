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

    private float sSize => menu.manager.rainWorld.screenSize.x;

    public ChangeablePage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index)
    {
        defaultPos = pos;
        targetPos = pos;

        if (extraSelectables != null)
            selectables.AddRange(extraSelectables);

        //move pos out far, so we can't see the menu
        pos.x += sSize * 4f;
        this.Container.SetPosition(pos);
    }

    public override void Update()
    {
        base.Update();

        if (moving)
        {
            pos.x = Custom.LerpAndTick(pos.x, targetPos.x, 0.02f, 25f); //move quickly towards target, then slow to 25/tick
            this.Container.SetPosition(pos);
            moving = pos.x != targetPos.x;
            if (!moving)
            {
                pageInactive = movingOut;
                if (pageInactive)
                {
                    pos.x = defaultPos.x + 4f * sSize; //ensure it's well out of the way
                    this.Container.SetPosition(pos);
                    this.inactive = true;
                }
            }
        }

        foreach (MenuObject obj in subObjects) //looping like this is inefficient, but I don't want to miss any new objects
        {
            obj.inactive = pageInactive;
        }
    }

    public void ChangePage(bool moveOut, float dir)
    {
        moving = true;
        movingOut = moveOut;
        this.inactive = false;
        pageInactive = true;

        if (moveOut)
        {
            pos = defaultPos;
            targetPos = defaultPos + new Vector2(sSize * dir, 0);
            this.Container.SetPosition(pos);
        }
        else
        {
            pos = defaultPos - new Vector2(sSize * dir, 0);
            targetPos = defaultPos;
            this.Container.SetPosition(pos);
        }
        Plugin.Log($"Page moving: pos: {pos}, target: {targetPos}", 2);
    }
}
