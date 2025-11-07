using UnityEngine;

namespace MetroidvaniaMode;

public class Options : AutoConfigOptions
{
    public Options() : base(new TabInfo[]
    {
        new("Abilities") { spacing = 30f },
        new("General")
    })
    {

    }

    [Config("Abilities", "Jump Boost", "Multiplies the jump boost, which heavily affects jump height. Slugpup = 0.875"), LimitRange(0, 10f)]
    public static float JumpBoost = 1;
    [Config("Abilities", "Pole Jump Boost", "Further multiplies the jump boost when jumping off of a pole. Intended to make jumping from horizontal poles mostly useless.", rightSide = true), LimitRange(0, 10f)]
    public static float PoleJumpBoost = 1;

    [Config("Abilities", "Jump Boost Decrement", "How quickly Player.JumpBoost decrements while holding jump. Set below 1 to make jumps feel floatier."), LimitRange(0, 10f)]
    public static float JumpBoostDecrement = 1;

    [Config("Abilities", "Can Wall Jump", "Allows the slugcat to wall jump")]
    public static bool CanWallJump = true;

    [Config("Abilities", "Can Grab Poles", "Allows the slugcat to grab poles, both horizontal and vertical")]
    public static bool CanGrabPoles = true;
    [Config("Abilities", "Climb Vertical Poles", "Allows the slugcat to grab vertical poles", rightSide = true)]
    public static bool ClimbVerticalPoles = true;

    [Config("Abilities", "Climb Vertical Pipes", "Allows the slugcat to crawl upward through vertical pipes/corridors")]
    public static bool ClimbVerticalCorridors = true;

    [Config("Abilities", "Can Use Shortcuts", "Allows the player to use shortcuts within a room. Does not apply to room exits.")]
    public static bool CanUseShortcuts = true;

    [Config("Abilities", "Can Dash", "Allows the player to use a dash ability")]
    public static bool CanDash = false;
    [Config("Abilities", "Dash Keybind", "Which keybind activates the dash ability, if it is enabled", rightSide = true, width = 100f)]
    public static KeyCode DashKeyCode = KeyCode.D;

    [Config("Abilities", "Dash Speed", "The player's set speed upon dashing")]
    public static float DashSpeed = 20f;
    [Config("Abilities", "Dash Strength", "How much of the player's speed is converted to the dash speed.\n1 = dash completely overrides player speed. 0 = dash does nothing.")]
    public static float DashStrength = 0.9f;

    [Config("Abilities", "Has Health", "Enables the health bar system")]
    public static bool HasHealth = false;
    [Config("Abilities", "Max Health", "The maximum amount of health, and the default health", rightSide = true), LimitRange(0, 30)]
    public static int MaxHealth = 3;

    [Config("General", "Test String", "This is a test", width = 150f)]
    public static string TestString = "Hi!";
    [Config("General", "Test ComboBox", "This is also a test", width = 150f, dropdownOptions = new string[] {"Option1", "Option2", "Option3"})]
    public static string TestString2 = "Hi!";

}
