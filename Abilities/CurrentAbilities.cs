using MetroidvaniaMode.Collectibles;
using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using System;

namespace MetroidvaniaMode.Abilities;

public static class CurrentAbilities
{
    public static bool CountCollectibles(SlugcatStats.Name slugcat) => slugcat == SlugcatStats.Name.White;


    public static float ExtraRunSpeed = 0;

    public static float JumpBoost = 1;
    public static float PoleJumpBoost = 1;
    public static float JumpBoostDecrement = 1;

    public static bool CanWallJump = true;
    public static bool WallDashReset = false;

    public static bool CanGrabPoles = true;
    public static bool ClimbVerticalPoles = true;

    public static bool ClimbVerticalCorridors = true;

    public static bool CanUseShortcuts = true;

    public static bool CanSwim = true;
    public static bool CanDive = true;

    public static bool CanThrowObjects = true;
    public static bool CanThrowSpears = true;

    public static int DashCount = 0;
    public static float DashSpeed = 12f;
    public static float DashStrength = 0.95f;

    public static int ExtraJumps = 0;

    public static bool CanGlide = false;

    public static bool HasHealth = false;
    public static int MaxHealth = 3;

    public static bool HasInventory = false;

    public static bool AcidImmunity = false;

    public static void ResetAbilities(RainWorldGame game)
    {
        try
        {
            if (!game.IsStorySession)
            {
                OptionsAbilities();
                return;
            }

            BaseAbilities(game.StoryCharacter);

            if (CountCollectibles(game.StoryCharacter))
            {
                WorldSaveData data = game.GetStorySession.saveState.miscWorldSaveData.GetData();

                //blue unlocks

                ExtraRunSpeed += 0.1f * CollectibleTokens.UnlockedCount(data.UnlockedBlueTokens, CollectibleTokens.RunSpeedUnlocks);
                JumpBoost += 0.03f * CollectibleTokens.UnlockedCount(data.UnlockedBlueTokens, CollectibleTokens.JumpBoostUnlocks);
                PoleJumpBoost += (1f - PoleJumpBoost) * CollectibleTokens.UnlockedCount(data.UnlockedBlueTokens, CollectibleTokens.PoleJumpUnlocks); //1 pole jump unlock => PoleJumpBoost == 1
                DashSpeed += 1f * CollectibleTokens.UnlockedCount(data.UnlockedBlueTokens, CollectibleTokens.DashSpeedUnlocks);

                //red unlocks

                if (!CanWallJump)
                    CanWallJump = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.WallJumpUnlock);
                if (!WallDashReset)
                    WallDashReset = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.WallDashResetUnlock);

                int poleClimb = CollectibleTokens.UnlockedCount(data.UnlockedRedTokens, CollectibleTokens.ClimbPolesUnlocks);
                if (!CanGrabPoles && poleClimb > 0)
                {
                    CanGrabPoles = true;
                    poleClimb--;
                }
                if (!ClimbVerticalPoles && poleClimb > 0)
                {
                    ClimbVerticalPoles = true;
                    poleClimb--;
                }

                if (!ClimbVerticalCorridors)
                    ClimbVerticalCorridors = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.ClimbPipesUnlock);
                if (!CanUseShortcuts)
                    CanUseShortcuts = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.UseShortcutsUnlock);

                int swim = CollectibleTokens.UnlockedCount(data.UnlockedRedTokens, CollectibleTokens.SwimUnlocks);
                if (!CanSwim && swim > 0)
                {
                    CanSwim = true;
                    swim--;
                }
                if (!CanDive && swim > 0)
                {
                    CanDive = true;
                    swim--;
                }

                int thr = CollectibleTokens.UnlockedCount(data.UnlockedRedTokens, CollectibleTokens.ThrowUnlocks);
                if (!CanThrowObjects && thr > 0)
                {
                    CanThrowObjects = true;
                    thr--;
                }
                if (!CanThrowSpears && thr > 0)
                {
                    CanThrowSpears = true;
                    thr--;
                }

                DashCount += CollectibleTokens.UnlockedCount(data.UnlockedRedTokens, CollectibleTokens.DashUnlocks);
                ExtraJumps += CollectibleTokens.UnlockedCount(data.UnlockedRedTokens, CollectibleTokens.JumpUnlocks);

                if (!CanGlide)
                    CanGlide = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.GlideUnlock);

                if (!AcidImmunity)
                    CanGlide = CollectibleTokens.IsUnlocked(data.UnlockedRedTokens, CollectibleTokens.AcidImmunityUnlock);


                //green unlocks

                MaxHealth += CollectibleTokens.UnlockedCount(data.UnlockedGreenTokens, CollectibleTokens.HealthUnlocks);

            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void BaseAbilities(SlugcatStats.Name slugcat)
    {
        if (slugcat == SlugcatStats.Name.White)
        {
            ExtraRunSpeed = -0.1f;

            JumpBoost = 0.75f;
            PoleJumpBoost = 0.7f;
            JumpBoostDecrement = 0.5f;

            CanWallJump = false;
            WallDashReset = false;

            CanGrabPoles = false;
            ClimbVerticalPoles = false;

            ClimbVerticalCorridors = false;

            CanUseShortcuts = false;

            CanSwim = false;
            CanDive = false;

            CanThrowObjects = false;
            CanThrowSpears = false;

            DashCount = 0;
            DashSpeed = 12f;
            DashStrength = 0.95f;

            ExtraJumps = 0;

            CanGlide = false;

            HasHealth = true;
            MaxHealth = Math.Max(0, 3 + Options.ExtraHealth); //add extra health. Don't let MaxHealth be less than 0.

            HasInventory = true;

            AcidImmunity = false;
        }
        else
        {
            OptionsAbilities();
        }
    }

    private static void OptionsAbilities()
    {
        ExtraRunSpeed = Options.ExtraRunSpeed;

        JumpBoost = Options.JumpBoost;
        PoleJumpBoost = Options.PoleJumpBoost;
        JumpBoostDecrement = Options.JumpBoostDecrement;

        CanWallJump = Options.CanWallJump;
        WallDashReset = Options.WallDashReset;

        CanGrabPoles = Options.CanGrabPoles;
        ClimbVerticalPoles = Options.ClimbVerticalPoles;

        ClimbVerticalCorridors = Options.ClimbVerticalCorridors;

        CanUseShortcuts = Options.CanUseShortcuts;

        CanSwim = Options.CanSwim;
        CanDive = Options.CanDive;

        CanThrowObjects = Options.CanThrowObjects;
        CanThrowSpears = Options.CanThrowSpears;

        DashCount = Options.DashCount;
        DashSpeed = Options.DashSpeed;
        DashStrength = Options.DashStrength;

        ExtraJumps = Options.ExtraJumps;

        CanGlide = Options.CanGlide;

        HasHealth = Options.HasHealth;
        MaxHealth = Options.MaxHealth;

        HasInventory = Options.HasInventory;

        AcidImmunity = Options.AcidImmunity;
    }
}
