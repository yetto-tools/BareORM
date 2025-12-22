using System;
using System.Collections.Generic;
using System.Text;

namespace BareORM.Mapping
{
    public static class NamePolicies
    {
        public static string Default(string name)
            => Normalize(name);

        public static string Normalize(string name)
        {
            // “User_Id” -> “USERID”, “UserId” -> “USERID”
            // (Si querés algo más fino después, lo cambiamos)
            Span<char> buffer = stackalloc char[name.Length];
            int j = 0;

            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '_' || c == ' ') continue;
                buffer[j++] = char.ToUpperInvariant(c);
            }

            return new string(buffer[..j]);
        }
    }
}
