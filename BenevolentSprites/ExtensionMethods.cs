using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO;

namespace ALib
{
    public static class JsonExtensions
    {
        public static string Escape(this string source, char escapeChar = '\\', params char[] charsToEscape)
        {
            var result = new StringBuilder(source.Length * 2);
            var escape = new HashSet<char>(charsToEscape) { escapeChar };
            foreach (var c in source)
            {
                if (escape.Contains(c))
                    result.Append(escapeChar);
                result.Append(c);
            }
            return result.ToString();
        }
        public static string Unescape(this string source, char escapeChar = '\\')
        {
            var result = new StringBuilder(source.Length);
            var escaped = false;
            foreach (var c in source)
                if (escaped || c != escapeChar)
                {
                    escaped = false;
                    result.Append(c);
                }
                else
                    escaped = true;
            return result.ToString();
        }

        public static int FindIndexUnescaped(this string source, char searchFor, char escapeChar = '\\', int startIndex = 0)
        {
            var escaped = false;
            for (int i = startIndex; i < source.Length; i++)
                if (source[i] == searchFor)
                    return i;
                else if (escaped || source[i] != escapeChar)
                    escaped = false;
                else
                    escaped = true;
            return -1;
        }
        internal static StringBuilder GetUnescaped(this IEnumerator<char> source, char searchFor, ref int pos, char escapeChar = '\\', int skip = 0)
        {
            var result = new StringBuilder();
            var escaped = false;
            pos += skip;
            for (int i = 0; i < skip; i++)
                source.MoveNext();
            while (source.MoveNext())
            {
                pos++;
                if (!escaped && source.Current == searchFor)
                    return result;
                if (escaped || source.Current != escapeChar)
                {
                    escaped = false;
                    result.Append(source.Current);
                }
                else
                    escaped = true;
            }
            return null;
        }

        public static bool Contains<T>(this T value, T flags, bool any = false) where T : Enum
        {
            try
            {
                var v = (value as IConvertible).ToInt64(CultureInfo.InvariantCulture);
                var f = (flags as IConvertible).ToInt64(CultureInfo.InvariantCulture);
                if (any)
                    return (v & f) != 0;
                return (v & f) == f;
            }
            catch
            {
                var v = (value as IConvertible).ToUInt64(CultureInfo.InvariantCulture);
                var f = (flags as IConvertible).ToUInt64(CultureInfo.InvariantCulture);
                if (any)
                    return (v & f) != 0;
                return (v & f) == f;
            }
        }
        public static bool IsCompilerGenerated(this MemberInfo member) => member.GetCustomAttributes(false).Any(x => x is CompilerGeneratedAttribute);
    }
}
