using MetroidvaniaMode.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MetroidvaniaMode.Tools;

public abstract class SaveManager
{
    public class SaveKey : Attribute
    {
        public string ID;
        public SaveKey(string iD) : base()
        {
            ID = iD;
        }
    }

    public const string PREFIX = "MVM_DATA_";
    private const string KEYVALUESPLIT = "=";
    private const char DELIMITER = '@';

    public string SaveData()
    {
        string data = PREFIX;

        try
        {
            FieldInfo[] infos = this.GetType().GetFields();
            foreach (FieldInfo info in infos)
            {
                try
                {
                    SaveKey att = info.GetCustomAttribute<SaveKey>();
                    if (att != null)
                    {
                        data += att.ID + KEYVALUESPLIT + SafeString(info.GetValue(this).ToString()) + DELIMITER;
                    }

                } catch (Exception ex) { Plugin.Error("Problematic field: " + info.Name); Plugin.Error(ex); }
            }
        } catch (Exception ex) { Plugin.Error(ex); }

        return data;
    }

    private static string SafeString(string s)
    {
        s = s.Replace("~", "<sqg>"); //~ is used by vanilla to split up save string
        s = s.Replace(KEYVALUESPLIT, "<kv>");
        s = s.Replace(DELIMITER.ToString(), "<del>"); //DELIMITER is used by us to split up save strings
        return s;
    }
    private static string UnsafenString(string s) //undo SafeString
    {
        s = s.Replace("<sqg>", "~");
        s = s.Replace("<kv>", KEYVALUESPLIT);
        s = s.Replace("<del>", DELIMITER.ToString());
        return s;
    }

    public void LoadData(List<string> strings)
    {
        try
        {
            //find the correct string
            string s = strings.Find(str => str.StartsWith(PREFIX));
            if (s == null)
            {
                Plugin.Error("COULD NOT FIND SAVE DATA IN UNRECOGNIZED SAVE STRINGS!!!");
                return;
            }
            string[] d = s.Substring(PREFIX.Length).Split(DELIMITER); //don't include the prefix... stupid me; this took WAY TOO LONG TO SOLVE

            FieldInfo[] infos = this.GetType().GetFields();
            foreach (FieldInfo info in infos)
            {
                try
                {
                    SaveKey att = info.GetCustomAttribute<SaveKey>();
                    if (att != null)
                    {
                        string key = att.ID + KEYVALUESPLIT;
                        string val = d.FirstOrDefault(str => str.StartsWith(key));
                        if (val == default)
                            Plugin.Log("Could not find save key " + key);
                        else
                            info.SetValue(this, FromString(info.FieldType, UnsafenString(val.Substring(key.Length))));
                    }
                }
                catch (Exception ex) { Plugin.Error("Problematic field: " + info.Name); Plugin.Error(ex); }
            }
        }
        catch (Exception ex) { Plugin.Error(ex); }
    }

    private static object FromString(Type t, string s)
    {
        if (t == typeof(int))
            return Int32.Parse(s);
        if (t == typeof(float))
            return float.Parse(s);
        if (t == typeof(bool))
            return s == "true";
        return s; //default case
    }

}

public static class SaveExt
{
    public static WorldSaveData GetData(this MiscWorldSaveData self)
        => WorldSaveData.CurrentInstance?.Data == self ? WorldSaveData.CurrentInstance : new(self); //give the current instance if it's right; otherwise make a new one
}
