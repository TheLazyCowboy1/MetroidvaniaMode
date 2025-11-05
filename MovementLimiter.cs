using System;
using UnityEngine;

namespace MetroidvaniaMode;

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
        On.Player.MovementUpdate += Player_MovementUpdate;

        //prevent using normal shortcuts
        On.Player.Update += Player_Update;
    }
    public static void RemoveHooks()
    {
        On.Player.Jump -= Player_Jump;
        On.Player.WallJump -= Player_WallJump;

        On.Player.UpdateBodyMode -= Player_UpdateBodyMode;

        On.Player.UpdateAnimation -= Player_UpdateAnimation;

        On.Player.MovementUpdate -= Player_MovementUpdate;

        On.Player.Update -= Player_Update;
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
                && self.room.shortcutData(self.mainBodyChunk.pos + new Vector2(0, 10)).shortCutType == ShortcutData.Type.DeadEnd //can climb if shortcut above
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
                && self.input[0].y > 0)
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
    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        if (!Options.CanGrabPoles && self.noGrabCounter <= 0)
            self.noGrabCounter = 1;

        orig(self, eu);
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

}
