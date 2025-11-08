using System;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class MovementLimiter
{
    public static void ApplyHooks()
    {
        //jump limiters/amplifiers
        On.Player.Jump += Player_Jump;
        On.Player.WallJump += Player_WallJump;

        //climbing up corridor limiter
        On.Player.UpdateBodyMode += Player_UpdateBodyMode;

        //climbing up poles limiter
        On.Player.UpdateAnimation += Player_UpdateAnimation;

        //prevent grabbing poles
        //also change jumpBoostDecrement
        //ALSO disable diving
        On.Player.MovementUpdate += Player_MovementUpdate;

        //prevent using normal shortcuts
        On.Player.Update += Player_Update;

        On.Player.SwimDir += Player_SwimDir;
    }

    public static void RemoveHooks()
    {
        On.Player.Jump -= Player_Jump;
        On.Player.WallJump -= Player_WallJump;

        On.Player.UpdateBodyMode -= Player_UpdateBodyMode;

        On.Player.UpdateAnimation -= Player_UpdateAnimation;

        On.Player.MovementUpdate -= Player_MovementUpdate;

        On.Player.Update -= Player_Update;

        On.Player.SwimDir -= Player_SwimDir;
    }


    //Scales jumpBoost
    //ALSO separately scales jumpBoost when on poles
    private static void Player_Jump(On.Player.orig_Jump orig, Player self)
    {
        bool onPole = self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam;

        orig(self);

        if (self.jumpBoost <= 0 && Options.JumpBoost > 1)
            self.jumpBoost += 12f * (Options.JumpBoost - 1);
        else
            self.jumpBoost *= Options.JumpBoost;

        if (onPole)
            self.jumpBoost *= Options.PoleJumpBoost;
    }

    //Disables or amplifies walljumps
    private static void Player_WallJump(On.Player.orig_WallJump orig, Player self, int direction)
    {
        if (Options.CanWallJump)
        {
            orig(self, direction);

            if (self.jumpBoost <= 0 && Options.JumpBoost > 1)
                self.jumpBoost += 12f * (Options.JumpBoost - 1);
            else
                self.jumpBoost *= Options.JumpBoost;
        }
    }

    //Make the game think that the player isn't pressing up when climbing corridors and running the corridor movement code
    private static void Player_UpdateBodyMode(On.Player.orig_UpdateBodyMode orig, Player self)
    {
        try
        {
            if (!Options.ClimbVerticalCorridors && self.bodyMode == Player.BodyModeIndex.CorridorClimb && self.input[0].y > 0
                && !self.IsTileSolid(0, 0, -1) && !self.room.GetTile(self.bodyChunks[1].pos + new Vector2(0, -13)).IsSolid() //can still climb up if there is terrain beneath
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0, 15)).shortCutType == ShortcutData.Type.DeadEnd //can climb if shortcut above
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0, -20)).shortCutType == ShortcutData.Type.DeadEnd) //can climb if shortcut below
            {
                self.input[0].y = 0;
                orig(self);
                self.input[0].y = 1;
                return;
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        orig(self);
    }

    //Make the game think that the player isn't pressing up when climbing vertical poles (or hanging underneath a vertical one)
    private static void Player_UpdateAnimation(On.Player.orig_UpdateAnimation orig, Player self)
    {
        try
        {
            if (!Options.ClimbVerticalPoles
                && (self.animation == Player.AnimationIndex.ClimbOnBeam || self.animation == Player.AnimationIndex.HangUnderVerticalBeam)
                && self.input[0].y > 0
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0, 35)).shortCutType == ShortcutData.Type.DeadEnd) //can climb if shortcut above
            {
                self.input[0].y = 0;
                orig(self);
                self.input[0].y = 1;
                return;
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        orig(self);
    }

    //Use the noGrabCounter to prevent grabbing poles entirely
    //ALSO separately scales jumpBoost as it is decremented
    //ALSO disables floating on the surface of the water
    //ALSO disables diving down into the water
    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (!Options.CanGrabPoles && self.noGrabCounter <= 0)
            self.noGrabCounter = 1;

        float jb = self.jumpBoost; //track jumpBoost before

        int y = self.input[0].y;
        if (self.bodyMode == Player.BodyModeIndex.Swimming) //swimming prevention
        {
            if (!Options.CanSwim)
            {
                self.input[0].x = 0;
                self.input[0].jmp = false;
                self.input[0].y = -1; //swim down if on surface of water and unable to float
            }
            else if (!Options.CanDive && y < 0)
            {
                self.input[0].y = 0; //do not swim down if unable to dive
            }
        }

        orig(self, eu);

        if (jb > self.jumpBoost)
            self.jumpBoost = jb + (self.jumpBoost - jb) * Options.JumpBoostDecrement;

        self.input[0].y = y; //fix the changed input
    }

    //Prevents the player from using normal shortcuts
    private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
    {
        try
        {
            if (!Options.CanUseShortcuts && self.enteringShortCut != null && self.Consious
                && self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.Normal)
            {
                self.enteringShortCut = null; //nope; no using shortcuts for you!
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        orig(self, eu);
    }

    //Prevents the player from swimming down if unable to dive
    //ALSO mostly disables swimming if unable to swim
    private static Vector2 Player_SwimDir(On.Player.orig_SwimDir orig, Player self, bool normalize)
    {
        if (!Options.CanSwim)
        {
            self.swimCycle = 0; //no swimming animation
            return new Vector2(0, -0.3f); //swim down slightly if we can't swim
        }

        Vector2 o = orig(self, normalize);

        if (!Options.CanDive)
            o.y = Mathf.Max(0.05f, o.y); //always swim slightly up if we can't dive

        return o;
    }

}
