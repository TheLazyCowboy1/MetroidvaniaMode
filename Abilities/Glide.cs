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
    public static void ApplyHooks()
    {
        On.Player.MovementUpdate += Player_MovementUpdate;
    }

    public static void RemoveHooks()
    {
        On.Player.MovementUpdate -= Player_MovementUpdate;
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
            if (info.Gliding && !self.input[0].jmp)
            {
                info.Gliding = false; //stop gliding if we're not holding jump
            }

            //check if we should start gliding
            if (!info.Gliding && self.wantToJump > 0
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
                    /*
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

                    //input //set each to 1 for proper physics sim
                    float dragXMod = Mathf.Clamp01(0.5f - 0.5f * dir.x * Mathf.Sign(chunk.vel.x)); //full forward => no xDrag; full backward => full xDrag
                    float dragYMod = Mathf.Clamp01(1 + dir.y); //holding down => no yDrag
                    //float liftMod = Mathf.Clamp01(dir.y);
                    float liftMod = new Vector2(dir.x, Mathf.Clamp01(dir.y)).magnitude; //1 most of the time. 0 when neutral or straight down

                    //physics-based formulas

                    //drag
                    float dragX = chunk.vel.x * chunk.vel.x * Options.GlideDragXCoef * dragXMod;
                    if (Mathf.Abs(dragX) > Mathf.Abs(chunk.vel.x))
                        dragX = chunk.vel.x; //drag cannot be greater than velocity

                    float dragY = chunk.vel.y * chunk.vel.y * Options.GlideDragYCoef * dragYMod;
                    if (Mathf.Abs(dragY) > Mathf.Abs(chunk.vel.y))
                        dragY = chunk.vel.y; //drag cannot be greater than velocity
                    if (chunk.vel.y > 0)
                        dragY = 0; //don't implement drag when going upwards

                    //lift
                    Vector2 lift = Vector2.Perpendicular(chunk.vel * chunk.vel.magnitude * Options.GlideLiftCoef * liftMod);
                    if (lift.y < 0)
                        lift = -lift; //ensure the lift doesn't pull us downwards

                    //apply the forces
                    chunk.vel.x -= dragX;
                    chunk.vel.y -= dragY;
                    chunk.vel += lift;

                }

                //lower gravity
                //self.customPlayerGravity = self.EffectiveRoomGravity * 0.25f;

                //appearance
                self.standing = false;
                //if (self.input[0].y > 0) //prevent trying to stand up?
                    //self.input[0].y = 0;
                self.animation = Player.AnimationIndex.DownOnFours;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static float YSlowdown(float y, float b) => (y < 0) ? (-y * y) / (-y + b) : (y * y) / (y + b);
}
