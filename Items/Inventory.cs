using MetroidvaniaMode.Abilities;
using System;

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
                            int canPullOut = CanPullOutItem(self, item);
                            if (canPullOut >= 0)
                            {
                                PullOutItem(self, item, canPullOut);
                            }
                            else //couldn't pull it out
                            {
                                Plugin.Log("Failed to pull out the item. Attempting to store it.", 2);
                                int canStore = CanStoreItem(self, item);
                                if (canStore >= 0)
                                {
                                    StoreItem(self, canStore);
                                }
                                else //couldn't store it either!
                                {
                                    Plugin.Log("Failed to store the item. Attempting to swap it.", 2);
                                    for (int i = 0; i < self.grasps.Length; i++)
                                    {
                                        if (self.grasps[i] != null)
                                        {
                                            int canStore2 = CanStoreGrasp(self, i);
                                            int canPullOut2 = CanPullOutItem(self, item, canStore2);
                                            if (canPullOut2 >= 0)
                                            {
                                                StoreItem(self, canStore2);
                                                PullOutItem(self, item, canPullOut2);
                                                break;
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
        int grasp = -1;
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] == null || i == artificiallyEmptyGrasp) //find the first EMPTY grasp
            {
                grasp = i;
                break;
            }
        }

        if (grasp >= 0 && itemInfo.count > 0
            && (item != AbstractPhysicalObject.AbstractObjectType.Spear || self.grasps[1 - grasp] == null || 1 - grasp == artificiallyEmptyGrasp) //can't pull out two spears
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
        else
            abObj = new(self.abstractPhysicalObject.world, item, null, self.abstractPhysicalObject.pos, self.abstractPhysicalObject.world.game.GetNewID());
        abObj.RealizeInRoom();
        self.SlugcatGrab(abObj.realizedObject, grasp);

        self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, self.mainBodyChunk);
        itemInfo.count--;

        self.noPickUpOnRelease = 10; //briefly prevents picking up objects

        Plugin.Log("Pulling item out of inventory: " + item, 2);
    }
    private static int CanStoreItem(Player self, AbstractPhysicalObject.AbstractObjectType item)
    {
        if (!CurrentItems.ItemInfos.ContainsKey(item))
            return -1;
        CurrentItems.ItemInfo itemInfo = CurrentItems.ItemInfos[item];

        //check if we already have item in hand
        int grasp = -1;
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] != null && self.grasps[i].grabbed.abstractPhysicalObject.type == item)
            {
                grasp = i;
                break;
            }
        }

        if (grasp >= 0 && itemInfo.count < itemInfo.max)
        {
            
            return grasp;
        }
        return -1;
    }
    private static int CanStoreGrasp(Player self, int grasp)
    {
        AbstractPhysicalObject ab = self.grasps[grasp].grabbed.abstractPhysicalObject;
        if (ab is AbstractSpear spear && (spear.explosive || spear.electric))
            return -1; //don't store explosive spears; that'd be annoying

        return CanStoreItem(self, ab.type);
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
