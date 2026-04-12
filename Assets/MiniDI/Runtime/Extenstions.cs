using System;

namespace MiniDI
{
    public static class Extenstions
    {
        public static string GetNiceTypeName(this Type type)
        {
            if (!type.IsGenericType && !type.IsGenericTypeDefinition)
                return type.Name;

            string name = type.Name;
            int backtickIndex = name.IndexOf('`');
            if (backtickIndex > 0)
            {
                name = name.Substring(0, backtickIndex);
            }

            var genericArgs = type.GetGenericArguments();
            var argNames = new string[genericArgs.Length];
            for (int i = 0; i < genericArgs.Length; i++)
            {
                // For open generics, this will be "T", "TKey", "TValue", etc.
                argNames[i] = genericArgs[i].GetNiceTypeName();
            }

            return $"{name}<{string.Join(", ", argNames)}>";
        }
    }
}
