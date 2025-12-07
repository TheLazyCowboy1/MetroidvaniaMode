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
            if (CurrentAbilities.ExtraJumps <= 0) return; //don't even run this code if it doesn't apply!

            if (self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Swimming //don't jump while swimming
                && (self.wantToJump > 0 //usually, check wantToJump. However, sometimes things like flips make wantToJump always 0
                    || (self.jumpBoost <= 0 && self.input[0].jmp && !self.input[1].jmp //just pressed jump
                        && self.animation != Player.AnimationIndex.None))) //special check only applies if we're not in the None/default animation
            {
                PlayerInfo info = self.GetInfo();
                if (info.ExtraJumpsLeft > 0)
                {
                    if (self.EffectiveRoomGravity > 0) //better jumps...?
                    {
                        self.animation = Player.AnimationIndex.None;
                        self.bodyMode = Player.BodyModeIndex.Default;
                        self.standing = true;
                    }
                    self.Jump();
                    self.wantToJump = 0;
                    self.canJump = 0;
                    info.ExtraJumpsLeft--;

                    if (CurrentAbilities.CanGlide)
                        info.Gliding = true; //start gliding immediately

                    //wings flap
                    if (info.Wings != null && info.Wings.NeedsDestroy)
                        info.Wings.Destroy();

                    if (info.Wings == null)
                    {
                        info.Wings = new(self, info);
                        self.room.AddObject(info.Wings);
                        Plugin.Log("Added PlayerWings", 2);
                    }
                    info.Wings?.Flap();

                    Plugin.Log("Doubled jumped", 2);
                }
            }
            else if (self.canJump > 1 || (CurrentAbilities.WallDashReset && self.canJump > 0))
            {
                self.GetInfo().ExtraJumpsLeft = CurrentAbilities.ExtraJumps;
            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

}
