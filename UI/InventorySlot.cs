using MetroidvaniaMode.Items;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class InventorySlot
{
    //private HUD.HUD hud;
    //private FContainer fContainer => this.hud.fContainers[1];
    private FContainer container;

    private const float CircleDiameter = 45f;
    private const float SymbolSize = 25f;
    private const float LabelHeight = 15f;

    private const float BaseCircleAlpha = 0.3f;
    private const float SelectedCircleAlpha = 1f;

    private float alpha = 0;
    private Vector2 pos = new(0, 0);

    private FSprite circle;
    private FSprite item;
    private FLabel label;

    public bool selected = false;

    public InventorySlot(FContainer fContainer)
    {
        this.container = fContainer;

        circle = new(Futile.whiteElement);
        circle.shader = Custom.rainWorld.Shaders["VectorCircle"];
        circle.width = CircleDiameter;
        circle.height = CircleDiameter;
        circle.color = new(0.5f, 0.5f, 0.5f);
        circle.alpha = 0;
        circle.isVisible = false;
        container.AddChild(circle);
    }

    public void SetAlpha(float drawnAlpha)
    {
        alpha = drawnAlpha;

        circle.isVisible = alpha > 0;
        circle.alpha = alpha * (selected ? SelectedCircleAlpha : BaseCircleAlpha);

        if (item != null)
        {
            item.isVisible = alpha > 0;
            item.alpha = alpha;
        }
        if (label != null)
        {
            label.isVisible = alpha > 0;
            label.alpha = alpha;
        }
    }

    public void ClearSprites()
    {
        circle.RemoveFromContainer();
        item?.RemoveFromContainer();
        label?.RemoveFromContainer();
    }

    public void SetPosition(Vector2 pos)
    {
        this.pos = pos;

        circle.SetPosition(pos);
        item?.SetPosition(pos);// + new Vector2(0.5f * (CircleDiameter - SymbolSize), 0.5f * (CircleDiameter - SymbolSize))); //center item
        label?.SetPosition(pos + new Vector2(SymbolSize * 0.5f, -SymbolSize * 0.5f));
    }

    public void SetItemSprite(AbstractPhysicalObject.AbstractObjectType itemType)
    {
        try
        {
            //set item sprite
            string spriteName = itemType == null ? null : ItemSymbol.SpriteNameForItem(itemType, 0);
            CurrentItems.ItemInfo info = itemType == null ? null : CurrentItems.ItemInfos[itemType];

            bool shouldShowSprite = itemType != null && info.max > 0;
            if (item == null && shouldShowSprite)
            {
                //initiate the item sprite
                item = new(Futile.atlasManager.GetElementWithName(spriteName));
                item.alpha = alpha;
                item.isVisible = alpha > 0;
                item.scale = Mathf.Min(SymbolSize / item.width, SymbolSize / item.height);
                container.AddChild(item);

                Plugin.Log("Set up inventory symbol for item: " + itemType, 2);
            }
            else if (item != null && !shouldShowSprite)
            {
                //remove the item sprite
                item.RemoveFromContainer();
                item = null;
                //also remove label
                label?.RemoveFromContainer();
                label = null;

                Plugin.Log("Removed inventory symbol for item: " + itemType, 2);
            }
            else if (item != null && item.element.name != spriteName)
            {
                //the item has changed!
                item.element = Futile.atlasManager.GetElementWithName(spriteName);
                item.scale = Mathf.Min(SymbolSize / item.width, SymbolSize / item.height);

                Plugin.Log("Switched inventory symbol for item: " + itemType, 2);
            }

            //set color
            if (item != null && itemType != null)
            {
                Color col = ItemSymbol.ColorForItem(itemType, 0);
                if (info.count < 1)
                {
                    item.color = new(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.5f); //grey out the item if there's none left of it
                }
                else
                {
                    item.color = col;
                }

                //set label
                if (label == null)
                {
                    //create label
                    label = new(Custom.GetFont(), "0");
                    label.alpha = alpha;
                    label.isVisible = alpha > 0;
                    label.scale = LabelHeight / label.FontLineHeight;
                    container.AddChild(label);
                }
                label.text = info.count.ToString();
                label.color = info.count >= info.max ? new(0.2f, 1f, 0.2f) : Color.white; //show as green when at max
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        //reset position in case we added any new sprites that need to be relocated
        SetPosition(pos);
    }

}
