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
using Microsoft.SqlServer.Management.Smo;
using Microsoft.CSharp;

namespace MGenerator.Tools.OOPGeneration
{
    public class DALGenerator
    {
        #region [ Factory Methods ] 
        // DalCLassInfo
        // 
        // 
        public CodeTypeDeclaration BuildDalClass(String DataAccessLayerNameSpace, String DatabaseName,TableViewTableTypeBase table)
        {
            CodeTypeDeclaration ctd = new CodeTypeDeclaration();
            String cp_name = "cp_" + table.Name;
            String ssp_name = "ssp_" + table.Name;
            String BaseClassTypeName = String.Format("{0}.DataAccessBase",DataAccessLayerNameSpace);
            ConstructorGraph ctorGraph = new ConstructorGraph();
            ctd.Name = table.Name + "DAL";
            ctd.BaseTypes.Add(new CodeTypeReference(BaseClassTypeName));
            ctd.TypeAttributes = System.Reflection.TypeAttributes.Public;
            ctd.Attributes = MemberAttributes.Public;
            ctd.IsPartial = true;
            ctd.Members.Add(ctorGraph.GraphDalConstructor(DatabaseName));
            ctd.Members.Add(BuildFillMethod(table)); 

            foreach (CodeMemberMethod cmm in BuildSelectMethods(table))
            {
                ctd.Members.Add(cmm);
            }

            if (table.GetType() == typeof(Table))
            {
                foreach(CodeMemberMethod cmm2 in BuildDMLMethods(table))
                {
                    ctd.Members.Add(cmm2);
                }
            }
        
            return ctd;
        }
        #endregion 

        #region [ Method Collection Builders [
        /// <summary>
        /// Get Every Select Method into a Collection
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private Collection<CodeMemberMethod> BuildSelectMethods(TableViewTableTypeBase table)
        {
            Collection<CodeMemberMethod> SelectMethods = new Collection<CodeMemberMethod>();
            SelectMethods.Add(BuildSelectObjects(table));
            SelectMethods.Add(BuildSelect(table));
            SelectMethods.Add(BuildSelectAll(table));
            SelectMethods.Add(BuildSelectBE(table));
            return SelectMethods;
        }
        /// <summary>
        /// Adds all Data Modification Language Methods 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private Collection<CodeMemberMethod> BuildDMLMethods(TableViewTableTypeBase table)
        {
            Collection<CodeMemberMethod> DMLMethods = new Collection<CodeMemberMethod>();
            DMLMethods.Add(BuildUpdate(table));
            DMLMethods.Add(BuildPocoUpdate(table));
            DMLMethods.Add(BuildDelete(table));
            DMLMethods.Add(BuildPocoDelete(table));
            DMLMethods.Add(BuildInsert(table));
            DMLMethods.Add(BuildPocoInsert(table));
            return DMLMethods; 
        }
        #endregion 

        #region [ DAL Variables ]
        private CodeTypeMember GetDataAccessMember(String DataAccessLayerName)
        {
            CodeMemberField cmfAccess = new CodeMemberField();
            cmfAccess.Name = "Access";
            cmfAccess.Attributes = MemberAttributes.Private;
            cmfAccess.Type = new CodeTypeReference(DataAccessLayerName + ".DataAccess");
            return cmfAccess;
        }
        private CodeParameterDeclarationExpression PocoQueryParameter(String TypeName)
        {
            CodeParameterDeclarationExpression cpPocoPar = new CodeParameterDeclarationExpression();
            cpPocoPar.Name = "query";
            cpPocoPar.Type = new CodeTypeReference(TypeName);

            return cpPocoPar;
        }
        #endregion 

