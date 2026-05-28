using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vivu.Domain.AI
{
    public class AIProvider
    {
        public const string Claude = "Claude";
        public const string OpenAI = "OpenAI";
        public const string Gemini = "Gemini";
        public const string Groq = "Groq";

        public static readonly string[] SupportedProviders = { Claude, OpenAI, Gemini, Groq };
    }
}
