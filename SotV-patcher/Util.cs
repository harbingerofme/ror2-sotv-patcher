using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
