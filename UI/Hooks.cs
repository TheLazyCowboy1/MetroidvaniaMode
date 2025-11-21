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

            //Vector2 sSize = game.rainWorld.screenSize;
            //Vector2 off = manager.rainWorld.options.SafeScreenOffset;
            //float buttonY = self.continueButton.pos.y - 100f;
            //foreach (Page page in self.pages)
            //{
            Page page = self.pages[0];
            page.subObjects.Add(new BigArrowButton(self, page, "PREV", new(100, self.continueButton.pos.y + 100), -1));
            BigArrowButton nextButton = new BigArrowButton(self, page, "NEXT", self.continueButton.pos + new Vector2(-10, 100), 1);
            nextButton.nextSelectable[3] = self.continueButton;
            page.subObjects.Add(nextButton);
            self.continueButton.nextSelectable[1] = nextButton; //ensure it's actually selectable
            //}
            Plugin.Log("Added pause menu page changing buttons");

            //add page
            self.pages.Add(new InventoryCustomizationPage(self, null, "inventory", self.pages.Count, page.selectables)); //set index to 0 so mouse still appears on it

        } catch (Exception ex) { Plugin.Error(ex); }
    }


    private static void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
    {
        try
        {
            if (message == "PREV")
            {
                if (self.currentPage <= 0)
                    ChangePage(self, self.pages.Count - 1, 1f); //wrap around to end
                else
                    ChangePage(self, self.currentPage - 1, 1f); //move one left

                self.PlaySound(SoundID.HUD_Pause_Game, 0, 0.9f, 1.1f); //small sound effect; why not
                Plugin.Log("Moving to prev page", 2);
                return;
            }
            if (message == "NEXT")
            {
                if (self.currentPage >= self.pages.Count - 1)
                    ChangePage(self, 0, -1f); //wrap around to start
                else
                    ChangePage(self, self.currentPage + 1, -1f); //move one right

                self.PlaySound(SoundID.HUD_Pause_Game, 0, 0.9f, 1.1f); //small sound effect; why not
                Plugin.Log("Moving to next page", 2);
                return;
            }

            if (message != "")
                ChangePage(self, 0, -1f);
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, sender, message);
    }

    private static void ChangePage(PauseMenu self, int page, float dir)
    {
        if (page == self.currentPage) return;

        //add logic here to make the transition smooth?
        if (self.pages[self.currentPage] is ChangeablePage curPage)
            curPage.ChangePage(true, dir);
        if (self.pages[page] is ChangeablePage newPage)
            newPage.ChangePage(false, dir);

        self.currentPage = page;
    }
}