        #region [ DAL Methods ] 
        private CodeMemberMethod BuildFillMethod(TableViewTableTypeBase table)
        {
            // Accepts a DataRow and Returns a Poco of this type. 
            CodeMemberMethod cmmFill = new CodeMemberMethod();
            cmmFill.Name = "Fill";
            cmmFill.Attributes = MemberAttributes.Private;
            cmmFill.ReturnType = new CodeTypeReference(table.Name);
            CodeParameterDeclarationExpression cpdeDataRow = new CodeParameterDeclarationExpression();
            cpdeDataRow.Name = "row";
            cpdeDataRow.Type = new CodeTypeReference("System.Data.DataRow");
            cpdeDataRow.Direction = FieldDirection.In;

            cmmFill.Parameters.Add(cpdeDataRow);
            var init_Express = new CodeSnippetExpression("new " + table.Name + "()");

            var obj = new CodeVariableDeclarationStatement(new CodeTypeReference(table.Name), "obj", init_Express);
            cmmFill.Statements.Add(obj);

            foreach (Column c in table.Columns)
            {
                String DotNetTypeName = TypeConvertor.ToNetType(c.DataType.SqlDataType).ToString();
                MemberGraph mGraph = new MemberGraph(c);
                System.CodeDom.CodeConditionStatement ccsField = new CodeConditionStatement();
                ccsField.Condition = new CodeSnippetExpression("(row[\"" + c.Name + "\"] != System.DBNull.Value)");
                //if (!(mGraph.IsReadOnly))
                //{
                    // If Field is nullable Type 
                    if (mGraph.IsNullable)
                    {
                        if (mGraph.TypeName() == "String")
                        {
                            ccsField.TrueStatements.Add(new CodeSnippetExpression("obj." + mGraph.PropertyName() + " = row[\"" + c.Name + "\"].ToString()"));
                        }
                        else
                        {
                            ccsField.TrueStatements.Add(new CodeSnippetExpression("obj." + mGraph.PropertyName() + " = ((" + mGraph.TypeName() + ")(row[\"" + c.Name + "\"]))"));
                        }
                    }
                    else
                    {
                        if (mGraph.TypeName() == "String")
                        {
                             ccsField.TrueStatements.Add(new CodeSnippetExpression("obj." + mGraph.PropertyName() + " = row[\"" + c.Name + "\"].ToString()"));
                        }
                        else
                        {
                            ccsField.TrueStatements.Add(new CodeSnippetExpression("obj." + mGraph.PropertyName() + " = ((" + mGraph.TypeName() + ")(row[\"" + c.Name + "\"]))"));
                        }
                    }
                    cmmFill.Statements.Add(ccsField);
                //}
            }

            cmmFill.Statements.Add(new CodeSnippetExpression("return obj"));
            cmmFill.Comments.Add(new CodeCommentStatement("Returns a Hydrated POCO"));
            return cmmFill;
        }
        #endregion 

        #region [ Query Methods ]
        #region [ Search Methods ]
        /// <summary>
        /// Select One Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildSelect(TableViewTableTypeBase table)
        {
            
            CodeMemberMethod cmSelect = new CodeMemberMethod();
            String cp_name = "cp_" + table.Name;
            cmSelect.Attributes = MemberAttributes.Public;
            cmSelect.Name = "Select";
            cmSelect.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName",cp_name)));
            cmSelect.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 1, ParameterDirection.Input)"));
            cmSelect.Comments.Add(new CodeCommentStatement("Selects One By Primary Key, returns a Data Set"));

