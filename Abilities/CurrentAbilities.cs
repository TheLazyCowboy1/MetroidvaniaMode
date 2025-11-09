using static MetroidvaniaMode.Collectibles.Collectibles;
using MetroidvaniaMode.SaveData;
using MetroidvaniaMode.Tools;
using System;

namespace MetroidvaniaMode.Abilities;

public static class CurrentAbilities
{
    public static float JumpBoost = 1;
    public static float PoleJumpBoost = 1;
    public static float JumpBoostDecrement = 1;

    public static bool CanWallJump = true;

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

    public static bool HasHealth = false;
    public static int MaxHealth = 3;

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

            if (game.IsStorySession && CountCollectibles(game.StoryCharacter))
            {
                WorldSaveData data = game.GetStorySession.saveState.miscWorldSaveData.GetData();

                //blue unlocks
                string[] b = data.UnlockedBlueTokens.Split(';');

                JumpBoost += 0.02f * UnlockedCount(b, JumpBoostUnlocks);
                PoleJumpBoost += (1f - PoleJumpBoost) * UnlockedCount(b, PoleJumpUnlocks); //1 pole jump unlock => PoleJumpBoost == 1
                DashSpeed += 1f * UnlockedCount(b, DashSpeedUnlocks);

                //red unlocks
                string[] r = data.UnlockedRedTokens.Split(';');

                if (!CanWallJump)
                    CanWallJump = IsUnlocked(r, WallJumpUnlock);

                int poleClimb = UnlockedCount(r, ClimbPolesUnlocks);
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
                    ClimbVerticalCorridors = IsUnlocked(r, ClimbPipesUnlock);
                if (!CanUseShortcuts)
                    CanUseShortcuts = IsUnlocked(r, UseShortcutsUnlock);

                int swim = UnlockedCount(r, SwimUnlocks);
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

                DashCount += UnlockedCount(r, DashUnlocks);
                ExtraJumps += UnlockedCount(r, JumpUnlocks);


                //green unlocks
                string[] g = data.UnlockedGreenTokens.Split(';');

                MaxHealth += UnlockedCount(g, HealthUnlocks);

            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void BaseAbilities(SlugcatStats.Name slugcat)
    {
        if (slugcat == SlugcatStats.Name.White)
        {
            JumpBoost = 0.85f;
            PoleJumpBoost = 0.7f;
            JumpBoostDecrement = 0.5f;

            CanWallJump = false;

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

            HasHealth = true;
            MaxHealth = 3;
        }
        else
        {
            OptionsAbilities();
        }
    }

    private static void OptionsAbilities()
    {
        JumpBoost = Options.JumpBoost;
        PoleJumpBoost = Options.PoleJumpBoost;
        JumpBoostDecrement = Options.JumpBoostDecrement;

        CanWallJump = Options.CanWallJump;

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

        HasHealth = Options.HasHealth;
        MaxHealth = Options.MaxHealth;
    }

    private static bool CountCollectibles(SlugcatStats.Name slugcat) => slugcat == SlugcatStats.Name.White;
}
