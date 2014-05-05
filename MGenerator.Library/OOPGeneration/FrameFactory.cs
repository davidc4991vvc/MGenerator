using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.CSharp;
using System.IO;

namespace MGenerator.Tools
{
    public class FrameFactory
    {
        public static CodeTypeDeclaration GetPartialClassFrame(String Name)
        {
            CodeTypeDeclaration ctd = new CodeTypeDeclaration();

            ctd.Name = Name;
            ctd.IsPartial = true;
            ctd.Attributes = MemberAttributes.Public;
            ctd.IsClass = true;

            return ctd;
        }
    }
}
