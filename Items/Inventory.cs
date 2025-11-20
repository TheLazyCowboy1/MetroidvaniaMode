using MetroidvaniaMode.Abilities;
using System;
using System.Linq;

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
        WheelItems[7] = CustomItems.HealFruit;

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
                if (info.HoldGrabTime > Options.InventoryOpenTime - UI.InventoryWheel.OpenTime / 2) //start opening it early
                {
                    open = true;
                }
            }
            else
            {
                if (info.HoldGrabTime >= Options.InventoryOpenTime)
                {
                    //closing inventory after opening, so we should grab or store an item!
                    int selection = UI.InventoryWheel.GetSelection();//Array.IndexOf(UI.InventoryWheel.IntVecs, self.input[0].IntVec);
                    if (selection >= 0)
                    {
                        AbstractPhysicalObject.AbstractObjectType item = WheelItems[selection];
                        if (item != null)
                        {
                            //attempt to store the item first
                            int canStore = CanStoreItem(self, item);
                            if (canStore >= 0)
                            {
                                StoreItem(self, canStore);
                            }
                            else //couldn't store it
                            {
                                Plugin.Log("Failed to store the item. Attempting to pull it out.", 2); int canPullOut = CanPullOutItem(self, item);
                                if (canPullOut >= 0)
                                {
                                    PullOutItem(self, item, canPullOut);
                                }
                                else //couldn't pull it out, either
                                {
                                    Plugin.Log("Failed to pull out the item. Attempting to swap it.", 2);
                                    for (int i = 0; i < self.grasps.Length; i++)
                                    {
                                        if (self.grasps[i] != null) //look for an item that can be stored
                                        {
                                            //int canStore2 = CanStoreGrasp(self, i);
                                            if (CanStoreGrasp(self, i))
                                            {
                                                int canPullOut2 = CanPullOutItem(self, item, i);
                                                if (canPullOut2 >= 0)
                                                {
                                                    StoreItem(self, i);
                                                    PullOutItem(self, item, canPullOut2);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
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
    private static int CanPullOutItem(Player self, AbstractPhysicalObject.AbstractObjectType item, int artificiallyEmptyGrasp = -1)
    {
        if (!CurrentItems.ItemInfos.ContainsKey(item))
            return -1;
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];

        //check for empty hand
        int grasp = self.FreeHand();

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
        AbstractPhysicalObject abObj;
        if (item == AbstractPhysicalObject.AbstractObjectType.Spear)
            abObj = new AbstractSpear(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), false);
        else if (item == AbstractPhysicalObject.AbstractObjectType.BubbleGrass)
            abObj = new BubbleGrass.AbstractBubbleGrass(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), 1f, -1, -1, null);
        else if (item == AbstractPhysicalObject.AbstractObjectType.FlareBomb)
            abObj = new AbstractConsumable(self.abstractPhysicalObject.world, item, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), -1, -1, null);
        else if (item == CustomItems.HealFruit)
            abObj = new DangleFruit.AbstractDangleFruit(self.abstractPhysicalObject.world, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID(), -1, -1, false, null) { type = CustomItems.HealFruit }; //manually re-assign the type
        else
            abObj = new(self.abstractPhysicalObject.world, item, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID());
        
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
                self.AddPart(new UI.InventoryWheel(self));
                Plugin.Log("Added InventoryWheel to hud!");
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
