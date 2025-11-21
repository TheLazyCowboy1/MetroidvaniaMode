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
        On.AbstractRoom.AttractionForCreature_AbstractCreature += AbstractRoom_AttractionForCreature_AbstractCreature;
    }

    public static void RemoveHooks()
    {
        On.Room.NowViewed -= Room_NowViewed;
        //On.Room.TriggerCombatArena -= Room_TriggerCombatArena;
        On.AbstractRoom.AttractionForCreature_AbstractCreature -= AbstractRoom_AttractionForCreature_AbstractCreature;
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
                self.AddObject(new DoorLocker());
                //self.AddObject(new DoorLocker(new(self.world, new("FakeAbstractDoorLocker", false), null, new(self.abstractRoom.index, 0, 0, -1), self.game.GetNewID())));
            }
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    //Prevent creatures from moving into battle arenas
    private static AbstractRoom.CreatureRoomAttraction AbstractRoom_AttractionForCreature_AbstractCreature(On.AbstractRoom.orig_AttractionForCreature_AbstractCreature orig, AbstractRoom self, AbstractCreature creature)
    {
        try
        {
            if (self.isBattleArena || DoorLocker.LockedRooms.Contains(self.index)) //if it will be locked, or if it is locked
            {
                if (creature.spawnDen.room == self.index)
                    return AbstractRoom.CreatureRoomAttraction.Stay; //creatures who spawned in the room must stay in the room
                else
                    return AbstractRoom.CreatureRoomAttraction.Forbidden; //other creatures are forbidden
            }
            //prevent arena creatures from moving into neighboring rooms
            if (DoorLocker.LockedRooms.Contains(creature.spawnDen.room))
                return AbstractRoom.CreatureRoomAttraction.Forbidden;

        } catch (Exception ex) { Plugin.Error(ex); }

        return orig(self, creature);
    }


    private class DoorLocker : UpdatableAndDeletable
    {
        public static List<int> LockedRooms = new();
        private int roomIdx = -123;

        private const int TimeUntilLock = 40 * 3; //3 seconds until lock
        private const int MinTimeUntilUnlock = TimeUntilLock + 40 * 3; //must be locked for at least 3 seconds

        private List<VoidChain> chains;
        private int viewedTime = 0;
        private bool locked = false;

        public DoorLocker() : base()
        {
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (room == null || (!room.BeingViewed && !locked)) //the player isn't in the room; don't lock the shortcuts
            {
                this.Destroy();
                return;
            }

            if (roomIdx == -123)
            {
                roomIdx = room.abstractRoom.index;
            }

            //make creatures stay in room (or leave if not allowed)
            foreach (AbstractCreature crit in room.abstractRoom.creatures)
            {
                if (crit.spawnDen.room == roomIdx) //creatures who belong in the room
                {
                    if (crit.abstractAI != null && crit.abstractAI.destination.room != roomIdx) //GET BACK IN HERE!!!
                    {
                        WorldCoordinate newDest = new(roomIdx, room.abstractRoom.size.x / 2, room.abstractRoom.size.y / 2, -1);
                        crit.abstractAI.SetDestinationNoPathing(newDest, true); //stay here whether you like it or not
                        crit.abstractAI.freezeDestination = true; //don't try to change your destination ever again
                    }
                }
                else //creature who do not belong in the room
                {
                    if (crit.abstractAI != null && crit.abstractAI.destination.room == roomIdx) //GET OUT!!!
                    {
                        for (int i = 0; i < room.abstractRoom.connections.Length; i++)
                        {
                            int conn = room.abstractRoom.connections[i];
                            if (conn >= 0)
                            {
                                crit.abstractAI.SetDestinationNoPathing(new(conn, -1, -1, crit.world.GetAbstractRoom(conn).ExitIndex(roomIdx)), true);
                                break;
                            }
                        }
                    }
                }
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
                    if (crit.spawnDen.room == roomIdx //spawned in THIS ROOM
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

            LockedRooms.Add(roomIdx);

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

        public override void Destroy()
        {
            LockedRooms.Remove(roomIdx);

            base.Destroy();
        }

    }
}
