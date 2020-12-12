using System.Collections.Generic;
using System.Linq;

namespace Core.Meta.Attributes
{
    public class AuthorizeWhenHasAnyOfAttribute : BaseAttribute
    {
        public IReadOnlyList<string> Permissions { get; }

        public AuthorizeWhenHasAnyOfAttribute(string[] permissions)
        {
            Permissions = permissions;
            Value = string.Join(", ", permissions.Select(p => $"\"{p}\""));
        }

        public override bool TryValidate(out string reason)
        {
            reason = string.Empty;
            return true;
        }
    }
}