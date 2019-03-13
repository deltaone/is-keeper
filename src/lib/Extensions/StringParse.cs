using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;

namespace Core
{
    public static partial class StringExtensions
    {
		public static bool IsNumeric(this string s)
		{
			return float.TryParse(s, out float output);
		}

		public static List<string> SplitSafe(this string input, string separator)
        {
            return(input.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries).
                Select(e => e.Trim()).Where(e => e.Length > 0).ToList());
        }

        public static int ParseIntFast(string input)
        {
            int result = 0, sign = 1, i = 0;

            if (input[0] == '-')
            {
                sign = -1;
                i = 1;
            }

            for (; i < input.Length; i++)
                result = result * 10 + (input[i] - '0');
            return (result * sign);
        }
 
        public static T Parse<T>(this string input, T defaultValue = default(T))
        {   // var i = task["depth"].Parse(7); var i = task["depth"].Parse<int>();
            T result;
            if (TryParse(input, out result)) return (result);
            return (defaultValue);
        }

        public static bool TryParse<T>(this string input, out T result)
        {
            Type type = typeof(T);
            result = default(T);
 
            if (String.IsNullOrEmpty(input) || input.Trim().Length == 0)
                return (false);

            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>))) 
                type = Nullable.GetUnderlyingType(type); 
            try
            {
                if (type == typeof(bool))
                {
                    input = input.ToLowerInvariant().Trim();
                    if (input == "on" || input == "1" || input == "true")
                        result = (T)(object)true;
                    else if (input == "off" || input == "0" || input == "false")
                        result = (T)(object)false;
                    else
                        return (false);
                } 
                else if (typeof(IConvertible).IsAssignableFrom(type))
                    result = (T)Convert.ChangeType(input, type, CultureInfo.InvariantCulture);
                else if (type.IsEnum)
                    result = (T)Enum.Parse(type, input, true);
                else if (GenericParseHelper<T>.TryParse != null)
                    return (GenericParseHelper<T>.TryParse(input, out result));
                else
                    return (false);
                return (true);
            }
            catch
            {
                return (false);
            }
        }

        private static class GenericParseHelper<T>
        {
            public delegate bool TryParseFunction(string input, out T result);           
            private static TryParseFunction fnTryParse;
            private static bool canUseTryParse = true;

            public static TryParseFunction TryParse
            {
                get
                {
                    if (!canUseTryParse) return (null);
                    if (fnTryParse != null) return (fnTryParse);
                    MethodInfo method = typeof(T).GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, 
                        new[] { typeof(string), typeof(T).MakeByRefType() }, null);
                    if (method == null)
                    {
                        canUseTryParse = false;
                        return (null);
                    }
                    fnTryParse = Delegate.CreateDelegate(typeof(TryParseFunction), method) as TryParseFunction;
                    return (fnTryParse);
                }
            }
        }
        //---------------------------------------------------------------------
    }
}
