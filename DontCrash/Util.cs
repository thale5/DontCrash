using System;
using UnityEngine;
using System.Linq;
using System.Reflection;
using ColossalFramework.IO;
using System.IO;
using ColossalFramework;
using System.Threading;

namespace DontCrash
{
    public static class Util
    {
        public static void DebugPrint(params object[] args)
        {
            string s = string.Format("[DontCrash] {0}", " ".OnJoin(args));
            Debug.Log(s);
        }

        public static string OnJoin(this string delim, params object[] args)
        {
            return string.Join(delim, args.Select(o => o?.ToString() ?? "null").ToArray());
        }

        public static T[] RemoveAt<T>(this T[] array, int index)
        {
            T[] dest = new T[array.Length - 1];

            if (index > 0)
                Array.Copy(array, 0, dest, 0, index);

            if (index < array.Length - 1)
                Array.Copy(array, index + 1, dest, index, array.Length - index - 1);

            return dest;
        }

        public static bool IsBitSet(this int value, int pos) => (value & 1 << pos) != 0;
        public static int SetBit(this int value, int pos) => value | 1 << pos;
    }
}
