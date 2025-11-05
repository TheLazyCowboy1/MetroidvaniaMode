

namespace MetroidvaniaMode;

public class Options : AutoConfigOptions
{
    public Options() : base(new string[] { "Options", "Abilities" })
    {

    }

    [TabAtt("Abilities", "Jump Boost", "Multiplies the jump boost, which heavily affects jump height"), LimitRange(0, 10f)]
    public static float JumpBoost = 1;

    [TabAtt("Abilities", "Can Wall Jump", "Allows the slugcat to wall jump")]
    public static bool CanWallJump = true;

    [TabAtt("Abilities", "Can Grab Poles", "Allows the slugcat to grab poles, both horizontal and vertical")]
    public static bool CanGrabPoles = true;

    [TabAtt("Abilities", "Climb Vertical Poles", "Allows the slugcat to grab vertical poles")]
    public static bool ClimbVerticalPoles = true;

    [TabAtt("Abilities", "Climb Vertical Pipes", "Allows the slugcat to crawl upward through vertical pipes/corridors")]
    public static bool ClimbVerticalCorridors = true;

    [TabAtt("Abilities", "Can Use Shortcuts", "Allows the player to use shortcuts within a room. Does not apply to room exits.")]
    public static bool CanUseShortcuts = true;

    [TabAtt("Abilities", "Pole Jump Boost", "Further multiplies the jump boost when jumping off of a pole. Intended to make jumping from horizontal poles mostly useless.")]
    public static float PoleJumpBoost = 1;
}
