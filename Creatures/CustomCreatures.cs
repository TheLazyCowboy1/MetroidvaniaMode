using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetroidvaniaMode.Creatures;

public static class CustomCreatures
{

    public static void ApplyHooks()
    {
        On.StaticWorld.InitCustomTemplates += StaticWorld_InitCustomTemplates;
        On.Lizard.FollowConnection += Lizard_FollowConnection;

        IL.Lizard.Act += Lizard_Act;

        On.LizardGraphics.Update += LizardGraphics_Update;
    }

    public static void RemoveHooks()
    {
        On.StaticWorld.InitCustomTemplates -= StaticWorld_InitCustomTemplates;
        On.Lizard.FollowConnection -= Lizard_FollowConnection;

        IL.Lizard.Act -= Lizard_Act;

        On.LizardGraphics.Update -= LizardGraphics_Update;
    }

    private static void LizardGraphics_Update(On.LizardGraphics.orig_Update orig, LizardGraphics self)
    {
        orig(self);

        self.depthRotation = self.lastDepthRotation = self.headDepthRotation = self.lastHeadDepthRotation = 0;
    }

    /// <summary>
    /// Temporarily just adjusts pink lizards to think they can fly
    /// </summary>
    /// <param name="orig"></param>
    private static void StaticWorld_InitCustomTemplates(On.StaticWorld.orig_InitCustomTemplates orig)
    {
        orig();

        try
        {
            /*CreatureTemplate liz = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.YellowLizard);
            CreatureTemplate fly = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly);
            liz.doPreBakedPathing = false;
            liz.preBakedPathingAncestor = fly;
            liz.canFly = true;
            liz.pathingPreferencesTiles[(int)AItile.Accessibility.Air] = new(0.9f, PathCost.Legality.Allowed); //make air allowed...?
            */

            foreach (CreatureTemplate temp in StaticWorld.creatureTemplates)
            {
                if (temp.IsLizard)
                {
                    temp.canFly = true; //I don't actually know what this does...
                    temp.pathingPreferencesTiles[(int)AItile.Accessibility.Air] = new(1.1f, PathCost.Legality.Allowed);
                }
            }

            Plugin.Log("Made lizards think they can fly", 0);
        } catch (Exception ex) { Plugin.Error(ex); }
    }


    private static void Lizard_FollowConnection(On.Lizard.orig_FollowConnection orig, Lizard self, float runSpeed)
    {
        try
        {
            /*if (self.Template.type == CreatureTemplate.Type.YellowLizard)
            {
                switch (self.followingConnection.type)
                {
                    case MovementConnection.MovementType.Standard:
                    case MovementConnection.MovementType.Slope:
                    case MovementConnection.MovementType.CeilingSlope:
                    case MovementConnection.MovementType.OpenDiagonal:
                    case MovementConnection.MovementType.DropToFloor:
                    case MovementConnection.MovementType.DropToClimb:
                        break; //run orig
                    default:
                        Vector2 moveVec = RWCustom.Custom.DirVec(self.room.MiddleOfTile(self.followingConnection.DestTile), self.bodyChunks[0].pos)
                            * self.lizardParams.baseSpeed * self.BodyForce;
                        self.bodyChunks[0].vel += moveVec;
                        self.bodyChunks[1].vel += moveVec;
                        return; //don't run orig
                }
            }*/
            //decrease gravity for easier flying...?
            //self.SetLocalGravity(Mathf.Min(self.GetLocalGravity(), 0.4f));

            Vector2 moveVec = RWCustom.Custom.DirVec(self.bodyChunks[0].pos, self.room.MiddleOfTile(self.followingConnection.DestTile))
                * self.lizardParams.baseSpeed * self.BodyForce * 0.5f;

            if (moveVec.y < 0) moveVec.y *= 0.25f; //don't go down as much

            self.bodyChunks[0].vel *= 0.8f; //heavy drag
            self.bodyChunks[1].vel *= 0.8f;
            self.bodyChunks[0].vel += moveVec;
            self.bodyChunks[1].vel += moveVec + new Vector2(0, 1f); //pull upward more
            for (int i = 2; i < self.bodyChunks.Length; i++)
            {
                self.bodyChunks[i].vel = 0.9f * self.bodyChunks[i].vel + moveVec * 0.5f; //tail gets moved much less than body
            }

            //align bodychunks horizontally
            for (int i = 1; i < self.bodyChunks.Length; i++)
            {
                float pullStrength = 0.5f + 0.5f * i; //chunk1 = 1, chunk2 = 1.5
                float sign1 = Mathf.Sign(self.bodyChunks[0].vel.x); //desired direction (e.g: want chunk0 to be right)
                float sign2 = Mathf.Sign(self.bodyChunks[0].pos.x - self.bodyChunks[i].pos.x); //actual direction (e.g: chunk0 is left)
                if (sign1 != sign2)
                    self.bodyChunks[i].vel.x -= 1f * sign1; //move opposite direction to make lizard more horizontal
            }

            //dangle limbs
            if (self.graphicsModule is LizardGraphics graph)
            {
                foreach (LizardLimb limb in graph.limbs)
                {
                    limb.mode = LizardLimb.Mode.Dangle;
                }
            }

            return; //don't run orig
        } catch (Exception ex) { Plugin.Error(ex); }

        orig(self, runSpeed);
    }


    private static void Lizard_Act(ILContext il)
    {
        try
        {
            ILCursor c = new(il);
            if (c.TryGotoNext(MoveType.Before,
                //x => x.MatchStfld<Lizard>(nameof(Lizard.narrowUpcoming)),
                x => x.MatchLdarg(0),
                x => x.MatchLdflda<Lizard>(nameof(Lizard.gripPoint)),
                x => x.MatchCall(out _), //any call instruction
                x => x.MatchBrfalse(out _) //any brfalse instruction
                ))
            {
                Plugin.Log("Successfully found place in Lizard.Act to insert IL code!", 0);
                c.Emit(OpCodes.Ldarg_0); //reference THIS (the lizard)
                c.EmitDelegate((Lizard self) => {
                    self.gripPoint = null; //just always remove the grip point!!!
                    //we should probably only do this if it is a dragon AND it's flying
                });
            }
            else
                Plugin.Error("Could not find location for Lizard_Act IL hook!");
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

}
