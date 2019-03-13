using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Globalization;

namespace Core
{
    public class PrivateProfile
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string defaultValue, IntPtr buffer, int bufferSize, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileSectionNames(IntPtr buffer, int bufferSize, string filePath);

        private string _profilePath = "";

        public PrivateProfile()
        {
            SetPath(Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".ini"));
        }

        public PrivateProfile(string path)
        {
            SetPath(path);
        }

        public void SetPath(string path)
        {
            _profilePath = path;
        }

        public string GetPath()
        {
            return (_profilePath);
        }

        public string[] EnumerateSections()
        {
            string value = "";
            IntPtr buffer = Marshal.AllocCoTaskMem(ushort.MaxValue);

            int bytesReturned = GetPrivateProfileSectionNames(buffer, ushort.MaxValue, _profilePath) - 1;
            if (bytesReturned > 0) value = Marshal.PtrToStringUni(buffer, bytesReturned);
            Marshal.FreeCoTaskMem(buffer);
            return (value.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public string[] EnumerateKeys(string section)
        {
            string value = "";
            IntPtr buffer = Marshal.AllocCoTaskMem(ushort.MaxValue);

            int bytesReturned = GetPrivateProfileString(section, null, null, buffer, ushort.MaxValue, _profilePath) - 1;
            if (bytesReturned > 0) value = Marshal.PtrToStringUni(buffer, bytesReturned);
            Marshal.FreeCoTaskMem(buffer);
            return (value.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries));
        }
              
        public string Get(string section, string key, string defaultValue = default(string), bool writeIt = false)
        {
            IntPtr buffer = Marshal.AllocCoTaskMem(ushort.MaxValue);

            int bytesReturned = GetPrivateProfileString(section, key, null, buffer, ushort.MaxValue, _profilePath);
            if (bytesReturned > 0) defaultValue = Marshal.PtrToStringUni(buffer, bytesReturned);
            else if (writeIt)
                Set(section, key, defaultValue);
            Marshal.FreeCoTaskMem(buffer);
            return (defaultValue);
        }

        public T Get<T>(string section, string key, T defaultValue = default(T), bool writeIt = false)
        {
            T result;
            if (TryGet<T>(section, key, out result)) return (result);
            if (writeIt)
                Set(section, key, defaultValue);
            return (defaultValue);            
        }

        public bool TryGet<T>(string section, string key, out T result)
        {
            string value = Get(section, key, null, false);
            if (value == null)
            {
                result = default(T);
                return (false);
            }            
            return (value.TryParse(out result));
        }

        public bool Set(string section, string key, string value)
        {   // set value to null for remove record
            if (!String.IsNullOrEmpty(value) && value.Length > 0 &&  
                (value.StartsWith(" ") || value.EndsWith(" "))) value = "'" + value + "'";
            return (WritePrivateProfileString(section, key, value, _profilePath));
        }

        public bool Set<T>(string section, string key, T value)
        {
            Type type = typeof(T);
            string valueString;

            if (value == null)
                valueString = null;
            else if (type == typeof(bool)) 
                valueString = ((bool)(object)value ? "1" : "0");
            else if (type == typeof(float)) 
                valueString = ((float)(object)value).ToString(CultureInfo.InvariantCulture);
            else if (type == typeof(double))
                valueString = ((double)(object)value).ToString(CultureInfo.InvariantCulture);
            else if (type == typeof(decimal))
                valueString = ((decimal)(object)value).ToString(CultureInfo.InvariantCulture);
            else
                valueString = value.ToString();

            return (Set(section, key, valueString));
        }
    }
}