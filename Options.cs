using Menu.Remix.MixedUI;
using MetroidvaniaMode.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MetroidvaniaMode;

public class Options : AutoConfigOptions
{
    private const string ABILITIES = "Abilities",
        GENERAL = "General",
        ACCESSIBILITY = "Accessibility",
        ADVANCED = "Advanced";

    public Options() : base(new TabInfo[]
    {
        new(ABILITIES) { spacing = 30f },
        new(GENERAL),
        new(ACCESSIBILITY),
        new(ADVANCED) { spacing = 30f },
    })
    {
        //LogLevel = 3; //temporarily enable all logs
    }


    //ABILITIES

    [Config(ABILITIES, "Jump Boost", "Multiplies the jump boost, which heavily affects jump height. Slugpup = 0.875"), LimitRange(0, 10f)]
    public static float JumpBoost = 1;
    [Config(ABILITIES, "Pole Jump Boost", "Further multiplies the jump boost when jumping off of a pole. Intended to make jumping from horizontal poles mostly useless.", rightSide = true), LimitRange(0, 10f)]
    public static float PoleJumpBoost = 1;

    [Config(ABILITIES, "Jump Boost Decrement", "How quickly Player.JumpBoost decrements while holding jump. Set below 1 to make jumps feel floatier."), LimitRange(0, 10f)]
    public static float JumpBoostDecrement = 1;

    [Config(ABILITIES, "Can Wall Jump", "Allows the slugcat to wall jump")]
    public static bool CanWallJump = true;
    [Config(ABILITIES, "Wall Dash Reset", "Lets the slugcat reset dashes and extra jumps by clinging to a wall", rightSide = true)]
    public static bool WallDashReset = false;

    [Config(ABILITIES, "Can Grab Poles", "Allows the slugcat to grab poles, both horizontal and vertical")]
    public static bool CanGrabPoles = true;
    [Config(ABILITIES, "Climb Vertical Poles", "Allows the slugcat to grab vertical poles", rightSide = true)]
    public static bool ClimbVerticalPoles = true;

    [Config(ABILITIES, "Climb Vertical Pipes", "Allows the slugcat to crawl upward through vertical pipes/corridors")]
    public static bool ClimbVerticalCorridors = true;
    [Config(ABILITIES, "Can Use Shortcuts", "Allows the player to use shortcuts within a room. Does not apply to room exits.", rightSide = true)]
    public static bool CanUseShortcuts = true;

    [Config(ABILITIES, "Can Swim", "Allows the player to float to move around in water")]
    public static bool CanSwim = true;
    [Config(ABILITIES, "Can Dive", "Allows the player to swim downwards in water")]
    public static bool CanDive = true;

    [Config(ABILITIES, "Can Throw Objects", "Allows the player to throw objects, such as rocks and flashbangs. If false, all objects are tossed like blue fruits.")]
    public static bool CanThrowObjects = true;
    [Config(ABILITIES, "Can Throw Spears", "Allows the player to throw spears. If false, spears are tossed like Saint.", rightSide = true)]
    public static bool CanThrowSpears = true;

    [Config(ABILITIES, "Dash Count", "The number of dashes that the player can do before touching the ground again"), LimitRange(0, 100)]
    public static int DashCount = 0;
    [Config(ABILITIES, "Water Dash", "Allows the player to dash in water. Also refreshes the player's dash underwater.", rightSide = true)]
    public static bool WaterDash = false;

    [Config(ABILITIES, "Dash Speed", "The player's set speed upon dashing")]
    public static float DashSpeed = 12f;
    [Config(ABILITIES, "Dash Strength", "How much of the player's speed is converted to the dash speed.\n1 = dash completely overrides player speed. 0 = dash does nothing.")]
    public static float DashStrength = 0.95f;

    [Config(ABILITIES, "Extra Jumps", "Allows the player to double jump"), LimitRange(0, 100)]
    public static int ExtraJumps = 0;

    [Config(ABILITIES, "Can Glide", "Allows the player to glide in the air to slow descents")]
    public static bool CanGlide = false;

