using Menu;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class ChangeablePage : Page
{
    private Vector2 defaultPos;
    private Vector2 targetPos;

    public bool moving = false;
    public bool movingOut = true;
    public bool pageInactive = true;

    private float sSize => menu.manager.rainWorld.screenSize.x;

    public ChangeablePage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index)
    {
        defaultPos = pos;
        targetPos = pos;

        if (extraSelectables != null)
            selectables.AddRange(extraSelectables);

        //fix FContainer
        if (myContainer == null)
        {
            FContainer newContainer = new();
            this.Container.AddChild(newContainer);
            myContainer = newContainer;
        }

        //move pos out far, so we can't see the menu
        SetX(defaultPos.x + sSize * 4f);
    }

    public override void Update()
    {
        base.Update();

        if (moving)
        {
            SetX(Custom.LerpAndTick(pos.x, targetPos.x, 0.02f, 25f)); //move quickly towards target, then slow to 25/tick

            moving = pos.x != targetPos.x;
            if (!moving)
            {
                pageInactive = movingOut;
                if (pageInactive)
                {
                    SetX(defaultPos.x + sSize * 4f); //ensure it's well out of the way
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
            SetX(defaultPos.x);
            targetPos.x = defaultPos.x + sSize * dir;
        }
        else
        {
            SetX(defaultPos.x - sSize * dir);
            targetPos = defaultPos;
        }
        Plugin.Log($"Page moving: pos: {pos}, target: {targetPos}", 2);
    }

    private void SetX(float x)
    {
        pos.x = x;
        myContainer.x = x;
    }
}
