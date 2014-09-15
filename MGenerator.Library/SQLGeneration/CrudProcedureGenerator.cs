using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.SqlServer.Management.Smo;
namespace MGenerator.Tools.SqlGeneration
{
    public class CrudProcedureGenerator
    {
        #region [ Fields ]

        #region [ Constants ]

        // Clues that help look up the embedded template files in the 
        // assembly's resources.
        public const string TEXTBODY = "TextBody.txt";
        public const string TEXTHEADER = "TextHeader.txt";

        #endregion

        #endregion

        #region [ Constructors ]

        public CrudProcedureGenerator(Microsoft.SqlServer.Management.Smo.Database database, Table table)
        {
            _database = database;
            if (database != null && database.Tables.Contains(table))
            {
                _table = table;
            }
        }

        #endregion

        #region [ Methods ]

        private static string createColumnParameterAssignments(
            IEnumerable<KeyValuePair<Column, StoredProcedureParameter>> columnsToParameters,
            string firstDelimiter,
            string delimiter,
            int indent)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string indentString = string.Empty;
            for (int i = 0; i < indent; i++)
            {
                indentString = string.Format("{0}{1}", indentString, "\t");
            }

            int columnNameSize = 0;
            if (columnsToParameters.Count() > 1)
            {
                foreach (KeyValuePair<Column, StoredProcedureParameter> kv in columnsToParameters)
                {
                    if (kv.Key.Name.Length > columnNameSize)
                    {
                        columnNameSize = kv.Key.Name.Length;
                    }
                }
            }
            columnNameSize += 3;

            IEnumerator<KeyValuePair<Column, StoredProcedureParameter>> enumerator = columnsToParameters.GetEnumerator();
            if (enumerator.MoveNext())
            {
                KeyValuePair<Column, StoredProcedureParameter> current = enumerator.Current;
                string currentName = string.Format("[{0}]", current.Key.Name);
                stringBuilder = stringBuilder.AppendFormat("{0}{1}{2}= {3}{4}",
                    firstDelimiter,
                    currentName.PadRight(columnNameSize),
                    columnsToParameters.Count() > 1 ? "\t" : " ",
                    current.Value.Name,
                    Environment.NewLine);
            }

            while (enumerator.MoveNext())
            {
                KeyValuePair<Column, StoredProcedureParameter> current = enumerator.Current;
                string currentName = string.Format("[{0}]", current.Key.Name);
                stringBuilder = stringBuilder.AppendFormat("{0}{1}{2}{3}= {4}{5}",
                    indentString,
                    delimiter,
                    currentName.PadRight(columnNameSize),
                    columnsToParameters.Count() > 1 ? "\t" : " ",
                    current.Value.Name,
                    Environment.NewLine);
            }

            string str = stringBuilder.ToString();
            if (str.LastIndexOf(Environment.NewLine) > 0)
            {
                str = str.Remove(str.LastIndexOf(Environment.NewLine));
            }

            return str;
        }

        private static string createStringList(IEnumerable<string> strings, int indent)
        {
            StringBuilder stringBuilder = new StringBuilder();
            string indentString = string.Empty;
            for (int i = 0; i < indent; i++)
            {
                indentString = string.Format("{0}{1}", indentString, "\t");
            }
            IEnumerator<string> enumerator = strings.GetEnumerator();
            if (enumerator.MoveNext())
            {
                stringBuilder = stringBuilder.AppendFormat(" {0}{1}", enumerator.Current, Environment.NewLine);
            }
            while (enumerator.MoveNext())
            {
                stringBuilder = stringBuilder.AppendFormat("{0},{1}{2}", indentString, enumerator.Current, Environment.NewLine);
            }
            string str = stringBuilder.ToString();
            if (str.LastIndexOf(Environment.NewLine) >= 0)
            {
                str = str.Remove(str.LastIndexOf(Environment.NewLine));
            }
            return str;
        }

        public void Generate(string path, ProcedureGenerationType type)
        {
            generate(path, type, false);
        }