    [Config(ABILITIES, "Has Health", "Enables the health bar system")]
    public static bool HasHealth = false;
    [Config(ABILITIES, "Max Health", "The maximum amount of health, and the default health", rightSide = true), LimitRange(0, 30)]
    public static int MaxHealth = 3;

    [Config(ABILITIES, "Has Shield", "Enables the shield ability")]
    public static bool HasShield = false;

    [Config(ABILITIES, "Has Inventory", "Enables the inventory wheel")]
    public static bool HasInventory = false;
    [Config(ABILITIES, "Unlock All Inventory Items", "Makes all inventory items available", rightSide = true)]
    public static bool UnlockAllInventoryItems = false;

    [Config(ABILITIES, "Acid Immunity", "Makes the player immune to acid")]
    public static bool AcidImmunity = false;
    [Config(ABILITIES, "Extra Run Speed", "Increases or decreases the player's run speed. For reference, Survivor's speed is normally 1.", rightSide = true), LimitRange(-1, 5)]
    public static float ExtraRunSpeed = 0;


    //GENERAL

    [Config(GENERAL, "Log Level", "When this number is higher, less important logs are displayed."), LimitRange(0, 3)]
    public static int LogLevel = 0;

    [Config(GENERAL, "Test String", "This is a test", width = 150f)]
    public static string TestString = "Hi!";
    [Config(GENERAL, "Test ComboBox", "This is also a test", width = 150f, dropdownOptions = new string[] {"Option1", "Option2", "Option3"})]
    public static string TestString2 = "Hi!";


    //ACCESSIBILITY

    [Config(ACCESSIBILITY, "Press Jump to Dash", "Makes dashes be triggered by trying to jump in the air. This lessens the number of different buttons that need to be pressed.\n(However, it doesn't let you dash on the ground, but hopefully that's not a big deal.)")]
    public static bool PressJumpToDash = false;

    [Config(ACCESSIBILITY, "/40 Input Buffering", "How many ticks of input buffering for dash inputs. 40 ticks == 1 second.\nRainWorld has 5 tick input buffering for most inputs."), LimitRange(0, 40)]
    public static int InputBuffering = 5;

    [Config(ACCESSIBILITY, "Easier Glide Mode", "Enables code that attempts to make gliding easier, at the expense of taking away some of your fine control. Disable this if you want to call yourself an aviation pro.")]
    public static bool EasierGlideMode = true;

    [Config(ACCESSIBILITY, "/40 Inventory Open Time", "How long it takes for the inventory wheel to open, expressed in ticks (40 ticks == 1 second)"), LimitRange(0, 40)]
    public static int InventoryOpenTime = 10;
    [Config(ACCESSIBILITY, "/40 Inventory Stickiness", "How long it takes the inventory wheel to deselect something, expressed in ticks (40 ticks == 1 second)", rightSide = true), LimitRange(0, 40)]
    public static int InventoryWheelStickiness = 4;

    [Config(ACCESSIBILITY, "Extra Health", "Increases your health in order to make the game easier. Increase this number if the game is too difficult for you."), LimitRange(-10, 20)]
    public static int ExtraHealth = 0;

    [Config(ACCESSIBILITY, "Dash Keybind (Keyboard)", "Which keybind activates the dash ability, if it is enabled\nTHIS OPTION DOES NOTHING IF YOU HAVE IMPROVED INPUT CONFIG ENABLED!", width = 80f)]
    public static KeyCode DashKeyCode = KeyCode.D;
    [Config(ACCESSIBILITY, "Dash Keybind (Controller)", "Which keybind activates the dash ability, if it is enabled\nTHIS OPTION DOES NOTHING IF YOU HAVE IMPROVED INPUT CONFIG ENABLED!", rightSide = true, width = 120f)]
    public static KeyCode DashControllerKeyCode = KeyCode.JoystickButton4;

