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
using MGenerator.Library;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.CSharp;
using System.IO;
using Microsoft.VisualBasic;
using MGenerator.Tools.OOPGeneration;
using MGenerator.Library.ApiGeneration;

namespace MGenerator.Tools
{
    public class Generator
    {
        /// <summary>
        /// Generates Data Access, Mapping and Transfer Code from the Database
        /// </summary>
        /// <param name="FolderPath"></param>
        /// <param name="ServerName"></param>
        /// <param name="DatabaseName"></param>
        public static void GenerateFromDatabase(GenerationInfo GenInfo)
        {
            DirectoryInfo d = new DirectoryInfo(GenInfo.FolderPath);

            Microsoft.SqlServer.Management.Common.ServerConnection svrCon = new Microsoft.SqlServer.Management.Common.ServerConnection(GenInfo.ServerName);
            Server svr = new Server(svrCon);
            
            String DbScriptsPath = GenInfo.FolderPath + @"\Procedures";

            foreach (Microsoft.SqlServer.Management.Smo.Database datab in svr.Databases)
            {
                if (datab.IsAccessible)
                {
                    if (!(DatabasesToIgnore().Contains(datab.Name)))
                    {
                        if (datab.Name.ToLower() == GenInfo.DataBase.ToLower())
                        {
                            Console.Write("Generating Code for Database: " + datab.Name + "\n");
                            String RepositoryPath = GenInfo.FolderPath + @"\" + datab.Name;  // The Current Repository 
                            String DataLayerNamespace = String.Format("{0}.{1}.{2}", GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name);

                            #region [ Clean / Create Directory ]
                            if (Directory.Exists(RepositoryPath))
                            {
                                // Directory Exists clean it out
                                DirectoryInfo dRepository = new DirectoryInfo(RepositoryPath);
                                foreach (FileInfo f in dRepository.GetFiles("*.*", SearchOption.AllDirectories))
                                {
                                    f.Delete();
                                }
                            }
                            else
                            {
                                Directory.CreateDirectory(RepositoryPath);
                            }
                            #endregion

                            #region [ Tables ]
                            String RepositoryPathProcedures = RepositoryPath + @"\Procedures";

                            if (Directory.Exists(RepositoryPathProcedures))
                            {
                                Directory.Delete(RepositoryPathProcedures);
                            }
                            Directory.CreateDirectory(RepositoryPathProcedures);
                            Int32 GenerationStep = 0;
                            DALGenerator dg = new DALGenerator();
                            POCOGenerator pg = new POCOGenerator();

                            foreach (Table t in datab.Tables)
                            {
                                GenerationStep = 0;
                                try
                                {
                                    if (t.Name == "Loan")
                                    {
                                        "$".IsNormalized();
                                    }
                                    if (!(t.IsSystemObject))
                                    {
                                        // Check if the table has a primary key, if not then dont generate anything. Table muist be normalized
                                        if (ValidateSqlTable(t))
                                        {
                                            #region [ Generation Root ]
                                            String cpFilename = RepositoryPathProcedures + @"\\" + "cp_" + t.Name + ".sql";
                                            String sspFileName = RepositoryPathProcedures + @"\\" + "ssp_" + t.Name + ".sql";
                                            String EntityFileName = RepositoryPath + @"\\" + t.Name + ".cs";
                                            String DALFileName = RepositoryPath + @"\\" + t.Name + "DAL.cs";
                                            String DOMFileName = RepositoryPath + @"\\" + t.Name + "DomainObject.cs";

                                            GenerationStep = 1;
                                            // [0]  Stored Procedures
                                            MGenerator.Tools.SqlGeneration.CrudProcedureGenerator gen = new SqlGeneration.CrudProcedureGenerator(datab, t);
                                            gen.Generate(RepositoryPathProcedures, MGenerator.Tools.SqlGeneration.ProcedureGenerationType.Alter);
                                            GenerationStep = 2;

                                            ObjectiveSearchProcedureGenerator ospgen = new ObjectiveSearchProcedureGenerator(datab, t);
                                            ospgen.Generate(RepositoryPathProcedures);
                                            GenerationStep = 3;


                                            // [1]  Data Access Classes
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, dg.BuildDalClass(String.Format("{0}.{1}", GenInfo.CompanyName, GenInfo.DataNameSpace), datab.Name, t));
                                            GenerationStep = 4;

                                            // [2] Entity / Structure Class
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, pg.BuildPoc(t));
                                            GenerationStep = 5;

                                            // [3]  Service Layer 
                                            DomainServiceGraph svcgen = new DomainServiceGraph();
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, svcgen.BuildDomainObject(String.Format("{0}.{1}", GenInfo.CompanyName, GenInfo.DataNameSpace), datab.Name, t));
                                            GenerationStep = 6;

                                            // [4] API 
                                            ApiGraph api = new ApiGraph();
                                            string api_name = 
                                            api.WriteApiStation(GenInfo, datab, t,RepositoryPath);
                                            #endregion

                                            Console.Write(t.Name + "....COMPLETE" + "\n");
                                        }
                                    }
                                }
                                catch (Exception x)
                                {
                                    Console.Write(t.Name + "....ERROR" + "\n");
                                    // File.WriteAllText(RepositoryPath + t.Name + ".txt", x.Message + "\n Generation Step:" + GenerationStep.ToString() );
                                    continue;
                                }
                            }
                            #endregion

                            #region [ Views ]
                            foreach (View view in datab.Views)
                            {
                                try
                                {
                                    if (view.Name == "vwLOLicense")
                                    {
                                        String fd = "";
                                    }
                                    if (!(view.IsSystemObject))
                                    {
                                        if (ValidateSqlView(view))
                                        {
                                            ObjectiveSearchProcedureGenerator ospgen = new ObjectiveSearchProcedureGenerator(datab, view);
                                            DomainServiceGraph svcgen = new DomainServiceGraph();
                                            ospgen.Generate(RepositoryPathProcedures);
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, dg.BuildDalClass(String.Format("{0}.{1}", GenInfo.CompanyName, GenInfo.DataNameSpace), datab.Name, view));
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, pg.BuildPoco(view));
                                            WriteCsharpFile(GenInfo.CompanyName, GenInfo.DataNameSpace, datab.Name, RepositoryPath, svcgen.BuildDomainObject(String.Format("{0}.{1}", GenInfo.CompanyName, GenInfo.DataNameSpace), datab.Name, view));
                                            Console.Write(view.Name + "....COMPLETE" + "\n");
                                        }
                                    }
                                }
                                catch (Exception x)
                                {
                                    Console.Write(view.Name + "....ERROR" + "\n");
                                    continue;
                                }
                            }
                            #endregion
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="NameSpace"></param>
        /// <param name="FolderPath"></param>
        /// <param name="ctd"></param>
        public static void WriteCsharpFile(String CompanyName, String DataAccessNamespaceSegment, String DatabaseName , String FolderPath, CodeTypeDeclaration ctd)
        {
            // [0] Build the Namespaces
            String CompanyNamespace = CompanyName;
            String DataAccessNamespace = String.Format("{0}.{1}", CompanyName, DataAccessNamespaceSegment);
            String DataLayerNamespace = String.Format("{0}.{1}.{2}", CompanyName, DataAccessNamespaceSegment, DatabaseName);


            CodeCompileUnit targetUnit = new CodeCompileUnit();
            CodeNamespace gNamespace = new CodeNamespace(DataLayerNamespace);
            gNamespace.Imports.Add(new CodeNamespaceImport("System"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.IO"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Linq"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.ObjectModel"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Specialized"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Data"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Xml"));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Xml.Linq"));
            gNamespace.Imports.Add(new CodeNamespaceImport(CompanyNamespace));
            gNamespace.Imports.Add(new CodeNamespaceImport(DataAccessNamespace));
            gNamespace.Imports.Add(new CodeNamespaceImport("System.Runtime.Serialization"));
            gNamespace.Types.Add(ctd);

            String NewCodeFileName = FolderPath + @"\" + ctd.Name + ".generated.cs";
            Stream s = File.Open(NewCodeFileName, FileMode.Create);

            StreamWriter sw = new StreamWriter(s);

            CSharpCodeProvider cscProvider = new CSharpCodeProvider();
            //VBCodeProvider cscProvider = new VBCodeProvider();
            ICodeGenerator cscg = cscProvider.CreateGenerator(sw);
            

            CodeGeneratorOptions cop = new CodeGeneratorOptions();
            // Create the Generated File 
            cscg.GenerateCodeFromNamespace(gNamespace, sw, cop);
            sw.Close();
            s.Close();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FolderPath"></param>
        public static void CleanGeneratedOutput(String FolderPath)
        {
            String DbScriptsPath = FolderPath + @"\Procedures";
            String BadDbScriptsPath = FolderPath + @"\Bad";
            DirectoryInfo d = new DirectoryInfo(FolderPath);
            DirectoryInfo dSql = new DirectoryInfo(DbScriptsPath);


            if (!Directory.Exists(BadDbScriptsPath))
            {
                Directory.CreateDirectory(BadDbScriptsPath);
            }
            else
            {
                DirectoryInfo dbad = new DirectoryInfo(BadDbScriptsPath);
                foreach (FileInfo badf in dbad.GetFiles())
                {
                    badf.Delete();
                }

                foreach (FileInfo f in dSql.GetFiles())
                {

                    if (f.Extension.Contains("txt"))
                    {
                        // we have a bad file
                        f.MoveTo(BadDbScriptsPath + @"\" + f.Name);
                        // find the bad cSharpFile and Move it
                        String BadCSharpFile = f.Name.Replace("cp_", "").Replace(".txt", ".cs");
                        File.Move(FolderPath + @"\" + BadCSharpFile, BadDbScriptsPath + @"\" + BadCSharpFile);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// <remarks></remarks>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<String> DatabasesToIgnore()
        {
            Collection<String> dbNames = new Collection<String>();

            dbNames.Add("ReportServer");
            dbNames.Add("ReportServerTempDB");
            dbNames.Add("master");
            dbNames.Add("msdb");
            dbNames.Add("model");
            dbNames.Add("tempdb");

            return dbNames;
        }
        /// <summary>
        /// 
        /// <remarks></remarks>
        /// </summary>
        /// <param name="baseobj"></param>
        /// <returns></returns>
        public static Boolean ValidateSqlObject(TableViewTableTypeBase baseobj)
        {
            Boolean bResult = true;

            #region [ Normalization 1NF ]

            foreach (Column col in baseobj.Columns)
            {
                if (col.Identity == true)
                {
                    bResult = true;
                }

                if (col.IdentityIncrement > 0)
                {
                    bResult = true;
                }

                if (col.InPrimaryKey == true)
                {
                    bResult = true;
                }
            }
            #endregion

            #region [ Table Names that can not be .Net Types ]
            if (baseobj.Name.Contains("aspnet"))
            {
                bResult = false;
            }

            foreach (Char c in baseobj.Name)
            {
                if (!(char.IsLetter(c)))
                {
                    bResult = false;
                }
            }
            #endregion

            return bResult;
        }
        /// <summary>
        /// 
        /// <remarks></remarks>
        /// </summary>
        /// <param name="baseobj"></param>
        /// <returns></returns>
        public static Boolean ValidateSqlView(View baseobj)
        {
            Boolean bResult = true;

            #region [ Table Names that can not be .Net Types ]
            if (baseobj.Name.Contains("aspnet"))
            {
                bResult = false;
            }

            foreach (Char c in baseobj.Name.Replace("_",""))
            {
                if (!(char.IsLetter(c)))
                {
                    bResult = false;
                }
            }
            #endregion

            return bResult;
        }
        /// <summary>
        /// 
        /// <remarks></remarks>
        /// </summary>
        /// <param name="baseobj"></param>
        /// <returns></returns>
        public static Boolean ValidateSqlTable(Table baseobj)
        {
            Boolean bResult = false;

            #region [ Normalization 1NF ]

            foreach (Column col in baseobj.Columns)
            {
                if (col.InPrimaryKey == true)
                {
                    bResult = true;
                }
            }
            #endregion

            #region [ Table Names that can not be .Net Types ]
            if (baseobj.Name.Contains("aspnet"))
            {
                if (baseobj.Name == "aspnet_Client_Tracking")
                {
                    bResult = true;
                }
                else
                {
                    bResult = false;
                }
            }

            foreach (Char c in baseobj.Name)
            {
                if (!(char.IsLetter(c)))
                {
                    if (!(c == char.Parse("_")))
                    {
                        bResult = false;
                    }
                }
            }
            #endregion

            return bResult;
        }
    }

    public enum ClassType
    {
        DAL,
        Entity,
        Service
    }
}
