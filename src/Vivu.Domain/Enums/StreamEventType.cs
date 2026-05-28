using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.Enums
{
    public enum StreamEventType
    {
        Start,
        LocationMap,
        Parsing,
        Chunk,
        Complete,
        Saving,
        Saved,
        Error
    }

}
