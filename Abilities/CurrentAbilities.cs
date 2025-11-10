using MetroidvaniaMode.Collectibles;
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

                JumpBoost += 0.02f * CollectibleTokens.UnlockedCount(b, CollectibleTokens.JumpBoostUnlocks);
                PoleJumpBoost += (1f - PoleJumpBoost) * CollectibleTokens.UnlockedCount(b, CollectibleTokens.PoleJumpUnlocks); //1 pole jump unlock => PoleJumpBoost == 1
                DashSpeed += 1f * CollectibleTokens.UnlockedCount(b, CollectibleTokens.DashSpeedUnlocks);

                //red unlocks
                string[] r = data.UnlockedRedTokens.Split(';');

                if (!CanWallJump)
                    CanWallJump = CollectibleTokens.IsUnlocked(r, CollectibleTokens.WallJumpUnlock);

                int poleClimb = CollectibleTokens.UnlockedCount(r, CollectibleTokens.ClimbPolesUnlocks);
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
                    ClimbVerticalCorridors = CollectibleTokens.IsUnlocked(r, CollectibleTokens.ClimbPipesUnlock);
                if (!CanUseShortcuts)
                    CanUseShortcuts = CollectibleTokens.IsUnlocked(r, CollectibleTokens.UseShortcutsUnlock);

                int swim = CollectibleTokens.UnlockedCount(r, CollectibleTokens.SwimUnlocks);
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

                int thr = CollectibleTokens.UnlockedCount(r, CollectibleTokens.ThrowUnlocks);
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

                DashCount += CollectibleTokens.UnlockedCount(r, CollectibleTokens.DashUnlocks);
                ExtraJumps += CollectibleTokens.UnlockedCount(r, CollectibleTokens.JumpUnlocks);


                //green unlocks
                string[] g = data.UnlockedGreenTokens.Split(';');

                MaxHealth += CollectibleTokens.UnlockedCount(g, CollectibleTokens.HealthUnlocks);

            }

        } catch (Exception ex) { Plugin.Error(ex); }
    }

    private static void BaseAbilities(SlugcatStats.Name slugcat)
    {
        if (slugcat == SlugcatStats.Name.White)
        {
            JumpBoost = 0.8f;
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
            MaxHealth = Math.Max(0, 3 + Options.ExtraHealth); //add extra health. Don't let MaxHealth be less than 0.
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