        public void generate(string path, ProcedureGenerationType type, bool justGenerateScriptFiles)
        {
            #region [ Validate object state for generation. ]

            if (_database == null || _table == null)
            {
                return;
            }

            #endregion

            #region [ Set the procedure's name. ]

            string procedureName = string.Format("cp_{0}", _table.Name);

            #endregion

            #region [ Create the procedure object. ]

            StoredProcedure procedure = new StoredProcedure(_database, procedureName);
            procedure.Schema = _table.Schema;

            #endregion

            #region [ Set the procedure's parameters. ]

            procedure.TextMode = false;

            StoredProcedureParameter operationParameter = new StoredProcedureParameter(procedure, "@Operation");
            operationParameter.DataType = DataType.Int;
            operationParameter.DefaultValue = "0";
            procedure.Parameters.Add(operationParameter);

            StoredProcedureParameter userParameter = new StoredProcedureParameter(procedure, "@User");
            userParameter.DataType = DataType.NVarChar(128);
            userParameter.DefaultValue = "NULL";
            procedure.Parameters.Add(userParameter);

            IDictionary<Column, StoredProcedureParameter> columnsToParameters = new SortedDictionary<Column, StoredProcedureParameter>(new ColumnComparer());
            foreach (Column column in _table.Columns)
            {
                StoredProcedureParameter parameter = new StoredProcedureParameter(procedure, string.Format("@{0}", column.Name));
                parameter.DataType = column.DataType;
                parameter.DefaultValue = string.IsNullOrEmpty(column.Default) ? "NULL" : column.Default;
                columnsToParameters.Add(column, parameter);
                procedure.Parameters.Add(parameter);
            }

            #endregion

            #region [ Set the procedure's header. ]

            string textHeader = procedure.ScriptHeader(true);

            #region [ Sort and format the parameters for readability ]

            // Split the procedure's parameters by line
            string[] parameters = textHeader.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            // Create a line to hold each parameter's: name, data type, 
            // and default value.
            string[][] parameterParts = new string[parameters.Length - 2][];
            for (int l = 0; l < parameterParts.Length; l++)
            {
                parameterParts[l] = new string[3];
            }

            // Create a regular expression to split the parameter into 
            // its parts.
            Regex regex = new Regex("(\\s)*(@[^\\s]+)\\s+(\\[.*\\](\\s*\\([^\\(]*\\)){0,1})\\s*([^,]*)(,)*(.*)");

            // Determine the maximum length of parameter name and data 
            // type for formatting.
            int parameterNameSize = 0;
            int parameterTypeSize = 0;
            for (int i = 1; i < parameters.Length - 1; i++)
            {
                string parameter = parameters[i];
                Match match = regex.Match(parameter);
                if (match.Groups[2].Value.Length > parameterNameSize)
                {
                    parameterNameSize = match.Groups[2].Value.Length;
                }
                if (match.Groups[3].Value.Length > parameterTypeSize)
                {
                    parameterTypeSize = match.Groups[3].Value.Length;
                }
            }

            // Sort and format (align) the parameters.
            SortedDictionary<string, string[]> sortedParameters = new SortedDictionary<string, string[]>();
            for (int j = 1; j < parameters.Length - 1; j++)
            {
                string parameter = parameters[j];
                Match match = regex.Match(parameter);
                parameterParts[j - 1][0] =
                    match.Groups[1].Value +
                    (j == 1 ? " " : ",") +
                    match.Groups[2].Value
                    .PadRight(parameterNameSize + 1);
                parameterParts[j - 1][1] = match.Groups[3].Value
                    .Replace("(", "( ")
                    .Replace(")", " )")
                    .PadRight(parameterTypeSize + 3)
                    .ToUpper();
                parameterParts[j - 1][2] = match.Groups[5].Value;

                sortedParameters.Add(parameterParts[j - 1][0], parameterParts[j - 1]);
            }

            // Rebuild the text of the procedure's header with the 
            // processed parameters.
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(parameters[0]);
            foreach (string[] parameterPart in sortedParameters.Values)
            {
                stringBuilder.AppendFormat("{0}\t{1}\t{2}{3}",
                    parameterPart[0],
                    parameterPart[1],
                    parameterPart[2],
                    Environment.NewLine);
            }
            stringBuilder.AppendLine(parameters[parameters.Length - 1]);

            textHeader = stringBuilder.ToString();

            #endregion

            if (type == ProcedureGenerationType.Create)
            {
                textHeader = textHeader.Replace("ALTER PROCEDURE", "CREATE PROCEDURE");
            }

            textHeader = string.Format("{0}\r\n{1}", readResource(TEXTHEADER), textHeader);
            textHeader = string.Format(textHeader,
                procedure.Name,
                GetType().FullName,
                DateTime.Now,
                string.Format("Provides CRUD access to [{0}].[{1}].[{2}]", _database.Name, _table.Schema, _table.Name));

            procedure.TextMode = true;
            procedure.TextHeader = textHeader;

            #endregion

            #region [ Set the procedure's body. ]

            // Remove the header comment from the template that explains 
            // the meaning of the replacement tokens.
            string textBody = readResource(TEXTBODY);
            string textBodyCommentEnd = "}}}}}\r\n";
            textBody = textBody.Remove(0, textBody.LastIndexOf(textBodyCommentEnd) + textBodyCommentEnd.Length);

            // Create SELECT Column list.
            string selectColumns = createStringList(
                from Column c
                in columnsToParameters.Keys
                select string.Format("[{0}]", c.Name), 5);

            string selectPrimaryKeyColumns = createStringList(
                from c
                in columnsToParameters.Keys
                where c.InPrimaryKey
                select string.Format("[{0}]", c.Name), 5);

            // Create PRIMARY KEY constraint.
            string primaryKeyConstraint = string.Format(
                "WHERE\t{0}",
                createColumnParameterAssignments(
                    from kv
                    in columnsToParameters
                    where kv.Key.InPrimaryKey
                    select kv,
                    string.Empty,
                    "AND\t\t",
                    3));
            if (primaryKeyConstraint.Length == 6)
            {
                StringCollection lines = new StringCollection();
                lines.Add(string.Format("Error creating CrudProcedure for [{0}].[{1}].[{2}]: No primary key.",
                    _database.Name,
                    _table.Schema,
                    _table.Name));
                writeScriptFile(path, string.Format("{0}.txt", procedure.Name), lines);
                return;
            }

            // Create UPDATE Assignments.
            string updateAssignments = createColumnParameterAssignments(
                from kv
                in columnsToParameters
                where !(kv.Key.InPrimaryKey || kv.Key.Identity || kv.Key.Computed)
                select kv,
                " ",
                ",",
                5);

            // Create INSERT Column list.
            string insertColumns = createStringList(
                from Column c
                in columnsToParameters.Keys
                where !(c.Identity || c.Computed)
                select string.Format("[{0}]", c.Name), 6);

            // Create INSERT Parameter list.
            string insertParameters = createStringList(
                from KeyValuePair<Column, StoredProcedureParameter> kv
                in columnsToParameters
                where !(kv.Key.Identity || kv.Key.Computed)
                select kv.Value.Name, 6);

            // Replace tokens in text body.
            textBody = string.Format(
                textBody,
                string.Format("[{0}].[{1}].[{2}]", _database.Name, _table.Schema, _table.Name),
                selectColumns,
                primaryKeyConstraint,
                updateAssignments,
                insertColumns,
                insertParameters,
                selectPrimaryKeyColumns);

            procedure.TextBody = textBody;

            #endregion

            #region [ Write procedure script to file. ]

            writeScriptFile(path, string.Format("{0}.sql", procedure.Name), procedure.Script());

            #endregion

        }

