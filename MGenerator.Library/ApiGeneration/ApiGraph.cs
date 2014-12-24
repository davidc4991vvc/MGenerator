using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace MGenerator.Library.ApiGeneration
{
    public class ApiGraph
    {
        // Replacement Tags 
        const String NAMESPACE = "##NAMESPACE##";
        const String DOM_CLASS = "##DOM_CLASS##";
        const String TABLE_NAME = "##TABLE_NAME##";

        internal string WriteApiStation(GenerationInfo GenInfo, Microsoft.SqlServer.Management.Smo.Database datab, Microsoft.SqlServer.Management.Smo.Table t, string RepositoryPath)
        {
            string module_folder_name = String.Format(@"{0}\Modules",RepositoryPath);
            string file_path = string.Format(@"{0}\{1}Module.cs", module_folder_name, t.Name);

            #region [ Build Module Folder Name ]
            if (Directory.Exists(module_folder_name) == false)
            {
                Directory.CreateDirectory(module_folder_name);
            }
            #endregion




            return "";
        }
    }
}
