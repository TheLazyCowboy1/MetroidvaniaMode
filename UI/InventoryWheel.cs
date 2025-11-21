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
    private const float BaseCircleAlpha = 0.3f;
    private const float SelectedCircleAlpha = 1f;
    public bool visible = false;

    public bool anyItems = false;

    public int selection = -1;
    public int notSelectedTimer = 0;
    private static int Stickiness => Options.InventoryWheelStickiness;

    private FContainer fContainer => this.hud.fContainers[1];

    private const float WheelRadius = 90f;
    private const float CircleDiameter = 45f;
    private const float SymbolSize = 25f;
    private const float LabelHeight = 15f;
    //private HUDCircle[] circles;
    private FSprite[] circles;
    private FSprite[] items;
    private FLabel[] labels;

    public InventoryWheel(HUD.HUD hud) : base(hud)
    {
        circles = new FSprite[8];
        for (int i = 0; i < circles.Length; i++)
        {
            circles[i] = new(Futile.whiteElement);
            circles[i].shader = hud.rainWorld.Shaders["VectorCircle"];
            circles[i].width = CircleDiameter;
            circles[i].height = CircleDiameter;
            circles[i].color = new(0.5f, 0.5f, 0.5f);
            circles[i].alpha = 0;
            circles[i].isVisible = false;
            fContainer.AddChild(circles[i]);
        }

        items = new FSprite[8];
        labels = new FLabel[8];

        //LastInstance = this;
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
            for (int i = 0; i < circles.Length; i++)
            {
                circles[i].isVisible = drawnAlpha > 0;
                circles[i].alpha = drawnAlpha * (selection == i ? SelectedCircleAlpha : BaseCircleAlpha);

                if (items[i] != null)
                {
                    items[i].isVisible = drawnAlpha > 0;
                    items[i].alpha = drawnAlpha;
                }
                if (labels[i] != null)
                {
                    labels[i].isVisible = drawnAlpha > 0;
                    labels[i].alpha = drawnAlpha;
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
            labels[i]?.RemoveFromContainer();
        }

        //LastInstance = null; //it got cleared, so it's no longer active
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
                items[i]?.SetPosition(pos + offset);// + new Vector2(0.5f * (CircleDiameter - SymbolSize), 0.5f * (CircleDiameter - SymbolSize))); //center item
                labels[i]?.SetPosition(pos + offset + new Vector2(SymbolSize * 0.5f, -SymbolSize * 0.5f));
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
                string spriteName = CurrentItems.WheelItems[i] == null ? null : ItemSymbol.SpriteNameForItem(CurrentItems.WheelItems[i], 0);
                CurrentItems.ItemInfo info = CurrentItems.WheelItems[i] == null ? null : CurrentItems.ItemInfos[CurrentItems.WheelItems[i]];

                bool shouldShowSprite = CurrentItems.WheelItems[i] != null && info.max > 0;
                if (items[i] == null && shouldShowSprite)
                {
                    //initiate the item sprite
                    items[i] = new(Futile.atlasManager.GetElementWithName(spriteName));
                    items[i].alpha = alpha;
                    items[i].isVisible = alpha > 0;
                    items[i].scale = Mathf.Min(SymbolSize / items[i].width, SymbolSize / items[i].height);
                    fContainer.AddChild(items[i]);

                    Plugin.Log("Set up inventory symbol for item: " + CurrentItems.WheelItems[i], 2);
                }
                else if (items[i] != null && !shouldShowSprite)
                {
                    //remove the item sprite
                    items[i].RemoveFromContainer();
                    items[i] = null;
                    //also remove label
                    labels[i]?.RemoveFromContainer();
                    labels[i] = null;

                    Plugin.Log("Removed inventory symbol for item: " + CurrentItems.WheelItems[i], 2);
                }
                else if (items[i] != null && items[i].element.name != spriteName)
                {
                    //the item has changed!
                    items[i].element = Futile.atlasManager.GetElementWithName(spriteName);
                    items[i].scale = Mathf.Min(SymbolSize / items[i].width, SymbolSize / items[i].height);

                    Plugin.Log("Switched inventory symbol for item: " + CurrentItems.WheelItems[i], 2);
                }

                //set color
                if (items[i] != null && CurrentItems.WheelItems[i] != null)
                {
                    Color col = ItemSymbol.ColorForItem(CurrentItems.WheelItems[i], 0);
                    if (info.count < 1)
                    {
                        items[i].color = new(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.5f); //grey out the item if there's none left of it
                    }
                    else
                    {
                        items[i].color = col;
                    }

                    //set label
                    if (labels[i] == null)
                    {
                        //create label
                        labels[i] = new(Custom.GetFont(), "0");
                        labels[i].alpha = alpha;
                        labels[i].isVisible = alpha > 0;
                        labels[i].scale = LabelHeight / labels[i].FontLineHeight;
                        fContainer.AddChild(labels[i]);
                    }
                    labels[i].text = info.count.ToString();
                    labels[i].color = info.count >= info.max ? new(0.2f, 1f, 0.2f) : Color.white; //show as green when at max
                }
            }
            catch (Exception ex) { Plugin.Error(ex); }
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
        }
        else //we want to change selection, but we can't, so increase the timer
            notSelectedTimer++;
    }
    private static IntVector2[] IntVecs => new IntVector2[] { new(0, 1), new(1, 1), new(1, 0), new(1, -1), new(0, -1), new(-1, -1), new(-1, 0), new(-1, 1) };

}
