using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace MGenerator.AppModel
{
    public class AppClassFile
    {
        public const String FileNameTemp = "{0}.{1}.{2}";
        public String FileName { get; set; }
        public String GeneratedTag { get; set; }
        public String Extension { get; set; }


    }
}
