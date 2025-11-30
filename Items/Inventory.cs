using MetroidvaniaMode.Abilities;
using MetroidvaniaMode.UI;
using System;
using System.Linq;

namespace MetroidvaniaMode.Items;

public static class Inventory
{
    private static AbstractPhysicalObject CreateItem(Player self, AbstractPhysicalObject.AbstractObjectType item)
    {
        if (item == AbstractPhysicalObject.AbstractObjectType.Spear)
            return new AbstractSpear(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), false);
        else if (item == AbstractPhysicalObject.AbstractObjectType.BubbleGrass)
            return new BubbleGrass.AbstractBubbleGrass(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), 1f, -1, -1, null);
        else if (item == CustomItems.HealFruit)
            return new DangleFruit.AbstractDangleFruit(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), -1, -1, false, null) { type = CustomItems.HealFruit }; //manually re-assign the type

        //return an AbstractConsumable by default, because this is required for Mushrooms and FlareBombs, and it doesn't hurt anything else (like Lanterns)
        return new AbstractConsumable(self.abstractPhysicalObject.world, item, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), -1, -1, null);
    }


    public static void ApplyHooks()
    {
        On.Player.GrabUpdate += Player_GrabUpdate;

        //On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
    }

    public static void RemoveHooks()
    {
        On.Player.GrabUpdate -= Player_GrabUpdate;

        //On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
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
            //check if there are any items to pull out
            if (!CurrentItems.ItemInfos.Values.Any(i => i.max > 0))
            {
                orig(self, eu);
                return; //no eligible items for inventory, so don't open inventory!
            }

            PlayerInfo info = self.GetInfo();

            bool open = false;
            if (self.input[0].pckp)
            {
                info.HoldGrabTime++;
                if (info.HoldGrabTime > Options.InventoryOpenTime - InventoryWheel.OpenTime / 2) //start opening it early
                {
                    open = true;

                    if (info.InventoryWheel == null)
                    {
                        HUD.HUD hud = self.abstractPhysicalObject.world.game.cameras[0].hud;
                        if (hud != null)
                        {
                            info.InventoryWheel = new(hud);
                            hud.AddPart(info.InventoryWheel);
                            Plugin.Log("Created InventoryWheel for player " + self.playerState.playerNumber);
                        }
                    }
                }
            }
            else
            {
                if (info.HoldGrabTime >= Options.InventoryOpenTime && info.InventoryWheel != null)
                {
                    //closing inventory after opening, so we should grab or store an item!
                    int selection = info.InventoryWheel.selection;//Array.IndexOf(UI.InventoryWheel.IntVecs, self.input[0].IntVec);
                    if (selection >= 0)
                    {
                        AbstractPhysicalObject.AbstractObjectType item = CurrentItems.WheelItems[selection];
                        if (item != null)
                        {
                            TryPullOrStoreItem(self, item);
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
            if (info.InventoryWheel != null)
            {
                info.InventoryWheel.SetVisible(open, self.mainBodyChunk.pos - self.abstractPhysicalObject.world.game.cameras[0].pos);
                if (open)
                    info.InventoryWheel.SetSelection(self.input[0].IntVec);
            }

        } catch (Exception ex) { Plugin.Error(ex); }

        
        orig(self, eu);
    }

    private static void TryPullOrStoreItem(Player self, AbstractPhysicalObject.AbstractObjectType item)
    {
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];

        //attempt to store the item first
        int canStore = CanStoreItem(self, item);
        if (canStore >= 0)
        {
            StoreItem(self, canStore);
            return;
        }

        if (itemInfo.count < 1) //don't try to pull it out if we don't have any to pull out
        {
            Plugin.Log($"Failed to store {item}. NOT attempting to pull it out: We have 0 of it.");
        }

        //couldn't store it; attempt to pull it out
        Plugin.Log($"Failed to store {item}. Attempting to pull it out.", 2); int canPullOut = CanPullOutItem(self, item);
        if (canPullOut >= 0)
        {
            PullOutItem(self, item, canPullOut);
            return;
        }

        //couldn't pull it out, either
        Plugin.Log($"Failed to pull out {item}. Attempting to swap it.", 2);
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (CanStoreGrasp(self, i)) //look for an item that can be stored
            {
                int canPullOut2 = CanPullOutItem(self, item, i);
                if (canPullOut2 >= 0)
                {
                    StoreItem(self, i);
                    PullOutItem(self, item, canPullOut2);
                    return;
                }
            }
        }

        //couldn't swap it; last-ditch effort now!
        Plugin.Log($"Failed to swap {item}. Attempting to drop the currently held item.", 2);
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] != null) //can't drop null grasps, silly
            {
                int canPullOut3 = CanPullOutItem(self, item, i);
                if (canPullOut3 >= 0)
                {
                    self.ReleaseGrasp(i); //drop it
                    PullOutItem(self, item, canPullOut3);
                    return;
                }
            }
        }

        Plugin.Error($"Unable to pull out or store {item} whatsoever!"); //this should surely be impossible to trigger???
    }
    private static int CanPullOutItem(Player self, AbstractPhysicalObject.AbstractObjectType item, int artificiallyEmptyGrasp = -1)
    {
        if (!CurrentItems.ItemInfos.ContainsKey(item))
            return -1;
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];

        //check for empty hand
        int grasp = -1; // self.FreeHand();
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] == null || i == artificiallyEmptyGrasp)
            {
                grasp = i;
                break;
            }
        }

        if (grasp >= 0 && itemInfo.count > 0
            && (item != AbstractPhysicalObject.AbstractObjectType.Spear || self.grasps[1 - grasp] == null || 1 - grasp == artificiallyEmptyGrasp || self.Grabability(self.grasps[1 - grasp].grabbed) < Player.ObjectGrabability.BigOneHand) //can't pull out two spears
            ) //able to pull item out
        {
            return grasp;
        }
        return -1;
    }
    private static void PullOutItem(Player self, AbstractPhysicalObject.AbstractObjectType item, int grasp)
    {
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];

        //create the item
        AbstractPhysicalObject abObj = CreateItem(self, item);
        
        abObj.RealizeInRoom();

        //visual thingy; move item towards hand?
        if (self.graphicsModule != null)
            abObj.realizedObject.firstChunk.MoveFromOutsideMyUpdate(self.abstractPhysicalObject.world.game.evenUpdate, (self.graphicsModule as PlayerGraphics).hands[grasp].pos);

        self.SlugcatGrab(abObj.realizedObject, grasp);
        

        self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, self.mainBodyChunk);
        itemInfo.count--;

        self.noPickUpOnRelease = 10; //briefly prevents picking up objects

        Plugin.Log("Pulling item out of inventory: " + item, 2);
    }

    private static int CanStoreItem(Player self, AbstractPhysicalObject.AbstractObjectType item)
    {
        //check if we already have item in hand
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] != null && self.grasps[i].grabbed.abstractPhysicalObject.type == item)
            {
                return CanStoreGrasp(self, i) ? i : -1;
            }
        }
        return -1;
    }
    private static bool CanStoreGrasp(Player self, int grasp)
    {
        if (self.grasps[grasp] == null)
            return false; //duh

        AbstractPhysicalObject ab = self.grasps[grasp].grabbed.abstractPhysicalObject;
        if (ab is AbstractSpear spear && (spear.explosive || spear.electric))
            return false; //don't store explosive spears; that'd be annoying

        if (!CurrentItems.ItemInfos.ContainsKey(ab.type))
            return false; //don't store things that can't be in the inventory

        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[ab.type];
        return itemInfo.count < itemInfo.max; //we can store it unless it's at max
    }
    private static void StoreItem(Player self, int grasp)
    {
        PhysicalObject obj = self.grasps[grasp].grabbed;
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[obj.abstractPhysicalObject.type];

        //store the item
        self.ReleaseGrasp(grasp);
        obj.RemoveFromRoom();
        obj.abstractPhysicalObject.Room.RemoveEntity(obj.abstractPhysicalObject);
        obj.abstractPhysicalObject.Destroy();
        obj.Destroy();

        self.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, self.mainBodyChunk);
        itemInfo.count++;

        self.noPickUpOnRelease = 10; //briefly prevents picking up objects

        Plugin.Log("Storing item in inventory: " + obj.abstractPhysicalObject.type, 2);
    }


    //Initiate the inventory wheel HUD object
    private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    {
        orig(self, cam);

        try
        {
            if (CurrentAbilities.HasInventory)
            {
                self.AddPart(new InventoryWheel(self));
                Plugin.Log("Added InventoryWheel to hud!");
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
