using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode;

public static class ArenaRoomFix
{
    public static void ApplyHooks()
    {
        On.Room.TriggerCombatArena += Room_TriggerCombatArena;
    }

    public static void RemoveHooks()
    {
        On.Room.TriggerCombatArena -= Room_TriggerCombatArena;
    }


    //Trigger combat arena properly!
    private static void Room_TriggerCombatArena(On.Room.orig_TriggerCombatArena orig, Room self)
    {
        orig(self);

        self.AddObject(new DoorLocker(null, self));
        Plugin.Log("Added DoorLocker to room " + self.abstractRoom.name);
    }

    private class DoorLocker : PhysicalObject
    {
        private List<VoidChain> chains;

        public DoorLocker(AbstractPhysicalObject abstractPhysicalObject, Room room) : base(abstractPhysicalObject)
        {
            //make it have no collision, like HRGuardManager
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(-5000f, -5000f), 0f, 0f);
            base.bodyChunks[0].collideWithTerrain = false;
            base.bodyChunks[0].collideWithSlopes = false;
            base.bodyChunks[0].collideWithObjects = false;
            base.bodyChunks[0].restrictInRoomRange = 10000f;
            base.bodyChunks[0].defaultRestrictInRoomRange = 10000f;
            base.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0f;
            base.gravity = 0f;
            base.bounce = 0f;
            base.surfaceFriction = 0f;
            base.collisionLayer = 0;
            base.waterFriction = 0f;
            base.buoyancy = 0f;

            //this.room = room;

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
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Chain_Lock, 0f, 1f, global::UnityEngine.Random.value * 0.5f + 0.8f);

            //actually lock shortcuts
            if (room.lockedShortcuts == null)
                room.lockedShortcuts = new(room.shortcutsIndex.Length);
            else
                room.lockedShortcuts.Clear();

            foreach (IntVector2 shortcutPos in room.shortcutsIndex)
                room.lockedShortcuts.Add(shortcutPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            bool creaturesInRoom = false;
            foreach (List<PhysicalObject> objList in room.physicalObjects)
            {
                foreach (PhysicalObject obj in objList)
                {
                    if (obj is Creature crit && crit.abstractCreature.spawnDen.room == room.abstractRoom.index //spawned in THIS ROOM
                        && !crit.dead) //and is NOT dead
                    {
                        creaturesInRoom = true;
                        break;
                    }
                }
                if (creaturesInRoom)
                    break;
            }

            if (!creaturesInRoom)
            {
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
            }
        }

    }
}
