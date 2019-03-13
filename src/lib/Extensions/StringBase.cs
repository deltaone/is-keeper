using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Core
{
    public static partial class StringExtensions
    {
        public static bool IsEmpty(this string input)
        {   // bool isTrulyEmpty = String.IsNullOrWhiteSpace(source); // DOTNET4        
            if (string.IsNullOrEmpty(input) || input.Trim().Length == 0) return (true);
            return (false);
        }

        public static bool IsASCII(this string input)
        {
            foreach (char ch in input)
                if (ch > 0xff) return false;
            return true;
        }

        public static bool In(this string input, bool ignoreCase, params string[] stringValues)
        {
            foreach (string value in stringValues)
                if (string.Compare(input, value, ignoreCase) == 0)
                    return (true);
            return (false);
        }

        public static string Right(this string input, int length)
        {
            return (input != null && input.Length > length ? input.Substring(input.Length - length) : input);
        }

        public static string Left(this string input, int length)
        {
            return (input != null && input.Length > length ? input.Substring(0, length) : input);
        }

        public static string Reverse(this string input)
        {
            char[] a = input.ToCharArray();
            Array.Reverse(a);
            return (new string(a));
        }

        public static string FMT(this string input, params object[] args)
        {
            return (string.Format(input, args));
        }

        //---------------------------------------------------------------------
    }
}
