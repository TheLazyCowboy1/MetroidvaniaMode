using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static MultiplayerUnlocks;

namespace MetroidvaniaMode.Collectibles;

public static class Collectibles
{
    /**
     * Collectible color coding (tentative):
     * Blue (SandboxUnlockID) = Improved stats...? (e.g: Higher jumps)
     * Yellow (LevelUnlockID) = Permanent item (e.g: sword item in inventory)
     * Red (SafariUnlockID) = Ability
     * Green (SlugcatUnlockID) = Health Upgrade
     */

    //public static float JumpBoost = 1;
    [Collectible(10, "Increased jump height!")]
    public static SandboxUnlockID[] JumpBoostUnlocks;

    //public static float PoleJumpBoost = 1;
    [Collectible(2, "Unlocked Ability: Jump on Poles. Jumping on poles is much more effective now!", "Unlocked!... Wait, you can already pole jump. Heh, you unlocked nothing, loser.")]
    public static SandboxUnlockID[] PoleJumpUnlocks; //make it blue, because it is a minor ability that is jump-related

    //public static float JumpBoostDecrement = 1; //this probably shouldn't be an unlock anyway

    //public static bool CanWallJump = true;
    [Collectible(1, "Unlocked Ability: Wall Jump. Jump against walls to scale new heights!")]
    public static SafariUnlockID WallJumpUnlock;

    //public static bool CanGrabPoles = true;
    //public static bool ClimbVerticalPoles = true;
    [Collectible(2, "Unlocked Ability: Grab Poles. You can now grab and cling to poles! But you still cannot climb them.", "Unlocked Ability: Pole Climb. Shimmy up poles to scale the world!")]
    public static SafariUnlockID[] ClimbPolesUnlocks;

    //public static bool ClimbVerticalCorridors = true;
    [Collectible(1, "Unlocked Ability: Upward Pipe Crawl. Crawl up pipes and reach new places!")]
    public static SafariUnlockID ClimbPipesUnlock;

    //public static bool CanUseShortcuts = true;
    [Collectible(1, "Unlocked Ability: Use Shortcuts. Travel through walls and space using shortcuts! ...so long as there is a shortcut that goes there...")]
    public static SafariUnlockID UseShortcutsUnlock;

    //public static bool CanSwim = true;
    //public static bool CanDive = true;
    [Collectible(2, "Unlocked Ability: Swim. The slugcat has gotten over his apparently crippling (quite literally) fear of water!", "Unlocked Ability: Dive. Swim down to reach hidden depths! Be careful not to drown!")]
    public static SafariUnlockID[] SwimUnlocks;

    //public static bool CanThrowObjects = true;
    //public static bool CanThrowSpears = true;
    [Collectible(2, "Unlocked Ability: Throw. Toss rocks and bombs at your foes! Spears are still too heavy for you, though.", "Unlocked Ability: Throw Spears. ALERT! ALERT! THE SLUGCAT IS ARMED! RUN FOR YOUR LIVES!")]
    public static SafariUnlockID[] ThrowUnlocks;

    //public static int DashCount = 0;
    [Collectible(3, "Unlocked Ability: Dash. Press D to perform a dash!", "Unlocked an additional dash: Perform more dashes without touching the ground!")]
    public static SafariUnlockID[] DashUnlocks;

    //public static float DashSpeed = 12f;
    //public static float DashStrength = 0.95f;
    [Collectible(10, "Increased Dash Speed!")]
    public static SandboxUnlockID[] DashSpeedUnlocks;

    //public static int ExtraJumps = 0;
    [Collectible(3, "Unlocked Ability: Double Jump. Jump again in the air!", "Unlocked an additional jump: Perform more jumps in the air!")]
    public static SafariUnlockID[] JumpUnlocks;

    //public static bool HasHealth = false;
    //public static int MaxHealth = 3;
    [Collectible(10, "Increased Maximum Health! Your health bar will be larger next cycle!")]
    public static SlugcatUnlockID[] HealthUnlocks;


    private class Collectible : Attribute
    {
        public int Count;
        public string[] UnlockMessages;
        public Collectible(int count = 1, params string[] messages) : base()
        {
            Count = count;
            UnlockMessages = messages;
        }
    }

    public static List<ExtEnumBase> AllCollectibles;

    private const string PREFIX = "MVM_";

    public static void Register()
    {
        try
        {
            AllCollectibles = new();
            string debugList = "";

            FieldInfo[] infos = typeof(Collectibles).GetFields();
            foreach (FieldInfo info in infos)
            {
                try //a second try block, because we want to try to keep going if there is only an error with one or two fields
                {
                    Collectible att = info.GetCustomAttribute<Collectible>();
                    if (att != null)
                    {
                        if (info.FieldType.IsArray)
                        {
                            Type elType = info.FieldType.GetElementType();
                            ExtEnumBase[] arr = Array.CreateInstance(elType, att.Count) as ExtEnumBase[];

                            //set index 0
                            arr.SetValue(Activator.CreateInstance(elType, PREFIX + info.Name, true), 0);
                            debugList += PREFIX + info.Name + ";";

                            //set further indices
                            for (int i = 1; i < att.Count; i++)
                            {
                                arr.SetValue(Activator.CreateInstance(elType, PREFIX + info.Name + "_" + i, true), i);
                                debugList += PREFIX + info.Name + "_" + i + ";";
                            }

                            info.SetValue(null, arr);
                            AllCollectibles.AddRange(arr);
                        }
                        else
                        {
                            ExtEnumBase en = Activator.CreateInstance(info.FieldType, PREFIX + info.Name, true) as ExtEnumBase;
                            info.SetValue(null, en);
                            debugList += PREFIX + info.Name + ";";

                            AllCollectibles.Add(en);
                        }

                    }
                } catch (Exception ex) { Plugin.Error("Problem with field " + info.Name); Plugin.Error(ex); }
            }

            Plugin.Log("Registered collectible ExtEnums: " + debugList, 0); //always log this; I want to see this every time

        } catch (Exception ex) { Plugin.Error(ex); }
    }
    public static void Unregister()
    {
        throw new NotImplementedException("...yeah, I'm too lazy to add this");
    }

