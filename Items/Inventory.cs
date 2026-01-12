using MetroidvaniaMode.Abilities;
using MetroidvaniaMode.UI;
using System;
using System.Linq;
using UnityEngine;

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
    }

    public static void RemoveHooks()
    {
        On.Player.GrabUpdate -= Player_GrabUpdate;
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
            if (!CurrentItems.ItemInfos.Values.Any(i => i.max > 0) || self.isNPC)
            {
                orig(self, eu);
                return; //no eligible items for inventory, so don't open inventory!
            }

            PlayerInfo info = self.GetInfo();

            bool open = false;
            if (self.input[0].pckp)
            {
                info.HoldGrabTime++;
                if (info.HoldGrabTime > Options.InventoryOpenTime - InventoryWheel.OpenTime / 2 && !self.inShortcut) //start opening it early
                {
                    open = true;

                    HUD.HUD hud = self.abstractPhysicalObject.world.game.cameras[0].hud;
                    if (info.InventoryWheel == null || info.InventoryWheel.slatedForDeletion || info.InventoryWheel.hud != hud) //need a new InventoryWheel
                    {
                        if (hud != null)
                        {
                            if (info.InventoryWheel != null) info.InventoryWheel.slatedForDeletion = true;

                            info.InventoryWheel = new(hud);
                            hud.AddPart(info.InventoryWheel);
                            Plugin.Log("Created InventoryWheel for player " + self.playerState.playerNumber);
                        }
                    }

                    //make the slugcat put its arms out
                    if (info.InventoryWheel != null && self.graphicsModule is PlayerGraphics graph)
                    {
                        int handIdx = HandIndex(self, info.InventoryWheel.selection);
                        if (handIdx >= 0)
                        {
                            graph.hands[handIdx].reachingForObject = true;
                            graph.hands[handIdx].absoluteHuntPos = graph.head.pos + new Vector2(10f * (handIdx == 0 ? -1 : 1), 0); //reach to close side of head
                        }
                    }
                }
            }
            else
            {
                if (info.HoldGrabTime >= Options.InventoryOpenTime && info.InventoryWheel != null && !self.inShortcut)
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

                //handle slowing down time based on InventoryWheel alpha
                if (Options.InventorySlowTimeFac > 0 && self.mushroomCounter <= 0) //and we haven't eaten a real mushroom
                {
                    if (info.InventoryWheel.alpha > 0 || info.InventoryWheel.lastAlpha > 0) //inventory wheel is open or actively closing
                    {
                        self.mushroomEffect = info.InventoryWheel.alpha * Options.InventorySlowTimeFac; //set "Adrenaline" (mushroomEffect)
                    }
                }
            }

        } catch (Exception ex) { Plugin.Error(ex); }

        
        orig(self, eu);
    }

    /// <summary>
    /// Finds which hand should be moved.
    /// </summary>
    /// <returns>The index of the best hand, or -1 if neither are suitable choices.</returns>
    private static int HandIndex(Player self, int wheelSelection)
    {
        //no selection = no hand movement
        if (wheelSelection < 0) return -1;
        //empty selection = no hand
        AbstractPhysicalObject.AbstractObjectType selected = CurrentItems.WheelItems[wheelSelection];
        if (selected == null) return -1;
        //holding the selected item (and can store) = use that hand
        for (int i = 0; i < self.grasps.Length; i++)
        {
            if (self.grasps[i] != null && selected == self.grasps[i].grabbed.abstractPhysicalObject.type
                && CurrentItems.ItemInfos[selected].count < CurrentItems.ItemInfos[selected].max) return i;
        }

        //if we can pull the selected item out
        if (CurrentItems.ItemInfos[selected].count > 0)
        {
            //try free hand
            int free = self.FreeHand();
            if (free >= 0) return free;

            //first item that can be swapped
            for (int i = 0; i < self.grasps.Length; i++)
            {
                if (self.grasps[i] != null)
                {
                    AbstractPhysicalObject.AbstractObjectType heldType = self.grasps[i].grabbed.abstractPhysicalObject.type;
                    if (CurrentItems.WheelItems.Contains(heldType)) //if this item is in the wheel
                    {
                        if (CurrentItems.ItemInfos[heldType].count < CurrentItems.ItemInfos[heldType].max)
                            return i;
                    }
                }
            }

            return 0; //just use the first hand, because it'll probably be dropped anyway
        }

        return -1; //cannot store nor pull out, so don't move hand
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
            abObj.realizedObject.firstChunk.HardSetPosition((self.graphicsModule as PlayerGraphics).hands[grasp].pos);
            //abObj.realizedObject.firstChunk.MoveFromOutsideMyUpdate(self.abstractPhysicalObject.world.game.evenUpdate, (self.graphicsModule as PlayerGraphics).hands[grasp].pos);

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

}
