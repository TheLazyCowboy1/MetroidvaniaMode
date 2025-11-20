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
        String = s;
    }
    public StringList(string s, string delimiter)
    {
        String = s;
        Delimiter = delimiter;
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
    private void RemoveAtPos(int pos)
    {
        String.Remove(pos, Move(1, pos) - pos);
    }
    public bool RemoveAt(int idx)
    {
        if (String.Length == 0 || idx >= Array.Length) return false;

        if (Array.Length <= 1) //if there isn't even a delimiter to find, just clear the string
        {
            Clear();
            return true;
        }

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
