using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode;

public static class ArenaRoomFix
{
    public static void ApplyHooks()
    {
        On.Room.NowViewed += Room_NowViewed;
        //On.Room.TriggerCombatArena += Room_TriggerCombatArena;
    }

    public static void RemoveHooks()
    {
        On.Room.NowViewed -= Room_NowViewed;
        //On.Room.TriggerCombatArena -= Room_TriggerCombatArena;
    }


    //Have battle arena rooms actually trigger
    private static void Room_NowViewed(On.Room.orig_NowViewed orig, Room self)
    {
        orig(self);

        try
        {
            if (self.abstractRoom.isBattleArena)
            {
                //self.TriggerCombatArena();

                Plugin.Log("Adding DoorLocker to room " + self.abstractRoom.name);
                self.AddObject(new DoorLocker(new(self.world, new("FakeAbstractDoorLocker", false), null, new(self.abstractRoom.index, 0, 0, -1), self.game.GetNewID())));
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private class DoorLocker : PhysicalObject
    {
        private const int TimeUntilLock = 40 * 3; //3 seconds until lock
        private const int MinTimeUntilUnlock = TimeUntilLock + 40 * 3; //must be locked for at least 3 seconds

        private List<VoidChain> chains;
        private int viewedTime = 0;
        private bool locked = false;

        public DoorLocker(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            //make it have no collision, like HRGuardManager
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(-5000f, -5000f), 0f, 0f);
            base.bodyChunks[0].collideWithTerrain = false;
            base.bodyChunks[0].collideWithSlopes = false;
            base.bodyChunks[0].collideWithObjects = false;
            base.bodyChunks[0].restrictInRoomRange = 10000f;
            base.bodyChunks[0].defaultRestrictInRoomRange = 10000f;
            base.bodyChunkConnections = new BodyChunkConnection[0];
            base.airFriction = 0f;
            base.gravity = 0f;
            base.bounce = 0f;
            base.surfaceFriction = 0f;
            base.collisionLayer = 0;
            base.waterFriction = 0f;
            base.buoyancy = 0f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (room == null || (!room.BeingViewed && !locked)) //the player isn't in the room; don't lock the shortcuts
            {
                this.Destroy();
                return;
            }

            viewedTime++;
            if (viewedTime < TimeUntilLock)
            {
                return; //don't do anything until it is time to lock
            }

            if (!locked)
            {
                if (room.abstractRoom.isBattleArena)
                {
                    room.TriggerCombatArena();
                    LockShortcuts();
                }
                else
                {
                    this.Destroy();
                }
                return; //give at least 1 tick for creatures to catch up
            }

            if (viewedTime >= MinTimeUntilUnlock) //don't check for creatures until MinTimeUntilUnlock
            {
                //search for a valid creature within the room
                bool creaturesInRoom = false;
                foreach (AbstractCreature crit in room.abstractRoom.creatures)
                {
                    if (crit.spawnDen.room == room.abstractRoom.index //spawned in THIS ROOM
                            && crit.state.alive //and is NOT dead
                            && (!crit.InDen || crit.remainInDenCounter < 80))
                    {
                        creaturesInRoom = true;
                        break;
                    }
                }

                if (!creaturesInRoom)
                {
                    UnlockShortcuts();
                }
            }

        }

        private void LockShortcuts()
        {
            locked = true;

            //add void chains
            chains = new(room.shortcuts.Length);
            foreach (ShortcutData shortcut in room.shortcuts)
            {
                if (shortcut.shortCutType == ShortcutData.Type.RoomExit && room.abstractRoom.connections[shortcut.destNode] >= 0)
                {
                    Vector2 pos = room.MiddleOfTile(shortcut.StartTile) + room.ShorcutEntranceHoleDirection(shortcut.StartTile).ToVector2() * 15f;
                    VoidChain c = new(room, pos, pos);
                    room.AddObject(c);
                    chains.Add(c);
                }
            }
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Lock, 0f, 1f, UnityEngine.Random.value * 0.5f + 0.8f);

            //actually lock shortcuts
            if (room.lockedShortcuts == null)
                room.lockedShortcuts = new(room.shortcutsIndex.Length);
            else
                room.lockedShortcuts.Clear();

            foreach (IntVector2 shortcutPos in room.shortcutsIndex)
                room.lockedShortcuts.Add(shortcutPos);

            Plugin.Log("Locked shortcuts in room " + room.abstractRoom.name);
        }

        private void UnlockShortcuts()
        {
            locked = false;

            //unlock the shortcuts!
            room.lockedShortcuts.Clear();

            //chain break sounds
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, UnityEngine.Random.value * 0.5f + 0.95f);
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Break, 0f, 1f, UnityEngine.Random.value * 0.5f + 0.95f);

            //remove the void chains
            foreach (VoidChain c in chains)
            {
                c.RemoveFromRoom();
                c.Destroy();
            }

            //destroy myself
            this.Destroy();

            Plugin.Log("Unlocked shortcuts in room " + room.abstractRoom.name);
        }

    }
}
