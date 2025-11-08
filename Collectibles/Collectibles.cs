using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static MultiplayerUnlocks;

namespace MetroidvaniaMode.Collectibles;

public static class Collectibles
{
    [Collectible(3)]
    public static SandboxUnlockID[] DashUnlock;

    [Collectible(3)]
    public static SandboxUnlockID[] JumpUnlock;

    private class Collectible : Attribute
    {
        public int Count;
        public Collectible(int count = 1) : base()
        {
            Count = count;
        }
    }

    private const string PREFIX = "MVM_";

    public static void Register()
    {
        try
        {
            string debugList = "";

            FieldInfo[] infos = typeof(Collectibles).GetFields();
            foreach (FieldInfo info in infos)
            {
                try //a second try block, because we want to try to keep going if there is only an error with one or two fields
                {
                    Collectible att = info.GetCustomAttribute<Collectible>();
                    if (att != null)
                    {
                        if (info.FieldType == typeof(SandboxUnlockID[]))
                        {
                            SandboxUnlockID[] arr = new SandboxUnlockID[att.Count];
                            arr[0] = new(PREFIX + info.Name, true);
                            debugList += PREFIX + info.Name + ";";

                            for (int i = 1; i < att.Count; i++) //register any additional ones
                            {
                                arr[i] = new(PREFIX + info.Name + "_" + i, true);
                                debugList += PREFIX + info.Name + "_" + i + ";";
                            }

                            info.SetValue(null, arr); //set the value of the actual field
                        }
                        else if (info.FieldType == typeof(LevelUnlockID[]))
                        {
                            LevelUnlockID[] arr = new LevelUnlockID[att.Count];
                            arr[0] = new(PREFIX + info.Name, true);
                            debugList += PREFIX + info.Name + ";";

                            for (int i = 1; i < att.Count; i++) //register any additional ones
                            {
                                arr[i] = new(PREFIX + info.Name + "_" + i, true);
                                debugList += PREFIX + info.Name + "_" + i + ";";
                            }

                            info.SetValue(null, arr); //set the value of the actual field
                        }
                        else if (info.FieldType == typeof(SafariUnlockID[]))
                        {
                            SafariUnlockID[] arr = new SafariUnlockID[att.Count];
                            arr[0] = new(PREFIX + info.Name, true);
                            debugList += PREFIX + info.Name + ";";

                            for (int i = 1; i < att.Count; i++) //register any additional ones
                            {
                                arr[i] = new(PREFIX + info.Name + "_" + i, true);
                                debugList += PREFIX + info.Name + "_" + i + ";";
                            }

                            info.SetValue(null, arr); //set the value of the actual field
                        }
                        else if (info.FieldType == typeof(SlugcatUnlockID[]))
                        {
                            SlugcatUnlockID[] arr = new SlugcatUnlockID[att.Count];
                            arr[0] = new(PREFIX + info.Name, true);
                            debugList += PREFIX + info.Name + ";";

                            for (int i = 1; i < att.Count; i++) //register any additional ones
                            {
                                arr[i] = new(PREFIX + info.Name + "_" + i, true);
                                debugList += PREFIX + info.Name + "_" + i + ";";
                            }

                            info.SetValue(null, arr); //set the value of the actual field
                        }
                        else
                        {
                            Plugin.Error("Unsupported type: " + info.FieldType + ". Field: " + info.Name);
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
                        Array arr = info.GetValue(null) as Array;
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
                catch (Exception ex) { Plugin.Error(ex); }
            }

            Plugin.Log("Cleared progression data of unlock tokens");
        } catch (Exception ex) { Plugin.Error(ex); }
    }

    public static int UnlockedCount(string[] splitSaveString, ExtEnumBase[] unlock)
    {
        int count = 0;
        while (count < unlock.Length && splitSaveString.Contains(unlock[count].value))
            count++;
        return count;
    }

}
