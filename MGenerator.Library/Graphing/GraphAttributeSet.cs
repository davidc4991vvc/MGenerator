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


namespace MGenerator.Tools
{
    public class GraphAttributeSet
    {
        /// <summary>
        /// Builds List of Code Statmenets that converts a Type into a DAL Parameter Statement
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static CodeStatementCollection BuildAttributeSetStatement(TableViewTableTypeBase table)
        {
            CodeStatementCollection AttributeSetStatement = new CodeStatementCollection();
            String PocoTypeName = table.Name;
            String FullPocoTypeName = PocoTypeName;

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                if (mGraph.IsReadOnly)
                {
                    // nothing yet 
                }
                else
                {
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

                    AttributeSetStatement.Add(ccsField);
                }
            }

            return AttributeSetStatement;
        }

        public static CodeStatementCollection BuildValidationSetStatement(TableViewTableTypeBase table)
        {
            CodeStatementCollection ValidationSetStatement = new CodeStatementCollection();
            String PocoTypeName = table.Name;
            
            String FullPocoTypeName = PocoTypeName;
            ValidationSetStatement.Add(new CodeSnippetExpression("Boolean bResult = new Boolean()"));
            ValidationSetStatement.Add(new CodeSnippetExpression("bResult = true"));

            foreach (Column c in table.Columns) 
            {
                MemberGraph mGraph = new MemberGraph(c);

                CodeConditionStatement csTest1 = new CodeConditionStatement();
                if (mGraph.IsNullable)
                {
                    csTest1.Condition = new CodeSnippetExpression("query." + mGraph.PropertyName() + ".HasValue == false");
                    csTest1.TrueStatements.Add(new CodeSnippetExpression("bResult = false"));
                }
                else
                {
                    csTest1.Condition = new CodeSnippetExpression("query." + mGraph.PropertyName() + " == null");
                    csTest1.TrueStatements.Add(new CodeSnippetExpression("bResult = false"));
                }
            }
        
           ValidationSetStatement.Add(new CodeSnippetExpression("return bResult"));
             
            return ValidationSetStatement;
        }

        public static CodeStatementCollection BuildDetectChangedMembers(TableViewTableTypeBase table)
        {
            CodeStatementCollection ValidationSetStatement = new CodeStatementCollection();
            String PocoTypeName = "this";
            ValidationSetStatement.Add(new CodeSnippetExpression("Boolean bResult = new Boolean()"));
            ValidationSetStatement.Add(new CodeSnippetExpression("bResult = false"));

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                CodeConditionStatement csTest1 = new CodeConditionStatement();

                if (mGraph.IsNullable)
                {
                    csTest1.Condition = new CodeSnippetExpression(PocoTypeName + "." + mGraph.PropertyName() + ".HasValue == true");
                    csTest1.TrueStatements.Add(new CodeSnippetExpression("bResult = true"));
                }
                else
                {
                    csTest1.Condition = new CodeSnippetExpression(PocoTypeName + "." + mGraph.PropertyName() + " == null");
                    csTest1.TrueStatements.Add(new CodeSnippetExpression(""));
                    csTest1.FalseStatements.Add(new CodeSnippetExpression("bResult = true"));
                }
            }

            return ValidationSetStatement;
        }
    }
}
