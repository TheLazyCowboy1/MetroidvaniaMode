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
                _array = new string[d.Length - 1];
                for (int i = 0; i < _array.Length; i++)
                {
                    _array[i] = Unsafe(d[i]);
                }
            }
            return _array;
        }
        private set => _array = value;
    }

    private string _string = "";
    public string String { get => _string; private set { dirty = true; _string = value; } }

    private string _delimiter = ";";
    public string Delimiter { get => _delimiter; private set => _delimiter = value; }

    public StringList()
    {

    }
    public StringList(string s)
    {
        if (s.Length > 0 && !s.EndsWith(Delimiter))
            s += Delimiter; //for compatibility with past attempts that did not use Delimiter as a terminator
        String = s;
    }
    public StringList(string s, string delimiter)
    {
        Delimiter = delimiter;
        if (s.Length > 0 && !s.EndsWith(Delimiter))
            s += Delimiter; //for compatibility with past attempts that did not use Delimiter as a terminator
        String = s;
    }

    private string Safe(string s) => s == null ? "<NULL>" : s.Replace(Delimiter, "<ldel>");
    private string Unsafe(string s) => s == "<NULL>" ? null : s.Replace("<ldel>", Delimiter);

    private int Move(int count, int startPos = 0)
    {
        for (int i = 0; i < count; i++)
        {
            startPos = String.IndexOf(Delimiter, startPos);
            if (startPos < 0) return String.Length; //we've reached the end of the string!
            startPos += Delimiter.Length;
        }
        return startPos;
    }
    public StringList Add(string s)
    {
        //if (String.Length > 0) String += Delimiter;
        String += Safe(s) + Delimiter;
        return this;
    }
    private StringList InsertAtPos(string s, int pos)
    {
        String = String.Insert(pos, Safe(s) + Delimiter);
        return this;
    }
    public StringList Insert(string s, int idx)
    {
        //if (String.Length == 0 || idx >= Array.Length) return Add(s);
        return InsertAtPos(s, Move(idx));
    }

    public string Get(int idx) => (idx >= 0 && idx < Array.Length) ? Array[idx] : null;
    public StringList Set(string s, int idx)
    {
        //extend array to fit 
        int lenDif = idx - Array.Length; //we need to add this many nulls to pad the length
        for (int i = 0; i < lenDif; i++) Add(null);

        //if we can, just add
        if (String.Length == 0 || idx >= Array.Length) return Add(s);

        //just remove it and then re-insert it, lol
        int pos = Move(idx);
        RemoveAtPos(pos);
        return InsertAtPos(s, pos);
    }
    public StringList Set(string[] array)
    {
        Clear();
        foreach (string s in array) Add(s);
        return this;
    }
    public StringList SetLength(int length)
    {
        if (length < Array.Length) //shorten string
        {
            String = String.Remove(Move(length));
        }
        else //lengthen string
        {
            int lenDif = length - Array.Length; //we need to add this many nulls to pad the length
            for (int i = 0; i < lenDif; i++) Add(null);
        }
        return this;
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
    private void RemoveAtPos(int pos)
    {
        String = String.Remove(pos, Move(1, pos) - pos);
    }
    public bool RemoveAt(int idx)
    {
        if (String.Length == 0 || idx >= Array.Length) return false;

        RemoveAtPos(Move(idx));

        return true;
    }

    public void Clear() => String = "";

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
