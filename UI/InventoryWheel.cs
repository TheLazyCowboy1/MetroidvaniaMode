using HUD;
using MetroidvaniaMode.Items;
using RWCustom;
using System;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class InventoryWheel : HudPart
{
    private static InventoryWheel LastInstance;

    public float alpha = 0;
    private const float alphaStep = 1f / 0.1f / 40f; //0.1 second
    private const float baseCircleAlpha = 0.5f;
    private const float selectedCircleAlpha = 0.8f;
    public bool visible = false;

    public int selection = -1;

    private FContainer fContainer => this.hud.fContainers[1];

    private const float WheelRadius = 60f;
    private const float CircleDiameter = 30f;
    private const float SymbolSize = 25f;
    //private HUDCircle[] circles;
    private FSprite[] circles;
    private FSprite[] items;

    public InventoryWheel(HUD.HUD hud) : base(hud)
    {
        circles = new FSprite[8];
        for (int i = 0; i < circles.Length; i++)
        {
            circles[i] = new(Futile.whiteElement);
            circles[i].shader = hud.rainWorld.Shaders["VectorCircleFadable"];
            circles[i].width = CircleDiameter;
            circles[i].height = CircleDiameter;
            circles[i].color = new(0.5f, 0.5f, 0.5f);
            circles[i].alpha = 0;
            circles[i].isVisible = false;
            fContainer.AddChild(circles[i]);
        }

        items = new FSprite[8];

        LastInstance = this;
    }

    public override void Update()
    {
        base.Update();

        if (visible || alpha > 0)
        {
            if (visible && alpha < 1)
                alpha += alphaStep;
            else
                alpha -= alphaStep;

            //apply alpha to sprites
            for (int i = 0; i < circles.Length; i++)
            {
                if (circles[i].isVisible != alpha > 0)
                    circles[i].isVisible = alpha > 0;
                circles[i].alpha = alpha * (selection == i ? selectedCircleAlpha : baseCircleAlpha);

                if (items[i] != null)
                {
                    if (items[i].isVisible != alpha > 0)
                        items[i].isVisible = alpha > 0;
                    items[i].alpha = alpha;
                }
            }
        }
    }

    public override void ClearSprites()
    {
        base.ClearSprites();

        for (int i = 0; i < circles.Length; i++)
        {
            circles[i].RemoveFromContainer();
            items[i]?.RemoveFromContainer();
        }

        LastInstance = null; //it got cleared, so it's no longer active
    }


    public void MoveTo(Vector2 pos)
    {
        for (int i = 0; i < circles.Length; i++)
        {
            try
            {
                //Vector2 offset = new((i == 0 || i == 4) ? 0 : (i > 4 ? -1 : 1), (i == 2 || i == 6) ? 0 : ((i < 2 || i > 6) ? -1 : 1));
                Vector2 offset = IntVecs[i].ToVector2();
                if ((i & 1) == 1) offset *= 0.70710678118654752440084436210485f; //* sqrt(2)/2 to normalize
                offset *= WheelRadius;

                circles[i].SetPosition(pos + offset);
                items[i]?.SetPosition(pos + offset + new Vector2(0.5f * (CircleDiameter - SymbolSize), 0.5f * (CircleDiameter - SymbolSize))); //center item
            }
            catch (Exception ex) { Plugin.Error(ex); }
        }
    }
    public void SetItemSprites()
    {
        for (int i = 0; i < circles.Length; i++)
        {
            try
            {
                //set item sprite
                string spriteName = Inventory.WheelItems[i] == null ? null : ItemSymbol.SpriteNameForItem(Inventory.WheelItems[i], 0);
                if (items[i] == null && Inventory.WheelItems[i] != null)
                {
                    //initiate the item sprite
                    items[i] = new(Futile.atlasManager.GetElementWithName(spriteName));
                    items[i].alpha = alpha;
                    items[i].isVisible = alpha > 0;
                    items[i].scale = Mathf.Min(SymbolSize / items[i].width, SymbolSize / items[i].height);
                    fContainer.AddChild(items[i]);
                }
                else if (items[i] != null && Inventory.WheelItems[i] == null)
                {
                    //remove the item sprite
                    items[i].RemoveFromContainer();
                    items[i] = null;
                }
                else if (items[i] != null && items[i].element.name != spriteName)
                {
                    //the item has changed!
                    items[i].element = Futile.atlasManager.GetElementWithName(spriteName);
                    items[i].scale = Mathf.Min(SymbolSize / items[i].width, SymbolSize / items[i].height);
                }

                //set color
                if (items[i] != null && Inventory.WheelItems[i] != null)
                {
                    Color col = ItemSymbol.ColorForItem(Inventory.WheelItems[i], 0);
                    if (CurrentItems.ItemInfos[Inventory.WheelItems[i]].count < 1)
                    {
                        items[i].color = new(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.5f); //grey out the item if there's none left of it
                    }
                    else
                    {
                        items[i].color = col;
                    }
                }
            }
            catch (Exception ex) { Plugin.Error(ex); }
        }
    }
    public static void SetVisible(bool vis, Vector2 pos)
    {
        if (LastInstance == null)
        {
            Plugin.Error("Inventory Wheel does not exist!");
            return;
        }

        if (!LastInstance.visible && vis)
        {
            LastInstance.SetItemSprites();
        }
        LastInstance.visible = vis;

        if (vis)
        {
            LastInstance.MoveTo(pos);
        }
        else
        {
            LastInstance.selection = -1;
        }
    }

    public static void SetSelected(IntVector2 direction)
    {
        if (LastInstance == null)
        {
            Plugin.Error("Inventory Wheel does not exist!");
            return;
        }

        LastInstance.selection = Array.IndexOf(IntVecs, direction);
    }
    public static IntVector2[] IntVecs => new IntVector2[] { new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(-1, -1), new(-1, 0), new(-1, 1) };

}
