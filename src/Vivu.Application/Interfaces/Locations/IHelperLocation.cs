using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.Interfaces.Locations
{
    public interface IHelperLocation
    {
        string? NullIfEmpty(string? value);
        string? ConvertImagesToJson(string? images);
    }
}
