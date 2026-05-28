
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.Interfaces.Blogs
{
    public interface ISlugService
    {
        string GenerateSlug(string title);
    }
}
