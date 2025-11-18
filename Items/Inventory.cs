using MetroidvaniaMode.Abilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Items;

public static class Inventory
{
    public static AbstractPhysicalObject.AbstractObjectType[] WheelItems = new AbstractPhysicalObject.AbstractObjectType[8];

    public static void ApplyHooks()
    {
        //TEMPORARILY SET WHEEL ITEMS
        WheelItems[0] = AbstractPhysicalObject.AbstractObjectType.Spear;
        WheelItems[1] = AbstractPhysicalObject.AbstractObjectType.ScavengerBomb;
        WheelItems[2] = AbstractPhysicalObject.AbstractObjectType.BubbleGrass;
        WheelItems[3] = AbstractPhysicalObject.AbstractObjectType.FlareBomb;

        On.Player.GrabUpdate += Player_GrabUpdate;

        On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
    }

    public static void RemoveHooks()
    {
        On.Player.GrabUpdate -= Player_GrabUpdate;

        On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
    }


    private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        if (!CurrentAbilities.HasInventory)
        {
            orig(self, eu);
            return;
        }

        try
        {
            PlayerInfo info = self.GetInfo();

            bool open = false;
            if (self.input[0].pckp)
            {
                info.HoldGrabTime++;
                if (info.HoldGrabTime > Options.InventoryOpenTime)
                {
                    open = true;
                }
            }
            else
            {
                if (info.HoldGrabTime > Options.InventoryOpenTime)
                {
                    //closing inventory after opening, so we should grab or store an item!
                    int selection = Array.IndexOf(UI.InventoryWheel.IntVecs, self.input[0].IntVec);
                    if (selection >= 0)
                    {
                        AbstractPhysicalObject.AbstractObjectType item = WheelItems[selection];
                        if (item != null)
                        {
                            int graspIdx = -1;
                            if (self.grasps[0] != null && self.grasps[0].grabbed.abstractPhysicalObject.type == item)
                                graspIdx = 0;
                            else if (self.grasps[1] != null && self.grasps[1].grabbed.abstractPhysicalObject.type == item)
                                graspIdx = 1;

                            if (graspIdx >= 0) //we already have the item
                            {
                                CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];
                                if (itemInfo.count < itemInfo.max)
                                {
                                    //store the item
                                    PhysicalObject obj = self.grasps[graspIdx].grabbed;
                                    self.ReleaseGrasp(graspIdx);
                                    obj.RemoveFromRoom();
                                    obj.abstractPhysicalObject.Destroy();
                                    obj.Destroy();

                                    self.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, self.mainBodyChunk);
                                    itemInfo.count++;

                                    self.noPickUpOnRelease = 10; //briefly prevents picking up objects

                                    Plugin.Log("Storing item in inventory: " + item, 2);
                                }
                                else
                                    Plugin.Log("Inventory closed trying to store an item we already have the max of", 2);
                            }
                            else
                            {
                                //check for empty hand
                                if (self.grasps[0] == null)
                                    graspIdx = 0;
                                else if (self.grasps[1] == null)
                                    graspIdx = 1;

                                if (graspIdx >= 0)
                                {
                                    CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];
                                    if (itemInfo.count > 0)
                                    {
                                        //create the item
                                        AbstractPhysicalObject abObj = new(self.abstractPhysicalObject.world, item, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID());
                                        abObj.RealizeInRoom();
                                        self.SlugcatGrab(abObj.realizedObject, graspIdx);

                                        self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, self.mainBodyChunk);
                                        itemInfo.count--;

                                        self.noPickUpOnRelease = 10; //briefly prevents picking up objects

                                        Plugin.Log("Pulling item out of inventory: " + item, 2);
                                    }
                                }
                                else
                                    Plugin.Log("Inventory closed trying to pull out an item without any empty hands", 2);
                            }
                        }
                        else
                            Plugin.Log("Inventory closed with a blank slot selected", 2);
                    }
                    else
                        Plugin.Log("Inventory closed with no selection", 2);
                }

                info.HoldGrabTime = 0;
            }

            //set inventory UI
            UI.InventoryWheel.SetVisible(open, self.mainBodyChunk.pos - self.abstractPhysicalObject.world.game.cameras[0].pos);
            if (open)
                UI.InventoryWheel.SetSelected(self.input[0].IntVec);

        } catch (Exception ex) { Plugin.Error(ex); }

        
        orig(self, eu);
    }


    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);

        try
        {
            if (CurrentAbilities.HasInventory)
            {
                self.AddPart(new UI.InventoryWheel(self));
                Plugin.Log("Added InventoryWheel to hud!");
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
