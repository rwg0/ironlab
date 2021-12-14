using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PythonConsoleControl
{
    public static class PythonConfig
    {
        public static string[] SearchPaths = new string[0];
        public static Func<Stream> SyntaxHighlightingStreamSource { get; set; }
    }
}
