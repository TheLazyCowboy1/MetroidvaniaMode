using MetroidvaniaMode.Tools;
using UnityEngine;

namespace MetroidvaniaMode;

public class Options : AutoConfigOptions
{
    public Options() : base(new TabInfo[]
    {
        new("Abilities") { spacing = 30f },
        new("General"),
        new("Accessibility"),
        new("Advanced") { spacing = 30f },
    })
    {
        LogLevel = 3; //temporarily enable all logs
    }


    [Config("General", "Log Level", "When this number is higher, less important logs are displayed."), LimitRange(0, 3)]
    public static int LogLevel = 0;

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
    [Config("Abilities", "Can Use Shortcuts", "Allows the player to use shortcuts within a room. Does not apply to room exits.", rightSide = true)]
    public static bool CanUseShortcuts = true;

    [Config("Abilities", "Can Swim", "Allows the player to float to move around in water")]
    public static bool CanSwim = true;
    [Config("Abilities", "Can Dive", "Allows the player to swim downwards in water")]
    public static bool CanDive = true;

    [Config("Abilities", "Can Throw Objects", "Allows the player to throw objects, such as rocks and flashbangs. If false, all objects are tossed like blue fruits.")]
    public static bool CanThrowObjects = true;
    [Config("Abilities", "Can Throw Spears", "Allows the player to throw spears. If false, spears are tossed like Saint.", rightSide = true)]
    public static bool CanThrowSpears = true;

    //[Config("Abilities", "Can Dash", "Allows the player to use a dash ability")]
    //public static bool CanDash = false;
    [Config("Abilities", "Dash Count", "The number of dashes that the player can do before touching the ground again"), LimitRange(0, 100)]
    public static int DashCount = 0;
    [Config("Abilities", "Dash Keybind", "Which keybind activates the dash ability, if it is enabled", rightSide = true, width = 100f)]
    public static KeyCode DashKeyCode = KeyCode.D;

    [Config("Abilities", "Dash Speed", "The player's set speed upon dashing")]
    public static float DashSpeed = 12f;
    [Config("Abilities", "Dash Strength", "How much of the player's speed is converted to the dash speed.\n1 = dash completely overrides player speed. 0 = dash does nothing.")]
    public static float DashStrength = 0.95f;

    [Config("Abilities", "Extra Jumps", "Allows the player to double jump"), LimitRange(0, 100)]
    public static int ExtraJumps = 0;

    [Config("Abilities", "Can Glide", "Allows the player to glide in the air to slow descents")]
    public static bool CanGlide = false;

    [Config("Advanced", "Glide Slowdown Var", "Kind of the max falling speed...?"), LimitRange(0, 200)]
    public static float GlideSlowdownVar = 10f;
    [Config("Advanced", "Glide Anti-Gravity", "How much to subtract from gravity.", rightSide = true), LimitRange(0, 2)]
    public static float GlideAntiGrav = 0.5f;
    [Config("Advanced", "Glide XConversion Efficiency", "advanced"), LimitRange(0, 10)]
    public static float GlideXConversionEfficiency = 1f;
    [Config("Advanced", "Glide YConversion Efficiency", "advanced", rightSide = true), LimitRange(0, 10)]
    public static float GlideYConversionEfficiency = 4f;
    [Config("Advanced", "Glide Max XConversion", "advanced", precision = 3), LimitRange(0, 1)]
    public static float GlideMaxXConversion = 0.03f;
    [Config("Advanced", "Glide Max YConversion", "advanced", rightSide = true, precision = 3), LimitRange(0, 1)]
    public static float GlideMaxYConversion = 0.03f;

    [Config("Abilities", "Has Health", "Enables the health bar system")]
    public static bool HasHealth = false;
    [Config("Abilities", "Max Health", "The maximum amount of health, and the default health", rightSide = true), LimitRange(0, 30)]
    public static int MaxHealth = 3;


    [Config("General", "Test String", "This is a test", width = 150f)]
    public static string TestString = "Hi!";
    [Config("General", "Test ComboBox", "This is also a test", width = 150f, dropdownOptions = new string[] {"Option1", "Option2", "Option3"})]
    public static string TestString2 = "Hi!";


    [Config("Accessibility", "Press Jump to Dash", "Makes dashes be triggered by trying to jump in the air. This lessens the number of different buttons that need to be pressed.\n(However, it doesn't let you dash on the ground, but hopefully that's not a big deal.)")]
    public static bool PressJumpToDash = false;

    [Config("Accessibility", "Extra Health", "Increases your health in order to make the game easier. Increase this number if the game is too difficult for you."), LimitRange(-10, 20)]
    public static int ExtraHealth = 0;

}
