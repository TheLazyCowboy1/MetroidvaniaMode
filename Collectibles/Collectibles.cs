using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static MultiplayerUnlocks;

namespace MetroidvaniaMode.Collectibles;

public static class Collectibles
{
    /**
     * Collectible color coding (tentative):
     * Blue (SandboxUnlockID) = Improved stats...? (e.g: Higher jumps)
     * Yellow (LevelUnlockID) = Fast travel...?
     * Red (SafariUnlockID) = Ability
     * Green (SlugcatUnlockID) = Health Upgrade
     */

    [Collectible(3, "Unlocked Ability: Dash. Press D to perform a dash!", "Unlocked an additional dash: Perform more dashes without touching the ground!")]
    public static SandboxUnlockID[] DashUnlock;

    [Collectible(3, "Unlocked Ability: Double Jump. Jump again in the air!", "Unlocked an additional jump: Perform more jumps in the air!")]
    public static SandboxUnlockID[] JumpUnlock;

    [Collectible(1, "Unlocked Ability: Upward Pipe Crawl. Crawl up pipes and reach new places!")]
    public static SandboxUnlockID ClimbPipesUnlock;

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
                            Array arr = Array.CreateInstance(elType, att.Count);

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
                        }
                        else
                        {
                            info.SetValue(null, Activator.CreateInstance(info.FieldType, PREFIX + info.Name, true));
                            debugList += PREFIX + info.Name + ";";
                        }

                    }
                } catch (Exception ex) { Plugin.Error("Problem with field " + info.Name); Plugin.Error(ex); }
            }

            Plugin.Log("Registered collectible ExtEnums: " + debugList);

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


    public static bool IsUnlocked(ExtEnumBase en, string[] splitSaveString) => splitSaveString.Contains(en.value);
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
