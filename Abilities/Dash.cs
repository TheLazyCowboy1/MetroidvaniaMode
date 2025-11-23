using RWCustom;
using System;
using UnityEngine;

namespace MetroidvaniaMode.Abilities;

public static class Dash
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
            if (CurrentAbilities.DashCount <= 0 || self.playerState.playerNumber < 0) return; //don't run dash code if we can't dash!

            PlayerInfo info = self.GetInfo();

            //cooldown
            if (info.DashCooldown > 0)
                info.DashCooldown--;
            if (info.WantToDash > 0)
                info.WantToDash--;

            bool keyPressed = Tools.Keybinds.IsPressed(Tools.Keybinds.DASH_ID, self.playerState.playerNumber)
                || (Options.PressJumpToDash && self.wantToJump > 0 && self.canJump < 1 && info.ExtraJumpsLeft < 1);

            if (keyPressed && !info.DashHeld) //dash is NEWLY pressed
            {
                info.WantToDash = Options.InputBuffering + 1; //set that we want to dash
            }
            info.DashHeld = keyPressed;

            //The dash button is being pressed
            if (info.WantToDash > 0)
            {
                if (info.DashesLeft < 1 || info.DashCooldown > 0)
                    return; //don't dash when on cooldown!

                //pick dash direction vector
                Player.InputPackage input = self.input[0];
                Vector2 dir;
                if (input.analogueDir.sqrMagnitude > 0.02f)
                    dir = input.analogueDir.normalized;
                else if (input.x != 0 || input.y != 0)
                    dir = new Vector2(input.x, input.y).normalized; //this shouldn't actually be possible to run; but it's here just in case
                else
                    return; //there is no dash direction, so don't use up the dash

                dir.y = Mathf.Min(1, dir.y + 0.15f); //add a little extra upwards speed to help counteract gravity

                //actually move the player
                self.bodyChunks[0].vel = Vector2.LerpUnclamped(self.bodyChunks[0].vel, dir * CurrentAbilities.DashSpeed, CurrentAbilities.DashStrength);
                self.bodyChunks[1].vel = Vector2.LerpUnclamped(self.bodyChunks[1].vel, dir * CurrentAbilities.DashSpeed, CurrentAbilities.DashStrength * 0.95f); //dash is weaker for tail
                self.canJump = 0; //don't double-jump!
                self.wantToJump = 0;

                //sounds
                self.room.PlaySound(SoundID.Slugcat_Throw_Rock, self.mainBodyChunk, false, 1f, 0.9f);

                //particles
                for (int i = 0; i < 12; i++)
                {
                    Spark spark = new Spark(self.mainBodyChunk.pos + Custom.RNV() * 10f - self.mainBodyChunk.vel * 3f, self.mainBodyChunk.vel * 0.6f, new(0.8f, 0.8f, 0.9f), null, 15, 20);
                    spark.gravity *= 0.4f; //half the variation between extremes
                    spark.gravity += 0.1f; //gravity is in range 0.26 to 0.46
                    self.room.AddObject(spark);
                }

                //info.DashedSincePress = true;
                info.DashesLeft--;
                info.DashCooldown = Options.DashCooldown;
                info.WantToDash = 0;

                Plugin.Log("Dashed!", 2);
            }
            //else //we're no longer trying to dash, so we can make dashing available again
                //info.DashedSincePress = false;

            //Refresh dash
            if ((self.canJump > 1 || (CurrentAbilities.WallDashReset && self.canJump > 0)) //don't refresh dashes on wall, unless we have that ability
                && (CurrentAbilities.ClimbVerticalPoles || self.animation != Player.AnimationIndex.ClimbOnBeam)) //don't refresh dashes on poles, unless we can climb
            {
                info.DashesLeft = CurrentAbilities.DashCount;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
