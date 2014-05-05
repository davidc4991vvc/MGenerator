using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace MGenerator.Tools
{
    public class NameSpaceGraph
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Company">ex BrightGreen</param>
        /// <param name="Framework">ex. Data, Rules, ETC</param>
        /// <param name="DomainName">ex. Database Name or Service Name</param>
        /// <returns></returns>
        public static CodeNamespace NamespaceBuilder(String Company, String Framework, String DomainName)
        {
            CodeNamespace GraphNameSpace = new CodeNamespace();

            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System"));
            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System.Data"));
            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System.Collections.ObjectModel"));
            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System.IO"));
            GraphNameSpace.Imports.Add(new CodeNamespaceImport("System.Text"));

            // [0] Asepects



            return GraphNameSpace;
        }
    }
}
