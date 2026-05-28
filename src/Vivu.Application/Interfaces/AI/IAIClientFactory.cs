using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Application.Interfaces.AI
{
    public interface IAIClientFactory
    {
        IAIClient GetClient(string? providerName = null);
    }
}
