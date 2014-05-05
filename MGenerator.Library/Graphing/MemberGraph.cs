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
    /// <summary>
    /// Represents a Member of a Type
    /// <remarks></remarks>
    /// </summary>
    public class MemberGraph
    {
        #region [ Properties ] 
        public String Name { get; set; }
        public Column SqlColumn { get; set; }
        public Type GraphType { get; set; }
        public Boolean Required { get; set; }
        public Boolean IsNullable { get; set; }
        public Boolean IsReadOnly { get; set; } 
        #endregion 

        #region [ Constructors ]
        public MemberGraph(Column sqlColumn)
        {
            this.SqlColumn = sqlColumn;
            this.Name = SqlColumn.Name;
            this.GraphType = TypeConvertor.ToNetType(sqlColumn.DataType.SqlDataType);
            this.IsReadOnly = false;

            if (sqlColumn.Computed)
            {
                this.IsReadOnly = true;
            }
         
            if (sqlColumn.Parent.IsView())
            {
                this.IsReadOnly = true;
            }


            if (TypeConvertor.NullabeTypes().Contains(this.GraphType))
            {
                this.IsNullable = true;
            }
            else
            {
                this.IsNullable = false;
            }

            if (this.SqlColumn.Nullable)
            {
                this.Required = false;
            }
            else
            {
                this.Required = true;
            }
        }
        #endregion 

        #region [ Members ]
        
        public String FieldName()
        {
            return "_" + this.Name;
        }

        public String PropertyName()
        {
            String cleanPropertyName = ""; 
            char a = this.Name[0];


            if (Char.IsDigit(a))
            {
                cleanPropertyName = "P" + this.Name;
            }
            else
            {
                cleanPropertyName = this.Name;
            }
            return cleanPropertyName;
        }

        public String ParameterName()
        {
            return "p" + this.Name; 
        }
        /// <summary>
        /// Safe Type Name
        /// <remarks>Retrurns the safe Type Name</remarks>
        /// </summary>
        /// <returns></returns>
        public String TypeName()
        {
            Type NetType = TypeConvertor.ToBasicNetType(this.SqlColumn.DataType.SqlDataType);
            String SafeTypeName = NetType.Name;

                if (NetType == typeof(Boolean))
                {
                    SafeTypeName = "bool?";
                }

                if (NetType == typeof(Decimal))
                {
                    SafeTypeName = "Nullable<Decimal>";
                }

                if (NetType == typeof(Int32))
                {
                    SafeTypeName = "int?";
                }

                if (NetType == typeof(Int64))
                {
                    SafeTypeName = "long?";
                }

                if (NetType == typeof(DateTime))
                {
                    SafeTypeName = "DateTime?";
                }

            return SafeTypeName;
        }

        public CodeMemberField GetField()
        {
            CodeMemberField cmf = new CodeMemberField();
            // Version 2 TODO: Add Custom Attributes based on Data column 
            //                 Add ASP.NET UI Element Attributes
            cmf.Name = this.FieldName();
            cmf.Attributes = MemberAttributes.Private;
            cmf.Type = new CodeTypeReference(TypeConvertor.ToNetType(this.SqlColumn.DataType.SqlDataType));
            cmf.Type = new CodeTypeReference(this.GraphType);
            
            return cmf;
        }

        public CodeMemberProperty GetProperty()
        {
            CodeMemberProperty cmp = new CodeMemberProperty();

            cmp.Attributes = MemberAttributes.Public;
            cmp.Name = this.PropertyName();
            cmp.Type = new CodeTypeReference(this.GraphType);

            cmp.HasGet = true; 
            cmp.GetStatements.Add(new CodeSnippetExpression("return this." + this.FieldName()));

            //if (!this.SqlColumn.Computed)
            //{
                cmp.HasSet = true;
                cmp.SetStatements.Add(new CodeSnippetExpression(" this." + this.FieldName() + " = value"));
            //}

            return cmp;
        }

        public CodeParameterDeclarationExpression GetParameter()
        {
            CodeParameterDeclarationExpression cpde = new CodeParameterDeclarationExpression();

            cpde.Direction = FieldDirection.In;
            cpde.Name = this.ParameterName();
            cpde.Type = new CodeTypeReference(TypeConvertor.ToNetType(this.SqlColumn.DataType.SqlDataType)); 

            return cpde; 
        }

        public StoredProcedureParameter GetSqlParameter()
        {
            StoredProcedureParameter spPar = new StoredProcedureParameter();

            spPar.Name = this.SqlColumn.Name;
            spPar.DataType = this.SqlColumn.DataType;

            if (this.SqlColumn.Nullable)
            {
                if (this.SqlColumn.Default.Length > 0)
                {
                    spPar.DefaultValue = this.SqlColumn.Default;
                }
                else
                {
                    spPar.DefaultValue = "NULL";
                }
            }

            return spPar;
        }

        public CodeVariableDeclarationStatement BuildVariable()
        {
            CodeVariableDeclarationStatement cvds = new CodeVariableDeclarationStatement();
            cvds.Name = this.Name;
            cvds.Type = GetParameter().Type;
            return cvds; 
        }
        #endregion 
    }
}
