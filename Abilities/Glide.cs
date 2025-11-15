using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
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
                Vector2 dir = self.input[0].analogueDir; //NOT normalized
                if (dir.sqrMagnitude < 0.01f)
                    dir = self.input[0].IntVec.ToVector2().normalized;

                //glide physics
                foreach (BodyChunk chunk in self.bodyChunks)
                {
                    //aggressively slow down y-speed
                    /*if (chunk.vel.y < 0) //don't slow down going upwards just yet
                        chunk.vel.y -= YSlowdown(chunk.vel.y, Options.GlideSlowdownVar) * Mathf.Clamp01(1 + dir.y); //if y is straight down; just plummet

                    //Vector2 vel = chunk.vel; //save it separately

                    //convert x-speed into y-speed
                    //float yConvert = Options.GlideMaxConversion * Mathf.Clamp01(0.5f + 0.5f * (dir.y - dir.x * Mathf.Sign(vel.x)));
                    float yConvert = Options.GlideMaxYConversion * Mathf.Clamp01(dir.y);
                    chunk.vel.y += Mathf.Abs(chunk.vel.x) * yConvert * Options.GlideYConversionEfficiency;
                    chunk.vel.x -= chunk.vel.x * yConvert;

                    //convert y-speed into x-speed
                    //float xConvert = Options.GlideMaxConversion * Mathf.Clamp01(0.5f + 0.5f * (dir.x - dir.y * Mathf.Sign(vel.y)));
                    float xConvert = Options.GlideMaxXConversion * Mathf.Clamp01(dir.x * Mathf.Sign(chunk.vel.x));
                    chunk.vel.x += Mathf.Abs(chunk.vel.y) * Mathf.Sign(chunk.vel.x) * xConvert * Options.GlideXConversionEfficiency;
                    chunk.vel.y -= chunk.vel.y * xConvert;
                    */

                    //add thrust lol
                    chunk.vel += dir * Options.GlideThrust;

                    //physics-based approach v2

                    //calc drag
                    Vector2 nVel = chunk.vel.normalized;
                    Vector2 dragDir = Perpendicular(dir); //this is the proper one, but not easy to fly with
                    //shift dragDir to feel more natural to fly with.
                    //shift dragDir down slightly (so that the slugcat normally moves forward)
                    dragDir.y += Options.GlideBaseDirY;
                    //if dragDir.magnitude < 1, lerp it towards (0, 1)
                    dragDir = Vector2.LerpUnclamped(new(0, 1), dragDir, dragDir.magnitude);
                    //if dir is up and vel is down, lerp dragDir up
                    if (nVel.y < 0)
                        dragDir = Vector2.LerpUnclamped(dragDir, new(nVel.x, nVel.y*nVel.y), dir.y*dir.y);
                    //normalize dragDir
                    dragDir.Normalize();

                    float dragFac = (dragDir.x * nVel.x + dragDir.y * nVel.y) * Options.GlideDragCoef; 
                    Vector2 drag = (dragDir.y > 0 ? dragDir : -dragDir) * (chunk.vel * dragFac).sqrMagnitude;

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
                self.standing = false;
                //if (self.input[0].y > 0) //prevent trying to stand up?
                    //self.input[0].y = 0;
                self.animation = Player.AnimationIndex.DownOnFours;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
    private static Vector2 Perpendicular(Vector2 v) => new Vector2(-v.y, v.x); //made as my own function so I know it's correct

    private static float YSlowdown(float y, float b) => (y < 0) ? (-y * y) / (-y + b) : (y * y) / (y + b);

    private const float BaseCustomPlayerGravity = 0.9f;
    private static float Player_EffectiveRoomGravity(Func<Player, float> orig, Player self)
    {
        if (!CurrentAbilities.CanGlide) return orig(self);

        return orig(self) * self.customPlayerGravity / BaseCustomPlayerGravity; //apply customPlayerGravity
    }
}
