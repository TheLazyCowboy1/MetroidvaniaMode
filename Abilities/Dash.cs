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
            if (!Options.CanDash) return;

            if (Input.GetKeyDown(Options.DashKeyCode))
            {
                PlayerInfo info = self.GetInfo();
                if (!info.DashCooldown)
                {
                    Player.InputPackage i = self.input[0];
                    Vector2 dir;
                    if (i.analogueDir.sqrMagnitude > 0.02)
                        dir = i.analogueDir.normalized;
                    else if (i.x != 0 || i.y != 0)
                        dir = new Vector2(i.x, i.y).normalized;
                    else
                        return; //there is no dash direction, so don't use up the dash

                    dir.y = Mathf.Min(1, dir.y + 0.15f); //add a little extra upwards speed to help counteract gravity

                    self.bodyChunks[0].vel = Vector2.LerpUnclamped(self.bodyChunks[0].vel, dir * Options.DashSpeed, Options.DashStrength);
                    self.bodyChunks[1].vel = Vector2.LerpUnclamped(self.bodyChunks[1].vel, dir * Options.DashSpeed, Options.DashStrength * 0.85f); //dash is weaker for tail
                    self.canJump = 0; //don't double-jump!

                    info.DashCooldown = true;
                    Plugin.Log("Dashed!");
                }
            }
            else if (self.canJump > 1 && (Options.ClimbVerticalPoles || self.animation != Player.AnimationIndex.ClimbOnBeam))
            {
                self.GetInfo().DashCooldown = false;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }
}