            foreach (Column c in table.Columns)
            {
                if (c.InPrimaryKey)
                {
                    MemberGraph mGraph = new MemberGraph(c);
                    cmSelect.Parameters.Add(mGraph.GetParameter());
                    cmSelect.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.Name + "\", " + mGraph.ParameterName() + ", ParameterDirection.Input)"));
                }
            }

            
            cmSelect.Statements.Add(new CodeSnippetExpression("var value = this.Access.ExecuteDataSet()"));
            cmSelect.Statements.Add(new CodeSnippetExpression("return value"));
            cmSelect.ReturnType = new CodeTypeReference("System.Data.DataSet");

            return cmSelect;
        }
        /// <summary>
        /// Select All Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildSelectAll(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmSelectAll = new CodeMemberMethod();
            String cp_name = "cp_" + table.Name;
            cmSelectAll.Attributes = MemberAttributes.Public;
            cmSelectAll.Name = "SelectAll";
            cmSelectAll.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmSelectAll.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 2, ParameterDirection.Input)"));
            cmSelectAll.Statements.Add(new CodeSnippetExpression("DataSet value = this.Access.ExecuteDataSet()"));
            cmSelectAll.Statements.Add(new CodeSnippetExpression("return value"));
            cmSelectAll.ReturnType = new CodeTypeReference("System.Data.DataSet");
            cmSelectAll.Comments.Add(new CodeCommentStatement("Select All [Use Caution]"));
            return cmSelectAll;
        }
        /// <summary>
        /// Select ID Enumeration
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildSelectIDs(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmSelectIDs = new CodeMemberMethod();
            String cp_name = "cp_" + table.Name;

            

            return cmSelectIDs;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public CodeMemberMethod BuildSelectBE(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmSelect = new CodeMemberMethod();
            cmSelect.Attributes = MemberAttributes.Public;
            cmSelect.ReturnType = new CodeTypeReference("System.Data.DataSet");
            String cp_name = "ssp_" + table.Name;
            String PocoTypeName = table.Name;
            String FullPocoTypeName = PocoTypeName;

            CodeParameterDeclarationExpression cpdePoco = new CodeParameterDeclarationExpression();
            cpdePoco.Name = "query";
            cpdePoco.Type = new CodeTypeReference(table.Name);
            cpdePoco.Direction = FieldDirection.In;
            cmSelect.Parameters.Add(cpdePoco);
            cmSelect.Attributes = MemberAttributes.Public;
            cmSelect.Name = "Select";
            cmSelect.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"" + cp_name + "\")"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                String DotNetTypeName = TypeConvertor.ToNetType(c.DataType.SqlDataType).ToString();

                System.CodeDom.CodeConditionStatement ccsField = new CodeConditionStatement();
                if (mGraph.IsNullable)
                {
                    ccsField.Condition = new CodeSnippetExpression("query." + mGraph.PropertyName() + ".HasValue");
                    ccsField.TrueStatements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\",query." + mGraph.PropertyName() + ".Value, ParameterDirection.Input)"));
                    ccsField.FalseStatements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", null , ParameterDirection.Input)"));
                }
                else
                {
                    ccsField.Condition = new CodeSnippetExpression("query." + mGraph.PropertyName() + " == null");
                    ccsField.TrueStatements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", null , ParameterDirection.Input)"));
                    ccsField.FalseStatements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\",query." + mGraph.PropertyName() + ", ParameterDirection.Input)"));
                }

                cmSelect.Statements.Add(ccsField);
            }

            cmSelect.Statements.Add(new CodeSnippetExpression("return this.Access.ExecuteDataSet()"));
            cmSelect.Comments.Add(new CodeCommentStatement("Select by Object [Implements Query By Example], returns DataSet"));
            return cmSelect;
        }

        public CodeMemberMethod BuildSelectObjects(TableViewTableTypeBase table)
        {
            // Accepts a DataRow and Returns a Poco of this type. 
            CodeMemberMethod cmmPB = new CodeMemberMethod();
            cmmPB.Name = "SelectObjects";
            cmmPB.Attributes = MemberAttributes.Public;
            cmmPB.ReturnType = new CodeTypeReference("Collection<" + table.Name + ">");

            CodeParameterDeclarationExpression cpdeDataRow = new CodeParameterDeclarationExpression();
            cpdeDataRow.Name = "query";
            cpdeDataRow.Type = new CodeTypeReference(table.Name);
            cpdeDataRow.Direction = FieldDirection.In;
            cmmPB.Parameters.Add(cpdeDataRow);
            cmmPB.Statements.Add(new CodeSnippetExpression("Collection<" + table.Name + "> objs = new Collection<" + table.Name + ">()"));
            cmmPB.Statements.Add(new CodeSnippetExpression("DataSet dsResults = Select(query)"));

            CodeVariableDeclarationStatement varEnumerator = new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(System.Collections.IEnumerator)), "e", new CodeSnippetExpression("dsResults.Tables[0].Rows.GetEnumerator()"));
            cmmPB.Statements.Add(varEnumerator);

            // Creates a for loop that sets testInt to 0 and continues incrementing testInt by 1 each loop until testInt is not less than 10.
            CodeSnippetStatement cssWhile = new CodeSnippetStatement("while(e.MoveNext()) { \n DataRow row = (DataRow)e.Current;\n " + table.Name + " obj = Fill(row);\n objs.Add(obj);\n }");
            cmmPB.Statements.Add(cssWhile);
            cmmPB.Statements.Add(new CodeSnippetExpression("return objs"));
            cmmPB.Comments.Add(new CodeCommentStatement("Returns an IEnumerable of Hydrated POCO's"));
            return cmmPB;
        }
        #endregion 

        #region [ DML Methods ]

        #region [ Update Methods ]
        /// <summary>
        /// Update Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildUpdate(Table table)
        {
            CodeMemberMethod cmUpdate = new CodeMemberMethod();
            cmUpdate.Attributes = MemberAttributes.Public;
            cmUpdate.Name = "Update";
            String cp_name = "cp_" + table.Name;

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet 
                }
                else
                {
                    cmUpdate.Parameters.Add(mGraph.GetParameter());
                }
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 4, ParameterDirection.Input)"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", " + mGraph.ParameterName() + ", ParameterDirection.Input)")); 
                }
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("int value = this.Access.ExecuteNonQuery()"));
            cmUpdate.Statements.Add(new CodeSnippetExpression("return value"));
            cmUpdate.ReturnType = new CodeTypeReference("System.Int32");
            cmUpdate.Comments.Add(new CodeCommentStatement("Updates a Record"));
            return cmUpdate;
        }
        private CodeMemberMethod BuildPocoUpdate(Table table)
        {
            CodeMemberMethod cmUpdate = new CodeMemberMethod();
            cmUpdate.Attributes = MemberAttributes.Public;
            cmUpdate.Name = "Update";
            CodeParameterDeclarationExpression cpPocoPar = new CodeParameterDeclarationExpression();
            cpPocoPar.Name = "query";
            cpPocoPar.Type = new CodeTypeReference(table.Name);
            cmUpdate.Parameters.Add(cpPocoPar);

            String cp_name = "cp_" + table.Name;

            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 4, ParameterDirection.Input)"));

            CodeStatementCollection AttributeValidationStatements = GraphAttributeSet.BuildAttributeSetStatement(table);

            foreach(CodeStatement AttributeValidationStatment in AttributeValidationStatements)
            {
                cmUpdate.Statements.Add(AttributeValidationStatment);
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("int value = this.Access.ExecuteNonQuery()"));
            cmUpdate.Statements.Add(new CodeSnippetExpression("return value"));
            cmUpdate.ReturnType = new CodeTypeReference("System.Int32");
            cmUpdate.Comments.Add(new CodeCommentStatement("Updates a Record"));
            return cmUpdate;
        }
        #endregion 

        #region [ Insert Methods ] 
        /// <summary>
        /// Insert Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildInsert(Table table)
        {
            CodeMemberMethod cmInsert = new CodeMemberMethod();
            cmInsert.Name = "Insert";
            String cp_name = "cp_" + table.Name;
            cmInsert.Attributes = MemberAttributes.Public;
            cmInsert.ReturnType = new CodeTypeReference("System.Int32");

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmInsert.Parameters.Add(mGraph.GetParameter());
                }
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 5, ParameterDirection.Input)"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", " + mGraph.ParameterName() + " , ParameterDirection.Input)"));
                }
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("return this.Access.ExecuteNonQuery()"));
            cmInsert.Comments.Add(new CodeCommentStatement("Inserts a record"));
            return cmInsert;
        }
        private CodeMemberMethod BuildPocoInsert(Table table)
        {
            CodeMemberMethod cmInsert = new CodeMemberMethod();
            
            cmInsert.Name = "Insert";
            String cp_name = "cp_" + table.Name;
            cmInsert.Attributes = MemberAttributes.Public;
            cmInsert.ReturnType = new CodeTypeReference("System.Int32");
            cmInsert.Parameters.Add(this.PocoQueryParameter(table.Name));
      
            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 5, ParameterDirection.Input)"));

            CodeStatementCollection AttributeValidationStatmenets = GraphAttributeSet.BuildAttributeSetStatement(table);

            foreach (CodeStatement AttributeValidationStatmenet in AttributeValidationStatmenets)
            {
                cmInsert.Statements.Add(AttributeValidationStatmenet);
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("return this.Access.ExecuteNonQuery()"));
            cmInsert.Comments.Add(new CodeCommentStatement("Inserts a record"));
            return cmInsert;
        }
        #endregion 

        #region [ Delete Methods ] 
        #endregion 
        #endregion 
        #endregion

        #region [ Update Methods ]
        /// <summary>
        /// Update Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildUpdate(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmUpdate = new CodeMemberMethod();
            cmUpdate.Attributes = MemberAttributes.Public;
            cmUpdate.Name = "Update";
            String cp_name = "cp_" + table.Name;

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet 
                }
                else
                {
                    cmUpdate.Parameters.Add(mGraph.GetParameter());
                }
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 4, ParameterDirection.Input)"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", " + mGraph.ParameterName() + ", ParameterDirection.Input)")); 
                }
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("var value  = this.Access.ExecuteNonQuery()"));
            cmUpdate.Statements.Add(new CodeSnippetExpression("return value"));
            cmUpdate.ReturnType = new CodeTypeReference("System.Int32");
            cmUpdate.Comments.Add(new CodeCommentStatement("Updates a Record"));
            return cmUpdate;
        }
        private CodeMemberMethod BuildPocoUpdate(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmUpdate = new CodeMemberMethod();
            cmUpdate.Attributes = MemberAttributes.Public;
            cmUpdate.Name = "Update";
            CodeParameterDeclarationExpression cpPocoPar = new CodeParameterDeclarationExpression();
            cpPocoPar.Name = "query";
            cpPocoPar.Type = new CodeTypeReference(table.Name);
            cmUpdate.Parameters.Add(cpPocoPar);

            String cp_name = "cp_" + table.Name;

            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmUpdate.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 4, ParameterDirection.Input)"));

            CodeStatementCollection AttributeValidationStatements = GraphAttributeSet.BuildAttributeSetStatement(table);

            foreach(CodeStatement AttributeValidationStatment in AttributeValidationStatements)
            {
                cmUpdate.Statements.Add(AttributeValidationStatment);
            }

            cmUpdate.Statements.Add(new CodeSnippetExpression("int value = this.Access.ExecuteNonQuery()"));
            cmUpdate.Statements.Add(new CodeSnippetExpression("return value"));
            cmUpdate.ReturnType = new CodeTypeReference("System.Int32");
            cmUpdate.Comments.Add(new CodeCommentStatement("Updates a Record"));
            return cmUpdate;
        }
        #endregion 

        #region [ Insert Methods ] 
        /// <summary>
        /// Insert Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildInsert(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmInsert = new CodeMemberMethod();
            cmInsert.Name = "Insert";
            String cp_name = "cp_" + table.Name;
            cmInsert.Attributes = MemberAttributes.Public;
            cmInsert.ReturnType = new CodeTypeReference("System.Int32");

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmInsert.Parameters.Add(mGraph.GetParameter());
                }
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 5, ParameterDirection.Input)"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet
                }
                else
                {
                    cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", " + mGraph.ParameterName() + " , ParameterDirection.Input)"));
                }
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("return this.Access.ExecuteNonQuery()"));
            cmInsert.Comments.Add(new CodeCommentStatement("Inserts a record"));
            return cmInsert;
        }
        private CodeMemberMethod BuildPocoInsert(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmInsert = new CodeMemberMethod();
            
            cmInsert.Name = "Insert";
            String cp_name = "cp_" + table.Name;
            cmInsert.Attributes = MemberAttributes.Public;
            cmInsert.ReturnType = new CodeTypeReference("System.Int32");
            cmInsert.Parameters.Add(this.PocoQueryParameter(table.Name));

            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmInsert.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 5, ParameterDirection.Input)"));

            CodeStatementCollection AttributeValidationStatmenets = GraphAttributeSet.BuildAttributeSetStatement(table);

            foreach (CodeStatement AttributeValidationStatmenet in AttributeValidationStatmenets)
            {
                cmInsert.Statements.Add(AttributeValidationStatmenet);
            }

            cmInsert.Statements.Add(new CodeSnippetExpression("return this.Access.ExecuteNonQuery()"));
            cmInsert.Comments.Add(new CodeCommentStatement("Inserts a record"));
            return cmInsert;
        }
        #endregion 

        #region [ Delete Methods ] 
        /// <summary>
        /// Delete Query Method
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        private CodeMemberMethod BuildDelete(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmDelete = new CodeMemberMethod();
            cmDelete.Attributes = MemberAttributes.Public;
            cmDelete.Name = "Delete";
            String cp_name = "cp_" + table.Name;
            cmDelete.ReturnType = new CodeTypeReference("System.Int32");
            cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 6, ParameterDirection.Input)"));
            foreach (Column c in table.Columns)
            {
                if (c.InPrimaryKey)
                {
                    MemberGraph mGraph = new MemberGraph(c);
                    cmDelete.Parameters.Add(mGraph.GetParameter());
                    cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", " + mGraph.ParameterName() + ", ParameterDirection.Input)"));
                }
            }

            cmDelete.Statements.Add(new CodeSnippetExpression("var value = this.Access.ExecuteNonQuery()"));
            cmDelete.Statements.Add(new CodeSnippetExpression("return value"));
            cmDelete.Comments.Add(new CodeCommentStatement("Delete's a record"));

            return cmDelete;
        }
        private CodeMemberMethod BuildPocoDelete(TableViewTableTypeBase table)
        {
            CodeMemberMethod cmDelete = new CodeMemberMethod();
            cmDelete.Attributes = MemberAttributes.Public;
            cmDelete.Name = "Delete";
            String cp_name = "cp_" + table.Name;
            cmDelete.Parameters.Add(this.PocoQueryParameter(table.Name));
            cmDelete.ReturnType = new CodeTypeReference("System.Int32");
            cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.CreateProcedureCommand(\"ProcedureName\")".Replace("ProcedureName", cp_name)));
            cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"Operation\", 6, ParameterDirection.Input)"));


            foreach (Column c in table.Columns)
            {
                if (c.InPrimaryKey)
                {
                    MemberGraph mGraph = new MemberGraph(c);
                    if (mGraph.IsNullable)
                    {

                        cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", query." + mGraph.PropertyName() + ".Value, ParameterDirection.Input)"));
                    }
                    else
                    {
                        cmDelete.Statements.Add(new CodeSnippetExpression("this.Access.AddParameter(\"" + mGraph.PropertyName() + "\", query." + mGraph.PropertyName() + ", ParameterDirection.Input)"));
                    }
                }
            }

            cmDelete.Statements.Add(new CodeSnippetExpression("var value = this.Access.ExecuteNonQuery()"));
            cmDelete.Statements.Add(new CodeSnippetExpression("return value"));
            cmDelete.Comments.Add(new CodeCommentStatement("Delete's a record"));

            return cmDelete;
        }
        #endregion 
    }
}
