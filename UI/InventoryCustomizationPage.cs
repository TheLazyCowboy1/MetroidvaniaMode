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
    private ColoredSymbolButton[] itemButtons; //currently unused; maybe use to grey out buttons if it becomes a problem for controllers?

    private Vector2 wheelCenter = new(700, 500);
    private Vector2 itemBankCenter = new(700, 200);

    public bool selectingSlot = false;
    public AbstractPhysicalObject.AbstractObjectType selectedItem;
    private int selection = -1;

    public InventoryCustomizationPage(Menu.Menu menu, MenuObject owner, string name, int index, List<SelectableMenuObject> extraSelectables) : base(menu, owner, name, index, extraSelectables)
    {
        Title = new(menu, this, menu.Translate("Inventory"), new(550, 700), new(100, 50), true);
        subObjects.Add(Title);

        FadeSprite = new(Futile.whiteElement);
        FadeSprite.color = new(0, 0, 0); //black
        FadeSprite.alpha = 0; //currently not active
        Vector2 sSize = menu.manager.rainWorld.screenSize;
        FadeSprite.width = sSize.x + 20;
        FadeSprite.height = sSize.y + 20;
        FadeSprite.SetPosition(0.5f * sSize + new Vector2(10, 10)); //10 pixel buffer, just in case
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
        const float sizeX = 50, sizeY = 50;
        int groupHeight = items.Count / maxWidth;
        int actualWidth = Mathf.Min(items.Count, maxWidth);
        itemButtons = new ColoredSymbolButton[items.Count];
        for (int i = 0; i < itemButtons.Length; i++)
        {
            float y = -(i / maxWidth);
            float x = i + y * maxWidth - (actualWidth - 1) * 0.5f;
            //add the button itself
            itemButtons[i] = new(menu, this, ItemSymbol.SpriteNameForItem(items[i], 0), "INVENTORY_" + items[i].value, new(itemBankCenter.x + x * sizeX, itemBankCenter.y + y * sizeY));
            itemButtons[i].size = new(sizeX, sizeY);
            itemButtons[i].roundedRect.size = new(sizeX, sizeY); //also set the roundedRect. stupid weird behavior
            itemButtons[i].symbolSprite.scale = Mathf.Min(sizeX / itemButtons[i].symbolSprite.width, sizeY / itemButtons[i].symbolSprite.height); //scale to fit
            itemButtons[i].customColor = ItemSymbol.ColorForItem(items[i], 0); //set custom color
            this.subObjects.Add(itemButtons[i]);
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
            int lastSelection = selection;
            selection = -1;
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

            if (selection != lastSelection)
            {
                menu.PlaySound(SoundID.MENU_Button_Select_Gamepad_Or_Keyboard);
                if (lastSelection >= 0)
                    slots[lastSelection].selected = false;
            }

            //make a selection
            if (menu.holdButton && !menu.lastHoldButton)
            {
                if (!menu.manager.menuesMouseMode) //if using controller/keyboard
                    menu.lastHoldButton = true; //say we are not newly pressing any button

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
                    slots[selection].selected = false;
                }
                selectingSlot = false; //made our selection!
            }
            else if (selection >= 0) //show what is currently selected
                slots[selection].selected = true;
        }

        foreach (InventorySlot slot in slots)
            slot.SetAlpha(1); //update slot alphas (in this case, only to reflect selections)

    }

    public override void RemoveSprites()
    {
        foreach (InventorySlot slot in slots)
            slot.ClearSprites();

        base.RemoveSprites();
    }


    private class ColoredSymbolButton : SymbolButton
    {
        public Color? customColor = null;

        public ColoredSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string singalText, Vector2 pos) : base(menu, owner, symbolName, singalText, pos)
        {
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            if (customColor != null && !this.buttonBehav.greyedOut)
            {
                float brightness = Mathf.Clamp01(0.7f - 0.3f * Mathf.Sin(Mathf.Lerp(this.buttonBehav.lastSin, this.buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f));
                symbolSprite.color = customColor.Value * brightness;
            }
        }

        /*public override Color MyColor(float timeStacker)
        {
            if (customColor != null)
            {
                float brightness = Mathf.Lerp(this.buttonBehav.lastCol, this.buttonBehav.col, timeStacker);
                brightness = Mathf.Max(brightness, Mathf.Lerp(this.buttonBehav.lastFlash, this.buttonBehav.flash, timeStacker));
                Color.RGBToHSV(customColor.Value, out float H, out float S, out float V);
                return Color.HSVToRGB(H, S, V * brightness);
            }
            return base.MyColor(timeStacker);
        }*/
    }
}
