using System;
using Menu;
using UnityEngine;

namespace MetroidvaniaMode.UI;

public static class Hooks
{
    public static void ApplyHooks()
    {
        On.Menu.PauseMenu.ctor += PauseMenu_ctor;
        On.Menu.PauseMenu.Singal += PauseMenu_Singal;
    }

    public static void RemoveHooks()
    {
        On.Menu.PauseMenu.ctor -= PauseMenu_ctor;
        On.Menu.PauseMenu.Singal -= PauseMenu_Singal;
    }


    private static void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
    {
        orig(self, manager, game);

        try
        {
            //add page
            self.pages.Add(new InventoryCustomizationPage(self, null, "inventory", 0)); //set index to 0 so mouse still appears on it

            //Vector2 sSize = game.rainWorld.screenSize;
            //Vector2 off = manager.rainWorld.options.SafeScreenOffset;
            //float buttonY = self.continueButton.pos.y - 100f;
            //foreach (Page page in self.pages)
            //{
            Page page = self.pages[0];
                page.subObjects.Add(new BigArrowButton(self, page, "PREV", new(100, self.continueButton.pos.y + 100), -1));
                page.subObjects.Add(new BigArrowButton(self, page, "NEXT", self.continueButton.pos + new Vector2(-10, 100), 1));
            //}
            Plugin.Log("Added pause menu page changing buttons");

        } catch (Exception ex) { Plugin.Error(ex); }
    }


    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        try
        {
            if (message == "PREV")
            {
                if (self.currentPage <= 0)
                    ChangePage(self, self.pages.Count - 1, message); //wrap around to end
                else
                    ChangePage(self, self.currentPage - 1, message); //move one left

                self.PlaySound(SoundID.HUD_Pause_Game, 0, 0.9f, 1.1f); //small sound effect; why not
                return;
            }
            if (message == "NEXT")
            {
                if (self.currentPage >= self.pages.Count - 1)
                    ChangePage(self, 0, message); //wrap around to start
                else
                    ChangePage(self, self.currentPage + 1, message); //move one right
                return;
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, sender, message);
    }

    private static void ChangePage(PauseMenu self, int page, string message)
    {
        if (page == self.currentPage) return;

        self.currentPage = page;

        //find matching button
        /*foreach (MenuObject obj in self.pages[page].subObjects)
        {
            if (obj is BigArrowButton b && b.signalText == message)
            {
                self.selectedObject = b;
                return;
            }
        }*/
    }
}