        public static void Generate(string serverName, string databaseName, string tableName, string path, ProcedureGenerationType type)
        {
            generate(serverName, databaseName, tableName, path, type, false);
        }

        public static void Generate(string serverName, string databaseName, string path, ProcedureGenerationType type)
        {
            generate(serverName, databaseName, path, ProcedureGenerationType.Create, false);
        }

        private static void generate(string serverName, string databaseName, string path, ProcedureGenerationType type, bool justGenerateScriptFiles)
        {
            Server server = new Server(serverName);
            Microsoft.SqlServer.Management.Smo.Database database = server.Databases[databaseName];
            if (!database.IsSystemObject)
            {
                foreach (Table table in database.Tables)
                {
                    CrudProcedureGenerator procedureGenerator = new CrudProcedureGenerator(database, table);
                    procedureGenerator.generate(path, ProcedureGenerationType.Create, justGenerateScriptFiles);
                }
            }
        }

        private static void generate(string serverName, string databaseName, string tableName, string path, ProcedureGenerationType type, bool justGenerateScriptFiles)
        {
            Server server = new Server(serverName);
            Microsoft.SqlServer.Management.Smo.Database database = server.Databases[databaseName];
            if (!database.IsSystemObject)
            {
                CrudProcedureGenerator procedureGenerator = new CrudProcedureGenerator(database, database.Tables[tableName]);
                procedureGenerator.generate(path, ProcedureGenerationType.Create, justGenerateScriptFiles);
            }
        }

        public void GenerateScriptFiles(string path)
        {
            generate(path, ProcedureGenerationType.Create, true);
        }

        public void GenerateScriptFiles(string path, ProcedureGenerationType type)
        {
            generate(path, type, true);
        }

        public static void GenerateScriptFiles(string serverName, string databaseName, string path)
        {
            generate(serverName, databaseName, path, ProcedureGenerationType.Create, true);
        }

        public static void GenerateScriptFiles(string serverName, string databaseName, string tableName, string path)
        {
            generate(serverName, databaseName, tableName, path, ProcedureGenerationType.Create, true);
        }

        public static void GenerateScriptFiles(string serverName, string databaseName, string path, ProcedureGenerationType type)
        {
            generate(serverName, databaseName, path, type, true);
        }

        private string readResource(string name)
        {
            string resource = string.Empty;
   
            try
            {
                resource = File.ReadAllText(name);
            }
            catch (ArgumentNullException)
            {
            }
            return resource;
        }

        private void writeScriptFile(string path, string fileName, StringCollection lines)
        {
            #region [ Get directory information for the path. ]

            DirectoryInfo directory = default(DirectoryInfo);
            if (!string.IsNullOrEmpty(path))
            {
                directory = new DirectoryInfo(path);
            }

            #endregion

            if (directory != default(DirectoryInfo))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                FileInfo file = new FileInfo(string.Format("{0}\\{1}", directory.FullName, fileName));
                using (StreamWriter writer = new StreamWriter(file.Create()))
                {
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        #endregion

        #region [ Properties ]

        private Microsoft.SqlServer.Management.Smo.Database _database
        {
            get;
            set;
        }

        private Table _table
        {
            get;
            set;
        }

        #endregion
    }
}
