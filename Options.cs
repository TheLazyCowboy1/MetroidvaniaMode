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
        //LogLevel = 3; //temporarily enable all logs
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

    [Config("Advanced", "/40 Dash Cooldown", "The minimum time between dashes, expressed in ticks (40 ticks == 1 second)"), LimitRange(0, 200)]
    public static int DashCooldown = 0;

    [Config("Abilities", "Extra Jumps", "Allows the player to double jump"), LimitRange(0, 100)]
    public static int ExtraJumps = 0;

    [Config("Abilities", "Can Glide", "Allows the player to glide in the air to slow descents")]
    public static bool CanGlide = false;

    [Config("Advanced", "Glide Anti-Gravity", "How much to subtract from gravity"), LimitRange(0, 2)]
    public static float GlideAntiGrav = 0.5f;
    [Config("Abilities", "Glide Thrust", "Gives the player thrust forward while gliding, in case you wanted the slugcat literally become an airplane", rightSide = true, precision = 3), LimitRange(0, 1)]
    public static float GlideThrust = 0;
    [Config("Advanced", "Glide Drag Coef", "How much air resistance the slugcat has in the perpendicular direction. Setting this low makes gliding less effective; setting it high makes the motion rigid.", precision = 3), LimitRange(0, 1)]
    public static float GlideDragCoef = 0.25f;
    [Config("Advanced", "Glide Omni Drag Coef", "How much air resistance the slugcat has in ALL directions. Increasing this will decrease the slugcat's max speed.", precision = 3, rightSide = true), LimitRange(0, 1)]
    public static float GlideOmniDragCoef = 0.04f;
    [Config("Advanced", "Glide Lift Coef", "How much lift the slugcat generated when flying. This allows the slugcat to pull up when going fast, and it makes the controls feel more responsive.\nHowever, setting this too high will allow the slugcat to literally fly upwards, which is cheating. As funny as it is, we don't want to make a literal slugcat airplane.", precision = 3), LimitRange(0, 1)]
    public static float GlideLiftCoef = 0.25f;
    [Config("Advanced", "Glide Max Lift", "Caps the amount of lift, preventing the slugcat from exploding when going too fast.", rightSide = true), LimitRange(0, 1)]
    public static float GlideMaxLift = 0.6f;
    [Config("Advanced", "Glide Base Y Angle", "(EasierGlideMode only) Adjusts the glide direction for drag slightly downwards, because this is what players probably expect."), LimitRange(-1, 1)]
    public static float GlideBaseDirY = -0.1f;
    [Config("Advanced", "Glide Keyboard Y Mult", "Multiplies the y for the keyboard, so that instead of trying to move perfectly diagonally (1,1), the slugcat moves more smoothly (e.g: (1,0.5)).\nMakes flying much easier on a keyboard.", rightSide = true), LimitRange(0, 1)]
    public static float GlideKeyboardYFac = 0.5f;

    [Config("Abilities", "Has Health", "Enables the health bar system")]
    public static bool HasHealth = false;
    [Config("Abilities", "Max Health", "The maximum amount of health, and the default health", rightSide = true), LimitRange(0, 30)]
    public static int MaxHealth = 3;

    [Config("Abilities", "Has Inventory", "Enables the inventory wheel")]
    public static bool HasInventory = false;
    [Config("Abilities", "Unlock All Inventory Items", "Makes all inventory items available", rightSide = true)]
    public static bool UnlockAllInventoryItems = false;

    [Config("Accessibility", "/40 Inventory Open Time", "How long it takes for the inventory wheel to open."), LimitRange(0, 40)]
    public static int InventoryOpenTime = 10;


    [Config("General", "Test String", "This is a test", width = 150f)]
    public static string TestString = "Hi!";
    [Config("General", "Test ComboBox", "This is also a test", width = 150f, dropdownOptions = new string[] {"Option1", "Option2", "Option3"})]
    public static string TestString2 = "Hi!";


    [Config("Accessibility", "Press Jump to Dash", "Makes dashes be triggered by trying to jump in the air. This lessens the number of different buttons that need to be pressed.\n(However, it doesn't let you dash on the ground, but hopefully that's not a big deal.)")]
    public static bool PressJumpToDash = false;

    [Config("Accessibility", "Easier Glide Mode", "Enables code that attempts to make gliding easier, at the expense of taking away some of your fine control. Disable this if you want to call yourself an aviation pro.")]
    public static bool EasierGlideMode = true;

    [Config("Accessibility", "Extra Health", "Increases your health in order to make the game easier. Increase this number if the game is too difficult for you."), LimitRange(-10, 20)]
    public static int ExtraHealth = 0;

}