    public static void FixProgressionData(PlayerProgression.MiscProgressionData data)
    {
        try
        {
            FieldInfo[] infos = typeof(Collectibles).GetFields();
            foreach (FieldInfo info in infos)
            {
                try //a second try block, because we want to try to keep going if there is only an error with one or two fields
                {
                    Collectible att = info.GetCustomAttribute<Collectible>();
                    if (att != null)
                    {
                        Array arr;
                        if (info.FieldType.IsArray)
                            arr = info.GetValue(null) as Array;
                        else
                            arr = new ExtEnumBase[1] { info.GetValue(null) as ExtEnumBase };
                        foreach (object item in arr)
                        {
                            if (item is SandboxUnlockID sandbox)
                                data.sandboxTokens.Remove(sandbox);
                            else if (item is LevelUnlockID level)
                                data.levelTokens.Remove(level);
                            else if (item is SafariUnlockID safari)
                                data.safariTokens.Remove(safari);
                            else if (item is SlugcatUnlockID slugcat)
                                data.classTokens.Remove(slugcat);
                            else
                                Plugin.Error("Field " + info.Name + " is not a valid unlock type...?");
                        }
                    }
                }
                catch (Exception ex) { Plugin.Error("Problem with field " + info.Name); Plugin.Error(ex); }
            }

            Plugin.Log("Cleared progression data of unlock tokens");
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    public static string GetUnlockMessage(ExtEnumBase en, string[] splitSaveString) => GetUnlockMessage(en.value, splitSaveString);
    public static string GetUnlockMessage(string val, string[] splitSaveString)
    {
        try
        {
            FieldInfo[] infos = typeof(Collectibles).GetFields();
            foreach (FieldInfo info in infos)
            {
                try //a second try block, because we want to try to keep going if there is only an error with one or two fields
                {
                    Collectible att = info.GetCustomAttribute<Collectible>();
                    if (att != null)
                    {
                        if (att.UnlockMessages == null || att.UnlockMessages.Length < 1)
                            continue; //this unlock doesn't have any unlock message, so skip it

                        ExtEnumBase[] arr;
                        if (info.FieldType.IsArray)
                            arr = info.GetValue(null) as ExtEnumBase[];
                        else
                            arr = new ExtEnumBase[1] { info.GetValue(null) as ExtEnumBase };

                        if (arr.Any(en => en.value == val))
                        {
                            int msgIdx = UnlockedCount(splitSaveString, arr);
                            if (att.UnlockMessages.Length > msgIdx)
                                return att.UnlockMessages[msgIdx]; //return the message for this unlock, if it's in the array
                            else
                                return att.UnlockMessages[att.UnlockMessages.Length - 1]; //return the last message in the array if it's too small
                        }
                    }
                }
                catch (Exception ex) { Plugin.Error("Problem with field " + info.Name); Plugin.Error(ex); }
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        return null;
    }


    public static bool IsUnlocked(string[] splitSaveString, ExtEnumBase en) => splitSaveString.Contains(en.value);
    public static int UnlockedCount(string[] splitSaveString, ExtEnumBase[] collectibleSet) => collectibleSet.Count(u => splitSaveString.Contains(u.value));
    public static int UnlockedCount(string[] splitSaveString, ExtEnumBase en) => UnlockedCount(splitSaveString, en.value);
    /// <summary>
    /// How many of this particular category of unlock are unlocked
    /// </summary>
    /// <param name="splitSaveString">The save strings</param>
    /// <param name="val">The value of the enum to search for</param>
    /// <returns></returns>
    public static int UnlockedCount(string[] splitSaveString, string val)
    {
        try
        {
            FieldInfo[] infos = typeof(Collectibles).GetFields();
            foreach (FieldInfo info in infos)
            {
                try //a second try block, because we want to try to keep going if there is only an error with one or two fields
                {
                    Collectible att = info.GetCustomAttribute<Collectible>();
                    if (att != null)
                    {
                        ExtEnumBase[] arr;
                        if (info.FieldType.IsArray)
                            arr = info.GetValue(null) as ExtEnumBase[];
                        else
                            arr = new ExtEnumBase[1] { info.GetValue(null) as ExtEnumBase };

                        if (arr.Any(en => en.value == val))
                        {
                            return UnlockedCount(splitSaveString, arr);
                        }
                    }
                }
                catch (Exception ex) { Plugin.Error("Problem with field " + info.Name); Plugin.Error(ex); }
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }

        return 0;
    }

}
