using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MetroidvaniaMode.Tools;

public class StringList : IEnumerable<string>
{
    private bool dirty = true;
    private string[] _array = new string[0];
    public string[] Array {
        get {
            if (dirty) //the array needs to be modified
            {
                dirty = false;

                string[] d = String.Split(new string[] { Delimiter }, StringSplitOptions.None);
                _array = new string[d.Length];
                for (int i = 0; i < d.Length; i++)
                {
                    _array[i] = Unsafe(d[i]);
                }
            }
            return _array;
        }
        private set => _array = value;
    }

    private string _string = "";
    public string String { get => _string; private set => _string = value; }

    private string _delimiter = ";";
    public string Delimiter { get => _delimiter; private set => _delimiter = value; }

    public StringList()
    {

    }
    public StringList(string s)
    {
        String = s;
    }
    public StringList(string s, string delimiter)
    {
        String = s;
        Delimiter = delimiter;
    }

    private string Safe(string s) => s == null ? "<NULL>" : s.Replace(Delimiter, "<sdel>");
    private string Unsafe(string s) => s == "<NULL>" ? null : s.Replace("<sdel>", Delimiter);

    public StringList Add(string s)
    {
        if (String.Length > 0) String += Delimiter;
        String += Safe(s);
        dirty = true;
        return this;
    }
    public StringList Insert(string s, int idx)
    {
        if (String.Length == 0 || idx >= Array.Length) return Add(s);

        int searchIdx = 0;
        for (int i = 0; i < idx; i++)
        {
            searchIdx += Array[i].Length + Delimiter.Length;
        }
        String = String.Insert(searchIdx, Safe(s) + Delimiter);

        return this;
    }

    public string Get(int idx) => (idx >= 0 && idx < Array.Length) ? Array[idx] : null;
    public StringList Set(string s, int idx)
    {
        if (String.Length == 0 || idx >= Array.Length) return Add(s);

        RemoveAt(idx); //lazy but probably works lol
        return Insert(s, idx);
    }

    public bool Remove(string s)
    {
        s = Safe(s);
        for (int i = 0; i < Array.Length; i++)
        {
            if (Array[i] == s)
            {
                RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    public int RemoveAll(string s) //sloppy but sufficient implementation
    {
        int i;
        for (i = 0; Remove(s); i++) ;
        return i;
    }
    public bool RemoveAt(int idx)
    {
        if (String.Length == 0 || idx >= Array.Length) return false;

        int searchIdx = 0;
        int i = 0;
        while (i < idx)
        {
            searchIdx += Array[i].Length + Delimiter.Length;
            i++;
        }

        if (i >= Array.Length - 1)
            String = String.Remove(searchIdx - Delimiter.Length); //remove it and the delimiter before
        else
            String = String.Remove(searchIdx, Array[i].Length + Delimiter.Length); //remove it and the delimiter after

        dirty = true;
        return true;
    }

    public override string ToString() => String;
    public override bool Equals(object obj) => String.Equals(obj);
    public override int GetHashCode() => String.GetHashCode();

    public static StringList operator +(StringList a, string b) => a.Add(b);
    public static IEnumerable<string> operator +(IEnumerable<string> a, StringList b) => a.Concat(b);

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)Array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Array.GetEnumerator();
    }
}
