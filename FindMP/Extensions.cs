using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Controtex
{
    public static class Extensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> range)
        {
            foreach (T item in range)
                collection.Add(item);
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static bool IsSet(this int n, Enum flag)
        {
            return (n & (int) (object) flag) == (int) (object) flag;
        }

        public static bool IsNullOrEmpty(this string text)
        {
            return String.IsNullOrEmpty(text);
        }

        public static bool IsNotEmpty(this string text)
        {
            return !String.IsNullOrEmpty(text);
        }

        public static bool IsInt(this string x)
        {
            
            var rx = new Regex("^ *[0-9][0-9]* *$");
            return rx.IsMatch(x);
        }

        public static DateTime? ToDateTime(this object data, DateTime? defValue)
        {
            return ToDateTime(data, defValue, null);
        }

        public static DateTime? ToDateTime(this object data, CultureInfo ci)
        {
            return ToDateTime(data, null, ci);
        }

        public static DateTime? ToDateTime(this object data, DateTime? defValue = null, CultureInfo ci = null)
        {
            if (data is DateTime)
                return (DateTime) data;
            if (data is string)
            {
                DateTime d;
                if (ci == null)
                    DateTime.TryParse((string) data, out d);
                else
                {
                    DateTime.TryParse((string) data, ci, DateTimeStyles.None, out d);
                }
                if (d != DateTime.MinValue) return d;
            }
            return defValue;
        }


        public static int ToInt32(this object data, int defValue = 0)
        {
            if (data == null) return defValue;
            if (data is string)
            {
                int val;
                var text = (string) data;
                if (!Int32.TryParse(text, out val))
                    val = defValue;
                return val;
            }
            if (data is short)
                return (Int16) data;
            if (data is int)
                return (Int32) data;
            if (data is long)
                return (int) (Int64) data;
            return defValue;
        }

        public static Decimal ToDecimal(this object data, Decimal defValue, CultureInfo culture)
        {
            if (data == null) return defValue;
            if (data is string)
            {
                decimal val;
                var text = (string) data;
                if (
                    !Decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, culture,
                                      out val))
                    val = defValue;
                return val;
            }
            if (data is short)
                return new Decimal((short) data);
            if (data is int)
                return new Decimal((int)data);
            if (data is long)
                return new Decimal((long) data);
            if (data is double)
                return new Decimal((double) data);
            if (data is float)
                return new Decimal((float) data);
            return defValue;
        }

        public static Decimal ToDecimal(this object data, CultureInfo culture)
        {
            return ToDecimal(data, 0m, culture);
        }

        public static Decimal ToDecimal(this object data, Decimal defValue = 0m)
        {
            return ToDecimal(data, defValue, CultureInfo.CurrentCulture);
        }

        public static double ToDouble(this object obj, bool forceInvariant = false)
        {
            return ToDouble(obj, 0.0, forceInvariant);
        }

        public static double ToDouble(this object obj, double defaultValue, bool forceInvariant = false)
        {
            if (obj is double)
                return (double) obj;
            if (obj is string)
                return ((string) obj).ToDouble(defaultValue, forceInvariant);
            return defaultValue;
        }

        public static double ToDouble(this string text, bool forceInvariant = false)
        {
            return ToDouble(text, 0.0, forceInvariant);
        }

        public static double ToDouble(this string text, double defaultValue, bool forceInvariant = false)
        {
            double val;
            if (forceInvariant || !double.TryParse(text, out val))
                if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
                    val = defaultValue;
            return val;
        }

        public static string Join(this string[] s, string separator)
        {
            if (s.Length == 0) return "";
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length - 1; i++)
                sb.Append(s[i]).Append(separator);
            sb.Append(s[s.Length - 1]);
            return sb.ToString();
        }

        public static string CleanWhitespace(this string s)
        {
            return Regex.Replace(s, @"\s+", " ").Trim();
        }
    }
}