    [Config(ACCESSIBILITY, "Shield Input (Controller)", "What activates the shield ability for gamepads/controllers. Select Button to use a normal button.", width = 100f, extraMargin = 20f, dropdownOptions = new string[] {"LT", "RT", "Button"})]
    public static string ShieldInputType = "LT";

    [Config(ACCESSIBILITY, "Shield Keybind (Keyboard)", "Which keybind activates the shield ability, if it is enabled\nTHIS OPTION DOES NOTHING IF YOU HAVE IMPROVED INPUT CONFIG ENABLED!", width = 80f)]
    public static KeyCode ShieldKeyCode = KeyCode.S;
    [Config(ACCESSIBILITY, "Shield Keybind (Controller)", "Which keybind activates the shield ability, if it is enabled\nTHIS OPTION DOES NOTHING IF YOU HAVE IMPROVED INPUT CONFIG ENABLED!", width = 120f, rightSide = true)]
    public static KeyCode ShieldControllerKeyCode = KeyCode.JoystickButton6;


    //ADVANCED

    [Config(ADVANCED, "/40 Invincibility Frames", "How long the player is invincible after taking damage, expressed in ticks (40 ticks == 1 second).\nDoes not include time spent stunned."), LimitRange(0, 120)]
    public static int InvincibilityFrames = 40;
    [Config(ADVANCED, "/40 Max I-Frames", "The maximum time the player can be invincible after taking damage, expressed in ticks (40 ticks == 1 second).\nDOES include time spent stunned.", rightSide = true), LimitRange(0, 240)]
    public static int MaxInvincibilityFrames = 80;

    [Config(ADVANCED, "/40 Dash Cooldown", "The minimum time between dashes, expressed in ticks (40 ticks == 1 second)"), LimitRange(0, 200)]
    public static int DashCooldown = 0;
    [Config(ADVANCED, "/40 Water Dash Cooldown", "The minimum time between dashes, expressed in ticks (40 ticks == 1 second).\nUsed instead of Dash Cooldown when in water."), LimitRange(0, 200)]
    public static int WaterDashCooldown = 20;

    [Config(ADVANCED, "Glide Anti-Gravity", "How much to subtract from gravity"), LimitRange(0, 2)]
    public static float GlideAntiGrav = 0.25f;
    [Config(ADVANCED, "Glide Thrust", "Gives the player thrust forward while gliding, in case you wanted the slugcat literally become an airplane", rightSide = true, precision = 3), LimitRange(0, 1)]
    public static float GlideThrust = 0;

    [Config(ADVANCED, "Glide Drag Coef", "How much air resistance the slugcat has in the perpendicular direction. Setting this low makes gliding less effective; setting it high makes the motion rigid.", precision = 3), LimitRange(0, 1)]
    public static float GlideDragCoef = 0.2f;
    [Config(ADVANCED, "Glide Omni Drag Coef", "How much air resistance the slugcat has in ALL directions. Increasing this will decrease the slugcat's max speed.", precision = 3, rightSide = true), LimitRange(0, 1)]
    public static float GlideOmniDragCoef = 0.02f;

    [Config(ADVANCED, "Glide Lift Coef", "How much lift the slugcat generated when flying. This allows the slugcat to pull up when going fast, and it makes the controls feel more responsive.\nHowever, setting this too high will allow the slugcat to literally fly upwards, which is cheating. As funny as it is, we don't want to make a literal slugcat airplane.", precision = 3), LimitRange(0, 1)]
    public static float GlideLiftCoef = 0.25f;
    [Config(ADVANCED, "Glide Max Lift", "Caps the amount of lift, preventing the slugcat from exploding when going too fast.", rightSide = true), LimitRange(0, 1)]
    public static float GlideMaxLift = 0.6f;

    [Config(ADVANCED, "Glide Base Y Angle", "(EasierGlideMode only) Adjusts the glide direction for drag slightly downwards, because this is what players probably expect."), LimitRange(-1, 1)]
    public static float GlideBaseDirY = -0.1f;
    [Config(ADVANCED, "Glide Keyboard Y Mult", "Multiplies the y for the keyboard, so that instead of trying to move perfectly diagonally (1,1), the slugcat moves more smoothly (e.g: (1,0.5)).\nMakes flying much easier on a keyboard.", rightSide = true), LimitRange(0, 1)]
    public static float GlideKeyboardYFac = 0.5f;

