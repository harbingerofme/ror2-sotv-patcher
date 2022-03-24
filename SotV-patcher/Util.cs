using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SotV_patcher
{
    internal static class Util
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> keyValuePair, out T1 key, out T2 value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }

        public static string Log(this TypeReference typeRef)
        {
            return $"{typeRef.Module.Assembly.Name} : {typeRef.FullName}";
        }

        public static bool IsEqual(this IEnumerable<string> input, IEnumerable<string> comparedTo)
        {
            if (input == null || comparedTo == null || input.Count() != comparedTo.Count())
            {
                return false;
            } 
            for (int i = 0; i < input.Count(); i++)
            {
                if (input.ElementAtOrDefault(i) != comparedTo.ElementAtOrDefault(i))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
