using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.Shared
{
    public sealed record Error(string Code, string Message, Dictionary<string, string[]>? ValidationErrors = null)
    {
        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");
    }
}
