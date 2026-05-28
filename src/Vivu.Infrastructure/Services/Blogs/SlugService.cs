using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vivu.Application.Interfaces.Blogs;

namespace Vivu.Infrastructure.Services.Blogs
{
    public class SlugService : ISlugService
    {
        public string GenerateSlug(string title)
        {
            var slug = title.ToLowerInvariant();
            slug = slug.Normalize(System.Text.NormalizationForm.FormD);
            slug = new string(slug.Where(c =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());

            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
            slug = slug.Trim('-');
            return slug;
        }
    }
}