    [Config(ADVANCED, "Glide Angle Enforcement", "How strongly the slugcat is forced to be facing the right way when gliding")]
    public static float GlideAngleEnforcement = 0.25f;

    [Config(ADVANCED, "/40 Shield Full Time", "How long the shield can be at full strength, expressed in ticks (40 ticks == 1 second)"), LimitRange(0, 4000)]
    public static int ShieldFullTime = 80;
    [Config(ADVANCED, "/40 Shield Max Time", "How long the shield can be up at all, expressed in ticks (40 ticks == 1 second).\nPlease ensure this is greater than ShieldFullTime.", rightSide = true), LimitRange(0, 4000)]
    public static int ShieldMaxTime = 120;

    [Config(ADVANCED, "Shield Recovery Speed", "How quickly the shield cooldown decreases so it can be used again.\n2.0 == half as long as ShieldMaxTime"), LimitRange(0, 100)]
    public static float ShieldRecoverySpeed = 3f;
    [Config(ADVANCED, "Shield Damage Fac", "How much damage the shield can block", rightSide = true), LimitRange(0, 100)]
    public static float ShieldDamageFac = 2f;

    [Config(ADVANCED, "/40 Shield Break Stun", "How long the player is stunned when the shield breaks, expressed in ticks (40 ticks == 1 second)"), LimitRange(0, 400)]
    public static int ShieldStunTime = 80;


    private class AcceptableControllerButton : ConfigAcceptableBase
    {
        public KeyCode defaultCode;
        public AcceptableControllerButton(KeyCode defaultCode) : base(typeof(KeyCode))
        {
            this.defaultCode = defaultCode;
        }

        public override object Clamp(object value)
        {
            return IsValid(value) ? value : defaultCode;
        }

        public override bool IsValid(object value)
        {
            return (int)value >= (int)KeyCode.JoystickButton0 && (int)value <= (int)KeyCode.Joystick8Button19;
        }

        public override string ToDescriptionString()
        {
            return "# Acceptable values range from JoystickButton0 to Joystick8Button19";
        }
    }
    public override ConfigAcceptableBase AcceptableForConfig(string id)
    {
        if (id == nameof(DashControllerKeyCode)) return new AcceptableControllerButton(KeyCode.JoystickButton4);
        if (id == nameof(ShieldControllerKeyCode)) return new AcceptableControllerButton(KeyCode.JoystickButton6);
        return base.AcceptableForConfig(id);
    }

    public override void MenuInitialized()
    {
        base.MenuInitialized();

        //grey out keybinds if Improved Input Config is enabled
        if (Plugin.ImprovedInputEnabled)
        {
            foreach (OpTab tab in Tabs)
            {
                foreach (UIelement item in tab.items)
                {
                    if (item is OpKeyBinder keyBinder)
                        keyBinder.greyedOut = true; //disable it!
                }
            }
        }
        else
        {
            //controller keybind insanity
            foreach (OpTab tab in Tabs)
            {
                foreach (UIelement item in tab.items)
                {
                    try
                    {
                        if (item is OpComboBox comboBox)
                        {
                            string findName = comboBox.cfgEntry.key switch
                            {
                                nameof(ShieldInputType) => nameof(ShieldControllerKeyCode),
                                _ => null
                            };
                            if (findName == null) continue;

                            OpKeyBinder keyBinder = (OpKeyBinder)tab.items.First(el => el is OpKeyBinder k && k.cfgEntry.key == findName);
                            keyBinder.greyedOut = comboBox.value != "Button"; //grey out unless value is Button

                            comboBox.OnValueChanged += (UIconfig config, string value, string oldValue) =>
                            {
                                keyBinder.greyedOut = value != "Button";
                            };
                        }
                    } catch (Exception ex) { Plugin.Error(ex); }
                }
            }
        }

    }

}
