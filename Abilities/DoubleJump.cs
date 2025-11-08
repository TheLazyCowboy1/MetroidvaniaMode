using System;

namespace MetroidvaniaMode.Abilities;

public static class DoubleJump
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
            if (Options.ExtraJumps <= 0) return; //don't even run this code if it doesn't apply!

            if (self.wantToJump > 0 && self.canJump <= 0)
            {
                PlayerInfo info = self.GetInfo();
                if (info.ExtraJumpsLeft > 0)
                {
                    self.Jump();
                    self.wantToJump = 0;
                    self.canJump = 0;
                    info.ExtraJumpsLeft--;
                }
            }
            else if (self.canJump > 1)
            {
                self.GetInfo().ExtraJumpsLeft = Options.ExtraJumps;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

}
