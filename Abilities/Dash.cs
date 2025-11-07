using RWCustom;
using Smoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            if (!Options.CanDash) return; //don't run dash code if we can't dash!

            if (Input.GetKeyDown(Options.DashKeyCode))
            {
                PlayerInfo info = self.GetInfo();
                if (info.DashCooldown) return; //don't dash when on cooldown!

                //pick dash direction vector
                Player.InputPackage input = self.input[0];
                Vector2 dir;
                if (input.analogueDir.sqrMagnitude > 0.02)
                    dir = input.analogueDir.normalized;
                else if (input.x != 0 || input.y != 0)
                    dir = new Vector2(input.x, input.y).normalized;
                else
                    return; //there is no dash direction, so don't use up the dash

                dir.y = Mathf.Min(1, dir.y + 0.15f); //add a little extra upwards speed to help counteract gravity

                //actually move the player
                self.bodyChunks[0].vel = Vector2.LerpUnclamped(self.bodyChunks[0].vel, dir * Options.DashSpeed, Options.DashStrength);
                self.bodyChunks[1].vel = Vector2.LerpUnclamped(self.bodyChunks[1].vel, dir * Options.DashSpeed, Options.DashStrength * 0.85f); //dash is weaker for tail
                self.canJump = 0; //don't double-jump!

                //sounds
                self.room.PlaySound(SoundID.Slugcat_Throw_Rock, self.mainBodyChunk, false, 1f, 0.9f);

                //particles
                for (int i = 0; i < 12; i++)
                    self.room.AddObject(new Spark(self.mainBodyChunk.pos + Custom.RNV() * 10f - self.mainBodyChunk.vel * 3f, self.mainBodyChunk.vel * 0.6f, new(0.8f, 0.8f, 0.9f), null, 15, 20));

                info.DashCooldown = true;
                Plugin.Log("Dashed!");
            }
            else if (self.canJump > 1 && (Options.ClimbVerticalPoles || self.animation != Player.AnimationIndex.ClimbOnBeam))
            {
                self.GetInfo().DashCooldown = false;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
