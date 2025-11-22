using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using MetroidvaniaMode.Items;
using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using RWCustom;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public class InventoryCustomizationPage : ChangeablePage
{
    private MenuLabel Title;

    private FSprite FadeSprite;

    private InventorySlot[] slots;

    private Vector2 wheelCenter = new(500, 200);
    private Vector2 itemBankCenter = new(500, 100);

    public bool selectingSlot = false;
    public AbstractPhysicalObject.AbstractObjectType selectedItem;

    public InventoryCustomizationPage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index, extraSelectables)
    {
        Title = new(menu, this, menu.Translate("Inventory"), new(500, 500), new(500, 50), true);
        subObjects.Add(Title);

        FadeSprite = new(Futile.whiteElement);
        FadeSprite.color = new(0, 0, 0); //black
        FadeSprite.alpha = 0; //currently not active
        Vector2 sSize = menu.manager.rainWorld.screenSize;
        FadeSprite.width = sSize.x + 20;
        FadeSprite.height = sSize.y + 20;
        FadeSprite.SetPosition(-10, -10); //10 pixel buffer, just in case
        this.Container.AddChild(FadeSprite);

        //add the wheel
        slots = new InventorySlot[8];
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new(this.Container);
            slots[i].SetAlpha(1);

            //set item
            slots[i].SetItemSprite(CurrentItems.WheelItems[i]);

            //position the slots
            Vector2 offset = InventoryWheel.IntVecs[i].ToVector2();
            if ((i & 1) == 1) offset *= 0.70710678118654752440084436210485f; //* sqrt(2)/2 to normalize
            offset *= 90f; //wheel radius
            slots[i].SetPosition(wheelCenter + offset);
        }

        //add the buttons to be used
        //get list to add
        List<AbstractPhysicalObject.AbstractObjectType> items = new(CurrentItems.ItemInfos.Count);
        foreach (var kvp in CurrentItems.ItemInfos)
        {
            if (kvp.Value.max > 0)
                items.Add(kvp.Key);
        }
        const int maxWidth = 8;
        const float sizeX = 40, sizeY = 40;
        int groupHeight = items.Count / maxWidth;
        for (int i = 0; i < items.Count; i++)
        {
            int y = -i / maxWidth;
            int x = i - y * maxWidth - maxWidth / 2;
            //add the button itself
            SymbolButton b = new(menu, this, ItemSymbol.SpriteNameForItem(items[i], 0), "INVENTORY_" + items[i].value, new(itemBankCenter.x + x * sizeX, itemBankCenter.y + y * sizeY));
            b.size = new(sizeX, sizeY);
            this.subObjects.Add(b);
        }

    }

    public override void Singal(MenuObject sender, string message)
    {
        if (message.StartsWith("INVENTORY_"))
        {
            selectingSlot = true;
            selectedItem = new(message.Substring("INVENTORY_".Length));
            Plugin.Log("Starting wheel slot selection process", 2);
            return;
        }

        base.Singal(sender, message);
    }

    public override void Update()
    {
        base.Update();

        FadeSprite.alpha = selectingSlot ? 0.3f : 0f;

        if (selectingSlot)
        {
            int selection = -1;
            //deal with selections
            if (menu.manager.menuesMouseMode) //mouse handling
            {
                Vector2 mouseDir = (menu.mousePosition - wheelCenter).normalized;
                float bestScore = float.NegativeInfinity;
                for (int i = 0; i < InventoryWheel.IntVecs.Length; i++)
                {
                    Vector2 vec = InventoryWheel.IntVecs[i].ToVector2();
                    if ((i & 1) == 1) vec *= 0.70710678118654752440084436210485f; //* sqrt(2)/2 to normalize
                    float score = mouseDir.x * vec.x + mouseDir.y * vec.y;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        selection = i;
                    }
                }
            }
            else //controller handling
            {
                menu.allowSelectMove = false;
                selection = InventoryWheel.IntVecs.IndexfOf(menu.input.IntVec);
            }

            //disable selections in the sloppy way
            foreach (InventorySlot slot in slots)
                slot.selected = false;

            //make a selection
            if (menu.holdButton && !menu.lastHoldButton)
            {
                if (selection >= 0)
                {
                    Plugin.Log($"Assigned {selectedItem} to slot {selection}");
                    CurrentItems.WheelItems[selection] = selectedItem;
                    //NEED TO SAVE THIS SOMEHOW
                    if (menu.manager.currentMainLoop is RainWorldGame game && game.IsStorySession)
                    {
                        DeathSaveData data = game.GetStorySession.saveState.deathPersistentSaveData.GetData();
                        data.WheelItems.Set(selectedItem.value, selection);
                        Plugin.Log("Saved assignment to save data", 2);
                    }

                    slots[selection].SetItemSprite(selectedItem);
                }
                selectingSlot = false; //made our selection!
            }
            else if (selection >= 0) //show what is currently selected
                slots[selection].selected = true;
        }

    }

    public override void RemoveSprites()
    {
        foreach (InventorySlot slot in slots)
            slot.ClearSprites();

        base.RemoveSprites();
    }
}
