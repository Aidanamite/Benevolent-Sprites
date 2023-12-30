using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;

namespace ALib
{
    public sealed class JSON : IList<JSON>, IDictionary<string, JSON>
    {
        public readonly static CultureInfo Culture = CultureInfo.InvariantCulture;
        public static JSON EmptyArray => new JSON() { array = new List<JSON>() };
        public static JSON EmptyObject => new JSON() { map = new Dictionary<string, JSON>() };
        JSON() { }
        public JSON(string value) => SetString(value);
        public static implicit operator string(JSON j) => j.String;
        public static implicit operator JSON(string v) => new JSON(v);
        public JSON(bool value) => SetNumericValue(value);
        public static implicit operator bool(JSON j) => j.Boolean;
        public static implicit operator JSON(bool v) => new JSON(v);
        public JSON(sbyte value) => SetNumericValue(value);
        public static implicit operator sbyte(JSON j) => j.SByte;
        public static implicit operator JSON(sbyte v) => new JSON(v);
        public JSON(byte value) => SetNumericValue(value);
        public static implicit operator byte(JSON j) => j.Byte;
        public static implicit operator JSON(byte v) => new JSON(v);
        public JSON(ushort value) => SetNumericValue(value);
        public static implicit operator ushort(JSON j) => j.UShort;
        public static implicit operator JSON(ushort v) => new JSON(v);
        public JSON(short value) => SetNumericValue(value);
        public static implicit operator short(JSON j) => j.Short;
        public static implicit operator JSON(short v) => new JSON(v);
        public JSON(uint value) => SetNumericValue(value);
        public static implicit operator uint(JSON j) => j.UInteger;
        public static implicit operator JSON(uint v) => new JSON(v);
        public JSON(int value) => SetNumericValue(value);
        public static implicit operator int(JSON j) => j.Integer;
        public static implicit operator JSON(int v) => new JSON(v);
        public JSON(ulong value) => SetNumericValue(value);
        public static implicit operator ulong(JSON j) => j.ULong;
        public static implicit operator JSON(ulong v) => new JSON(v);
        public JSON(long value) => SetNumericValue(value);
        public static implicit operator long(JSON j) => j.Long;
        public static implicit operator JSON(long v) => new JSON(v);
        public JSON(float value) => SetNumericValue(value);
        public static implicit operator float(JSON j) => j.Float;
        public static implicit operator JSON(float v) => new JSON(v);
        public JSON(double value) => SetNumericValue(value);
        public static implicit operator double(JSON j) => j.Double;
        public static implicit operator JSON(double v) => new JSON(v);
        public JSON(JSON value, bool deepCopy = true) => Copy(value, deepCopy);
        public JSON(object obj)
        {
            if (obj is Enum e)
                obj = (obj as IConvertible).ToType(Enum.GetUnderlyingType(obj.GetType()), Culture);
            if (obj is DateTime dateTime)
                obj = dateTime.ToString(Culture);
            if (obj is TimeSpan timeSpan)
                obj = timeSpan.Ticks;
            if (obj is string s)
            {
                SetString(s);
                return;
            }
            if (obj is bool || obj is sbyte || obj is byte || obj is ushort || obj is short || obj is uint || obj is int || obj is ulong || obj is long || obj is float || obj is double)
            {
                SetNumericValue(obj as IConvertible);
                return;
            }
            if (obj is JSON j)
            {
                Copy(j, true);
                return;
            }
            var t = obj.GetType();
            var i = t.GetInterfaces();
            if (obj is IDictionary d)
                if (i.FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>) && x.GetGenericArguments()[0] == typeof(string)) != null)
                {
                    map = new Dictionary<string, JSON>(d.Count);
                    foreach (string key in d.Keys)
                        map.Add(key, new JSON(d[key]));
                    return;
                }
            if (obj is IList l)
            {
                array = new List<JSON>(l.Count);
                foreach (var o in l)
                    array.Add(new JSON(o));
                return;
            }
            if (t.Name.StartsWith("<>") && t.Name.Contains("AnonymousType"))
            {
                map = new Dictionary<string, JSON>();
                foreach (var p in t.GetProperties(~BindingFlags.Default))
                    if (p.GetGetMethod() != null)
                        map.Add(p.Name, new JSON(p.GetValue(obj,null)));
                return;
            }
            map = new Dictionary<string, JSON>();
            do
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && !f.IsCompilerGenerated())
                            map.Add(f.Name, new JSON(f.GetValue(obj)));
                foreach (var p in t.GetProperties(~BindingFlags.Default))
                    if (p.GetGetMethod() != null && !p.GetGetMethod().IsStatic && p.GetGetMethod().IsCompilerGenerated())
                            map.Add(p.Name, new JSON(p.GetValue(obj,null)));
            } while ((t = t.BaseType) != typeof(object));
        }

        void Reset()
        {
            array = null;
            map = null;
            FormatFlags = ValueFormat.None;
        }
        void SetString(string value)
        {
            Reset();
            _value = value;
        }
        void SetNumericValue(IConvertible value)
        {
            Reset();
            _value = value;
            if (value is float f)
                SetFloating(f);
            else if (value is double d)
                SetFloating(d);
            else if (((0 as IConvertible).ToType(_value.GetType(), Culture) as IComparable).CompareTo(_value) > 0)
            {
                var l = _value.ToInt64(Culture);
                SetSigned(l);
                SetFloating(l,false);

            }
            else if (_value is ulong ul)
            {
                SetUnsigned(ul);
                if (ul <= long.MaxValue)
                    SetSigned((long)ul);
                SetFloating(ul, false);
            }
            else
            {
                var l = _value.ToInt64(Culture);
                SetUnsigned((ulong)l);
                SetSigned(l);
                SetFloating(l, false);
            }
        }
        void SetFloating(double value, bool setOther = true)
        {
            FormatFlags |= ValueFormat.Double | ValueFormat.Float;
            if (!setOther || double.IsNaN(value) || double.IsInfinity(value) || value % 1 != 0)
                return;
            if (value < 0)
            {
                if (value >= long.MinValue)
                    SetSigned((long)value);
            }
            else
            {
                if (value <= ulong.MaxValue)
                    SetUnsigned((ulong)value);
                if (value <= long.MaxValue)
                    SetSigned((long)value);
            }
        }
        void SetSigned(long value)
        {
            FormatFlags |= ValueFormat.Long;
            if (int.MinValue <= value && value <= int.MaxValue)
            {
                FormatFlags |= ValueFormat.Integer;
                if (short.MinValue <= value && value <= short.MaxValue)
                {
                    FormatFlags |= ValueFormat.Short;
                    if (sbyte.MinValue <= value && value <= sbyte.MaxValue)
                        FormatFlags |= ValueFormat.SByte;
                }
            }
        }
        void SetUnsigned(ulong value)
        {
            FormatFlags |= ValueFormat.ULong;
            if (uint.MinValue <= value && value <= uint.MaxValue)
            {
                FormatFlags |= ValueFormat.UInteger;
                if (ushort.MinValue <= value && value <= ushort.MaxValue)
                {
                    FormatFlags |= ValueFormat.UShort;
                    if (byte.MinValue <= value && value <= byte.MaxValue)
                    {
                        FormatFlags |= ValueFormat.Byte;
                        if (value == 0 || value == 1)
                            FormatFlags |= ValueFormat.Boolean;
                    }
                }
            }
        }
        void Copy(JSON value, bool deepCopy)
        {
            if (value.IsArray)
            {
                array = new List<JSON>(value.Count);
                foreach (var i in value.array)
                    if (deepCopy)
                        array.Add(new JSON(i, true));
                    else
                        array.Add(i);
            }
            else
            if (value.IsObject)
            {
                map = new Dictionary<string, JSON>(value.Count);
                foreach (var i in value.map)
                    if (deepCopy)
                        map.Add(i.Key, new JSON(i.Value, true));
                    else
                        map.Add(i.Key, i.Value);
            }
            else
            {
                FormatFlags = value.FormatFlags;
                _value = value._value;
            }
        }
        List<JSON> array;
        Dictionary<string, JSON> map;
        public bool IsArray => array != null;
        public bool IsObject => map != null;
        public bool IsCollection => IsArray || IsObject;
        public bool IsString => !IsArray && !IsObject && FormatFlags == ValueFormat.None;
        public ValueFormat FormatFlags { protected set; get; }
        IConvertible _value;
        public string String => CanBeString ? _value.ToString(Culture) : "";
        public bool CanBeString => !IsArray && !IsObject;
        public bool Boolean => CanBeBoolean ? _value.ToBoolean(Culture) : default;
        public bool CanBeBoolean => FormatFlags.Contains(ValueFormat.Boolean);
        public long Long => CanBeLong ? _value.ToInt64(Culture) : default;
        public bool CanBeLong => FormatFlags.Contains(ValueFormat.Long);
        public int Integer => CanBeInteger ? _value.ToInt32(Culture) : default;
        public bool CanBeInteger => FormatFlags.Contains(ValueFormat.Integer);
        public short Short => CanBeShort ? _value.ToInt16(Culture) : default;
        public bool CanBeShort => FormatFlags.Contains(ValueFormat.Short);
        public sbyte SByte => CanBeSByte ? _value.ToSByte(Culture) : default;
        public bool CanBeSByte => FormatFlags.Contains(ValueFormat.SByte);
        public ulong ULong => CanBeULong ? _value.ToUInt64(Culture) : default;
        public bool CanBeULong => FormatFlags.Contains(ValueFormat.ULong);
        public uint UInteger => CanBeUInteger ? _value.ToUInt32(Culture) : default;
        public bool CanBeUInteger => FormatFlags.Contains(ValueFormat.UInteger);
        public ushort UShort => CanBeUShort ? _value.ToUInt16(Culture) : default;
        public bool CanBeUShort => FormatFlags.Contains(ValueFormat.UShort);
        public byte Byte => CanBeByte ? _value.ToByte(Culture) : default;
        public bool CanBeByte => FormatFlags.Contains(ValueFormat.Byte);
        public double Double => CanBeDouble ? _value.ToDouble(Culture) : default;
        public bool CanBeDouble => FormatFlags.Contains(ValueFormat.Double);
        public float Float => CanBeFloat ? _value.ToSingle(Culture) : default;
        public bool CanBeFloat => FormatFlags.Contains(ValueFormat.Float);
        public bool TryAsEnum<T>(out T value) where T : struct, Enum
        {
            if (FormatFlags.Contains(ValueFormat.ULong))
            {
                value = (T)Enum.ToObject(typeof(T), ULong);
                return (value as IConvertible).ToUInt64(Culture) == ULong;
            }
            if (FormatFlags.Contains(ValueFormat.Long))
            {
                value = (T)Enum.ToObject(typeof(T), Long);
                return (value as IConvertible).ToInt64(Culture) == Long;
            }
            if (String != null)
            {
                try
                {
                    value = (T)Enum.Parse(typeof(T), String);
                    return true;
                } catch { }
            }
            value = default;
            return false;
        }
        public JSON this[string key]
        {
            get
            {
                EnsureObject();
                return map[key];
            }
            set
            {
                EnsureObject();
                map[key] = value;
            }
        }
        public JSON this[int index]
        {
            get
            {
                EnsureArray();
                return array[index];
            }
            set
            {
                EnsureArray();
                array[index] = value;
            }
        }
        public int Count => map?.Count ?? array?.Count ?? -1;
        void EnsureArray()
        {
            if (!IsArray)
                throw new InvalidOperationException($"This JSON is not an Array. Check \"{nameof(IsArray)}\" before calling this method");
        }
        void EnsureObject()
        {
            if (!IsObject)
                throw new InvalidOperationException($"This JSON is not an Object. Check \"{nameof(IsObject)}\" before calling this method");
        }
        void EnsureCollection()
        {
            if (!IsCollection)
                throw new InvalidOperationException($"This JSON is not a Collection. Check \"{nameof(IsObject)}\" or \"{nameof(IsArray)}\" before calling this method");
        }
        public void Clear()
        {
            EnsureCollection();
            if (IsObject)
                map.Clear();
            else if (IsArray)
                array.Clear();
        }
        public void Add(JSON obj)
        {
            EnsureArray();
            array.Add(obj);
        }
        public void Add(string key, JSON obj)
        {
            EnsureObject();
            map.Add(key, obj);
        }
        public bool TryAdd(string key, JSON obj)
        {
            EnsureObject();
            if (map.ContainsKey(key))
                return false;
            map[key] = obj;
            return true;
        }

        public void Insert(int index, JSON obj)
        {
            EnsureArray();
            array.Insert(index, obj);
        }

        public int IndexOf(JSON obj)
        {
            EnsureArray();
            return array.IndexOf(obj);
        }

        public void RemoveAt(int index)
        {
            EnsureArray();
            array.RemoveAt(index);
        }

        public bool Contains(JSON obj)
        {
            EnsureCollection();
            if (IsArray) return array.Contains(obj);
            if (IsObject) return map.ContainsValue(obj);
            return false;
        }

        public void CopyTo(JSON[] array, int arrayIndex)
        {
            EnsureArray();
            array.CopyTo(array, arrayIndex);
        }

        public bool Remove(JSON obj)
        {
            EnsureArray();
            return array.Remove(obj);
        }
        public bool IsReadOnly => false;
        public IEnumerator<JSON> GetEnumerator()
        {
            EnsureCollection();
            if (IsArray)
                return array.GetEnumerator();
            return map.Values.GetEnumerator();
        }
        public bool ContainsKey(string key)
        {
            EnsureObject();
            return map.ContainsKey(key);
        }
        public bool Remove(string key)
        {
            EnsureObject();
            return map.Remove(key);
        }
        public bool TryGetValue(string key, out JSON value)
        {
            EnsureObject();
            return map.TryGetValue(key, out value);
        }
        public ICollection<string> Keys
        {
            get
            {
                EnsureObject();
                return map.Keys;
            }
        }
        public ICollection<JSON> Values
        {
            get
            {
                EnsureObject();
                return map.Values;
            }
        }
        public override string ToString() => ToString(false);
        public string ToString(bool indent) => ToString(indent ? "\t" : "", indent ? "\n" : " ");
        public string ToString(string indent, string linebreak = "\n", string currentIndent = "")
        {
            var i = false;
            if (IsArray || IsObject)
            {
                if (IsObject && Count > 5)
                    i = true;
                else
                {
                    var total = 0;
                    foreach (var item in this)
                        if (item.IsString)
                        {
                            total += item.String.Length;
                            if (total > 50)
                            {
                                i = true;
                                break;
                            }
                        }
                        else
                        {
                            i = true;
                            break;
                        }
                    if (!i && IsObject)
                        foreach (var item in Keys)
                            total += item.Length;
                    if (total > 50)
                        i = true;
                }
            }
            if (IsArray)
            {
                var r = new StringBuilder();
                foreach (var item in this)
                    if (item != null)
                    {
                        if (r.Length != 0)
                            r.Append(",");
                        if (i)
                            r.Append(linebreak + currentIndent + indent);
                        else
                            r.Append(" ");
                        r.Append(item.ToString(indent, linebreak, currentIndent + indent));
                    }
                if (i)
                {
                    r.Append(linebreak);
                    r.Append(currentIndent);
                }
                else
                    r.Append(" ");
                r.Insert(0, "[");
                r.Append("]");
                return r.ToString();
            }
            if (IsObject)
            {
                var r = new StringBuilder();
                foreach (var item in map)
                    if (item.Value != null)
                    {
                        if (r.Length != 0)
                            r.Append(",");
                        if (i)
                        {
                            r.Append(linebreak);
                            r.Append(currentIndent);
                            r.Append(indent);
                        }
                        else
                            r.Append(" ");
                        r.Append("\"");
                        r.Append(item.Key.Escape('\\', '"'));
                        r.Append("\": ");
                        r.Append(item.Value.ToString(indent, linebreak, currentIndent + indent));
                    }
                if (i)
                {
                    r.Append(linebreak);
                    r.Append(currentIndent);
                }
                else
                    r.Append(" ");
                r.Insert(0, "{");
                r.Append("}");
                return r.ToString();
            }
            if (FormatFlags == ValueFormat.None)
            {
                var r = new StringBuilder();
                r.Append("\"");
                r.Append(String?.Escape('\\', '"'));
                r.Append("\"");
                return r.ToString();
            }
            return String;
        }
        class FileStreamEnumerable : IEnumerable<char>
        {
            FileStream reader;
            public FileStreamEnumerable(FileStream source) => reader = source;
            public IEnumerator<char> GetEnumerator() => new Enumerator(reader);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            class Enumerator : IEnumerator<char>
            {
                FileStream reader;
                char last;
                public Enumerator(FileStream source) => reader = source;

                public char Current => last;

                object IEnumerator.Current => Current;

                public void Dispose() => reader.Dispose();

                public bool MoveNext()
                {
                    var next = reader.ReadByte();
                    if (next == -1)
                    {
                        last = default;
                        return false;
                    }
                    last = (char)next;
                    return true;
                }

                public void Reset()
                {
                    reader.Position = 0;
                    last = default;
                }
            }
        }
        public static JSON Parse(FileStream reader) => Parse(new FileStreamEnumerable(reader));
        public static JSON Parse(IEnumerable<char> source)
        {
            if (source == null || !source.GetEnumerator().MoveNext())
                return new JSON("");
            JSON top = null;
            var stack = new Stack<JSON>();
            bool reading = false;
            bool closedCollection = false;
            StringBuilder read = null;
            StringBuilder read2 = null;
            long line = 1;
            long col = 0;
            var i = 0;
            var enumer = source.GetEnumerator();
            while (enumer.MoveNext())
            {
                i++;
                var c = enumer.Current;
                
                if (stack.Count == 0 && top != null)
                    return top;
                if (c == '/' && enumer.MoveNext())
                {
                    i++;
                    c = enumer.Current;
                    if (c == '/')
                    {
                        do
                            i++;
                        while (enumer.MoveNext() && char.GetUnicodeCategory(enumer.Current) != UnicodeCategory.LineSeparator);
                        line++;
                        col = i;
                        continue;
                    }
                    if (c == '*')
                    {
                        i += 2;
                        bool last = false;
                        while (enumer.MoveNext() && !(last && enumer.Current != '/'))
                        {
                            if (char.GetUnicodeCategory(enumer.Current) == UnicodeCategory.LineSeparator)
                            {
                                line++;
                                col = i;
                            }
                            last = enumer.Current == '*';
                            i++;
                        }
                        i++;
                        continue;
                    }
                    throw new FormatException($"Unexpected \"/\" at line:{line} col:{i - col - 1} got \"{c}\"");
                }
                if (stack.Count > 0)
                {
                    if (stack.Peek().IsArray ? c == ',' || c == ']' : stack.Peek().IsObject ? c == ',' || c == '}' : false)
                    {
                        var value = c == ']' || c == '}' ? stack.Pop() : stack.Peek();
                        if (read != null)
                        {
                            if (value.IsArray)
                                value.Add(ParseRaw(read.ToString()));
                            else if (read2 == null)
                                throw new FormatException($"Expected \":\" at line:{line} col:{i - col} got \"{c}\"");
                            else
                                value[GetString(read2.ToString())] = ParseRaw(read.ToString());
                        }
                        else if (value.IsObject && read2 != null)
                            throw new FormatException($"Expected value at line:{line} col:{i - col} got \"{c}\"");
                        read2 = null;
                        read = null;
                        reading = false;
                        closedCollection = c != ',';
                        continue;
                    }
                    if (stack.Peek().IsObject && c == ':')
                    {
                        if (read == null)
                            throw new FormatException($"Expected key at line:{line} col:{i - col} got \":\"");
                        read2 = read;
                        read = null;
                        reading = false;
                        continue;
                    }
                }
                if (char.GetUnicodeCategory(c) == UnicodeCategory.LineSeparator)
                {
                    line++;
                    col = i;
                }
                if (!reading)
                {
                    if (char.IsWhiteSpace(c))
                        continue;
                    if (read != null && stack.Count != 0 && (closedCollection || !stack.Peek().IsObject || read2 != null))
                    {
                        if (!closedCollection && stack.Peek().IsObject && read2 == null)
                            throw new FormatException($"Expected \":\" at line:{line} col:{i - col} got \"{c}\"");
                        throw new FormatException($"Expected \",\" at line:{line} col:{i - col} got \"{c}\"");
                    }
                    if (c == '{' || c == '[')
                    {
                        var newObj = c == '[' ? EmptyArray : EmptyObject;
                        if (stack.Count > 0)
                        {
                            if (stack.Peek().IsObject)
                            {
                                if (read2 == null)
                                    throw new FormatException($"Expected key at line:{line} col:{i - col} got \"{c}\"");
                                stack.Peek()[GetString(read2.ToString())] = newObj;
                                read2 = null;
                            }
                            else
                                stack.Peek().Add(newObj);
                        }
                        else if (top == null)
                            top = newObj;
                        stack.Push(newObj);
                        continue;
                    }
                    if (c == '"' || c == '\'')
                    {
                        int p = 0;
                        read = enumer.GetUnescaped(c,ref p);
                        if (read == null)
                            throw new FormatException($"Unterminated JSON string at line:{line} col:{i - col}");
                        for (var cp = 0; cp < read.Length; cp++)
                            if (char.GetUnicodeCategory(read[cp]) == UnicodeCategory.LineSeparator)
                            {
                                line++;
                                col = i + cp;
                            }
                        i += p;
                        continue;
                    }
                    read = new StringBuilder(c.ToString());
                    reading = true;
                }
                else
                {
                    if (char.IsWhiteSpace(c))
                        reading = false;
                    else
                        read.Append(c);
                }
            }
            if (top == null)
            {
                if (read2 != null)
                    top = ParseRaw(read2.ToString());
                else if (read != null)
                    top = ParseRaw(read.ToString());
                else
                    top = new JSON("");
            }
            if (stack.Count != 0)
                throw new FormatException("Unterminated JSON " + (stack.Peek().IsObject ? "object. Expecting \"}\"" : stack.Peek().IsArray ? "array. Expecting \"]\"" : "structure"));
            return top;
        }
        static string GetString(string value)
        {
            if (value.StartsWith("\"") || value.StartsWith("'"))
                return value.Substring(1, value.Length - 2).Unescape();
            return value;
        }
        static JSON ParseRaw(string value)
        {
            if (value.StartsWith("\"") || value.StartsWith("'"))
                return new JSON(GetString(value));
            if (!value.StartsWith("-") && ulong.TryParse(value, out var u))
                return new JSON(u);
            if (long.TryParse(value, out var l))
                return new JSON(l);
            if (double.TryParse(value, out var d))
                return new JSON(d);
            return new JSON(value);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<KeyValuePair<string, JSON>>.Add(KeyValuePair<string, JSON> item)
        {
            EnsureObject();
            (map as ICollection<KeyValuePair<string, JSON>>).Add(item);
        }
        bool ICollection<KeyValuePair<string, JSON>>.Remove(KeyValuePair<string, JSON> item)
        {
            EnsureObject();
            return (map as ICollection<KeyValuePair<string, JSON>>).Remove(item);
        }
        void ICollection<KeyValuePair<string, JSON>>.CopyTo(KeyValuePair<string, JSON>[] items, int index)
        {
            EnsureObject();
            (map as ICollection<KeyValuePair<string, JSON>>).CopyTo(items, index);
        }
        bool ICollection<KeyValuePair<string, JSON>>.Contains(KeyValuePair<string, JSON> item)
        {
            EnsureObject();
            return (map as ICollection<KeyValuePair<string, JSON>>).Contains(item);
        }
        IEnumerator<KeyValuePair<string, JSON>> IEnumerable<KeyValuePair<string, JSON>>.GetEnumerator()
        {
            EnsureObject();
            return map.GetEnumerator();
        }

        [Flags]
        public enum ValueFormat
        {
            None = 0,
            SByte = 1,
            Short = 2,
            Integer = 4,
            Long = 8,
            Byte = 16,
            UShort = 32,
            UInteger = 64,
            ULong = 128,
            Float = 256,
            Double = 512,
            Boolean = 1024
        }
    }
}
