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

    private static SteamSmoke smoke = null;

    private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
    {
        orig(self, eu);

        try
        {
            if (!Options.CanDash) return;

            PlayerInfo info = self.GetInfo();

            //dash sound loop volume
            if (info.DashSoundLoop != null && info.DashSoundLoop.Volume > 0)
            {
                info.DashSoundLoop.Update();
                info.DashSoundLoop.Volume -= 1f / 40f; //one second sound loop
                if (info.DashSoundLoop.Volume <= 0)
                {
                    info.DashSoundLoop.sound = SoundID.None;
                    info.DashSoundLoop.Volume = 0;
                }
            }

            if (Input.GetKeyDown(Options.DashKeyCode) && !info.DashCooldown)
            {
                Player.InputPackage input = self.input[0];
                Vector2 dir;
                if (input.analogueDir.sqrMagnitude > 0.02)
                    dir = input.analogueDir.normalized;
                else if (input.x != 0 || input.y != 0)
                    dir = new Vector2(input.x, input.y).normalized;
                else
                    return; //there is no dash direction, so don't use up the dash

                dir.y = Mathf.Min(1, dir.y + 0.15f); //add a little extra upwards speed to help counteract gravity

                self.bodyChunks[0].vel = Vector2.LerpUnclamped(self.bodyChunks[0].vel, dir * Options.DashSpeed, Options.DashStrength);
                self.bodyChunks[1].vel = Vector2.LerpUnclamped(self.bodyChunks[1].vel, dir * Options.DashSpeed, Options.DashStrength * 0.85f); //dash is weaker for tail
                self.canJump = 0; //don't double-jump!

                //sounds
                info.DashSoundLoop ??= new(self.mainBodyChunk);
                info.DashSoundLoop.sound = SoundID.Rock_Through_Air_LOOP;
                info.DashSoundLoop.Volume = 1;

                //particles
                if (smoke == null || smoke.room != self.room)
                {
                    smoke?.RemoveFromRoom();
                    smoke?.Destroy();
                    smoke = new SteamSmoke(self.room);
                    self.room.AddObject(smoke);
                }
                Vector2 n = self.mainBodyChunk.vel.normalized;
                Vector2 pos = self.mainBodyChunk.pos, corner1 = pos - new Vector2(200, 200) + n * 150, corner2 = pos + new Vector2(200, 200) + n * 150;
                FloatRect confines = new(corner1.x, corner1.y, corner2.x, corner2.y);
                for (int i = 0; i < 20; i++)
                    smoke.EmitSmoke(pos, self.mainBodyChunk.vel, confines, 0.3f);

                info.DashCooldown = true;
                Plugin.Log("Dashed!");
            }
            else if (self.canJump > 1 && (Options.ClimbVerticalPoles || self.animation != Player.AnimationIndex.ClimbOnBeam))
            {
                info.DashCooldown = false;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
