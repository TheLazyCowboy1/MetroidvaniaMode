using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Glide
{
    private static Hook PlayerGravHook;
    public static void ApplyHooks()
    {
        On.Player.MovementUpdate += Player_MovementUpdate;
        try
        {
            PlayerGravHook = new(typeof(Player).GetProperty(nameof(Player.EffectiveRoomGravity)).GetGetMethod(), Player_EffectiveRoomGravity);
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    public static void RemoveHooks()
    {
        On.Player.MovementUpdate -= Player_MovementUpdate;
        PlayerGravHook?.Undo();
    }


    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        try
        {
            if (!CurrentAbilities.CanGlide)
                return;

            PlayerInfo info = self.GetInfo();

            //check if we should stop gliding
            if (info.Gliding && (!self.input[0].jmp || self.canJump > 0))
            {
                info.Gliding = false; //stop gliding if we're not holding jump, or if we regain our ability to jump
            }

            //check if we should start gliding
            if (!info.Gliding && self.wantToJump > 0 && self.canJump <= 0 && self.input[0].jmp
                && info.ExtraJumpsLeft <= 0 && (!Options.PressJumpToDash || info.DashesLeft <= 0)) //don't interrupt double-jumps
            {
                info.Gliding = true; //start gliding
            }

            if (info.Gliding)
            {
                //input
                Vector2 dir;
                if (self.input[0].gamePad)
                    dir = self.input[0].analogueDir; //NOT normalized
                else //keyboard logic
                {
                    if (self.input[0].x == 0)
                        dir = new Vector2(0, self.input[0].y);
                    else
                        dir = new Vector2(self.input[0].x, self.input[0].y * Options.GlideKeyboardYFac).normalized; //multiply y by 0.5
                }

                //glide physics
                foreach (BodyChunk chunk in self.bodyChunks)
                {

                    //add thrust lol
                    chunk.vel += dir * Options.GlideThrust;

                    //physics-based approach v2

                    //calc drag
                    Vector2 nVel = chunk.vel.normalized;
                    Vector2 dragDir;
                    if (Options.EasierGlideMode && self.EffectiveRoomGravity > 0) //don't apply in 0-g
                    {
                        //shift dragDir to feel more natural to fly with.
                        //shift dir down slightly (so that the slugcat normally moves forward)
                        Vector2 dir2 = dir + new Vector2(0, Options.GlideBaseDirY);
                        if (dir2.sqrMagnitude > 0.0001f) //don't let it explode by dividing by 0
                            dir2 *= Mathf.Sqrt(dir.sqrMagnitude / dir2.sqrMagnitude); //set dir2's magnitude to dir1's
                        dragDir = Perpendicular(dir2);
                        //if dragDir.magnitude < 1, lerp it towards (0, 1)
                        if (nVel.y < 0) //don't stop upwards speed, though
                            dragDir = Vector2.LerpUnclamped(new(0, 1), dragDir, dragDir.magnitude);
                        //if dir is up and vel is down, lerp dragDir up
                        if (nVel.y < 0 && dir.y > 0)
                            dragDir = Vector2.LerpUnclamped(dragDir, -nVel, dir.y * -nVel.y);
                        else if (nVel.y > 0 && dir.y > 0) //don't slow me down when trying to go upwards (e.g: double jump)
                            dragDir.y *= nVel.y * (1 - dir.y);

                            //normalize dragDir
                            dragDir.Normalize();
                    }
                    else
                        dragDir = Perpendicular(dir); //this is the proper one, but not easy to fly with

                    float dragFac = -(dragDir.x * nVel.x + dragDir.y * nVel.y) * Options.GlideDragCoef; 
                    Vector2 drag = (dragFac < 0 ? -dragDir : dragDir) * (chunk.vel * dragFac).sqrMagnitude;

                    //apply drag
                    if (drag.sqrMagnitude > chunk.vel.sqrMagnitude)
                        chunk.vel = new(0, 0); //don't let drag exceed velocity
                    else
                        chunk.vel += drag;

                    //calc lift
                    nVel = chunk.vel.normalized;
                    dragDir = Perpendicular(dir); //use the proper calculation for lift
                    float liftFac = -(dragDir.x * nVel.x + dragDir.y * nVel.y) * Options.GlideLiftCoef;
                    Vector2 lift = Perpendicular(chunk.vel) * liftFac;
                    lift *= lift.sqrMagnitude;

                    //apply lift
                    if (lift.sqrMagnitude > chunk.vel.sqrMagnitude * Options.GlideMaxLift * Options.GlideMaxLift)
                        lift = Vector2.ClampMagnitude(lift, chunk.vel.magnitude * Options.GlideMaxLift); //don't let velocity go supersonic
                    chunk.vel += lift;

                    //apply drag in all directions to prevent supersonic explosions
                    nVel = chunk.vel.normalized;
                    Vector2 omniDrag = -nVel * (chunk.vel * Options.GlideOmniDragCoef).sqrMagnitude;
                    if (omniDrag.sqrMagnitude > chunk.vel.sqrMagnitude)
                        chunk.vel = new(0, 0); //don't let drag exceed velocity
                    else
                        chunk.vel += omniDrag;

                }

                //lower gravity
                float antiGrav = Options.GlideAntiGrav * Mathf.Clamp01(1 + dir.y);
                self.customPlayerGravity = BaseCustomPlayerGravity * (1f - Options.GlideAntiGrav);

                //appearance
                if (self.EffectiveRoomGravity > 0)
                {
                    if (self.mainBodyChunk.vel.y <= Mathf.Abs(self.mainBodyChunk.vel.x))
                    {
                        self.standing = false;
                        self.animation = Player.AnimationIndex.DownOnFours;
                        if (self.flipDirection == 0)
                            self.flipDirection = (int)Mathf.Sign(self.mainBodyChunk.vel.x); //try to set flip direction so slugcat actually goes down
                    }
                    else //going upwards (going more up than left/right)
                    {
                        self.standing = true;
                        self.animation = Player.AnimationIndex.None;
                    }
                    self.bodyMode = Player.BodyModeIndex.Default;
                }
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x); //made as my own function so I know it's correct

    private static float YSlowdown(float y, float b) => (y < 0) ? (-y * y) / (-y + b) : (y * y) / (y + b);

    private const float BaseCustomPlayerGravity = 0.9f;
    private static float Player_EffectiveRoomGravity(Func<Player, float> orig, Player self)
    {
        if (!CurrentAbilities.CanGlide) return orig(self);

        return orig(self) * self.customPlayerGravity / BaseCustomPlayerGravity; //apply customPlayerGravity
    }
}
