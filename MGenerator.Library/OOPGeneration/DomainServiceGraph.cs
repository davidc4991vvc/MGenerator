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
using TSQL = Microsoft.SqlServer.Management.Smo;

using Microsoft.CSharp;
using System.IO;

namespace MGenerator.Tools
{
    public class DomainServiceGraph
    {
        public CodeTypeDeclaration BuildDomainObject(String DataLayerNamespace,String DataBaseName, TSQL.TableViewTableTypeBase table)
        {
            String FullEntityDAL = DataLayerNamespace + "." + DataBaseName + "." + table.Name + "DAL";
            String FullEntityTypeName = DataLayerNamespace + "." +  DataBaseName + "." + table.Name;
            
            String TypeName = table.Name + "DomainObject";
            String DalTypeName = FullEntityDAL;

            CodeTypeDeclaration ctdService = FrameFactory.GetPartialClassFrame(TypeName);
            CodeTypeReference IEnumEntity = new CodeTypeReference("IEnumerable<" + FullEntityTypeName + ">");
            CodeTypeReference IEntity = new CodeTypeReference((FullEntityTypeName));
            CodeTypeReference IIntReturn = new CodeTypeReference("System.Int32");
            CodeTypeReference IEntityOrder = new CodeTypeReference("Func<" + FullEntityTypeName + ",Object>");
            CodeTypeReference IEntityRule = new CodeTypeReference("Func<" + FullEntityTypeName + ",Int32>");
            CodeParameterDeclarationExpression cpEntityQuery = new CodeParameterDeclarationExpression(IEntity, "Query");
            CodeParameterDeclarationExpression cpEntityRule = new CodeParameterDeclarationExpression(IEntityRule, "BusinessRule");
            CodeParameterDeclarationExpression cpEntityOrderBy = new CodeParameterDeclarationExpression(IEntityOrder, "Orderby");
            CodeParameterDeclarationExpression cpSkip = new CodeParameterDeclarationExpression(new CodeTypeReference("Int32"), "Skip");
            CodeParameterDeclarationExpression cpTake = new CodeParameterDeclarationExpression(new CodeTypeReference("Int32"), "Take");
            
            CodeTypeReference ctrDal = new CodeTypeReference(DalTypeName);
            CodeVariableDeclarationStatement varDal = new CodeVariableDeclarationStatement(ctrDal, "dal", new CodeSnippetExpression(" new " + DalTypeName + "()"));

            #region [ Search Methods ] 
            CodeMemberMethod mSearchBasic = this.CreateServiceMethodBase("Search");
            mSearchBasic.Parameters.AddRange(new CodeParameterDeclarationExpression[] { cpEntityQuery });
            mSearchBasic.Statements.Add(varDal);
            mSearchBasic.Statements.Add(new CodeSnippetExpression("return dal.SelectObjects(Query)"));
            mSearchBasic.ReturnType = IEnumEntity;
            ctdService.Members.Add(mSearchBasic);

            CodeMemberMethod mSearch = this.CreateServiceMethodBase("Search");
            mSearch.Parameters.AddRange(new CodeParameterDeclarationExpression[] { cpEntityQuery, cpSkip, cpTake });
            mSearch.Statements.Add(varDal);
            mSearch.Statements.Add(new CodeSnippetExpression("return dal.SelectObjects(Query).Skip(Skip).Take(Take)"));
            mSearch.ReturnType = IEnumEntity;
            ctdService.Members.Add(mSearch);

            CodeMemberMethod mFullSearch = this.CreateServiceMethodBase("FullSearch");
            mFullSearch.Parameters.AddRange(new CodeParameterDeclarationExpression[] { cpEntityQuery, cpEntityOrderBy, cpSkip, cpTake });
            mFullSearch.Statements.Add(varDal);
            mFullSearch.Statements.Add(new CodeSnippetExpression("return dal.SelectObjects(Query).OrderBy(Orderby).Skip(Skip).Take(Take)"));
            mFullSearch.ReturnType = IEnumEntity;
            ctdService.Members.Add(mFullSearch);
            #endregion 

            #region [ OML Methods ] 
            /// If the parameter object is a Table then Generate OML methods
            if (table.GetType() == typeof(TSQL.Table))
            {
                CodeMemberMethod mUpdate = this.CreateServiceMethodBase("Update");
                mUpdate.Parameters.Add(cpEntityQuery);
                mUpdate.Statements.Add(varDal);
               // mUpdate.Statements.Add(new CodeSnippetExpression(" return dal.Update(Query)"));
                mUpdate.Statements.Add(FuncAction("dal","Update","Query"));
                mUpdate.ReturnType = IIntReturn;
                ctdService.Members.Add(mUpdate);

                CodeMemberMethod mInsert = this.CreateServiceMethodBase("Insert");
                mInsert.Parameters.Add(cpEntityQuery);
                mInsert.Statements.Add(varDal);
                mInsert.Statements.Add(new CodeSnippetExpression("Int32 rCode = new Int32()"));
                mInsert.Statements.Add(new CodeSnippetExpression("rCode = dal.Insert(Query)")); 
                mInsert.Statements.Add(new CodeSnippetExpression("return rCode"));
                mInsert.ReturnType = IIntReturn;
                ctdService.Members.Add(mInsert);

               
                                                  
                
                CodeMemberMethod mDelete = this.CreateServiceMethodBase("Delete");
                mDelete.Parameters.Add(cpEntityQuery);
                mDelete.Statements.Add(varDal);
                mDelete.Statements.Add(new CodeSnippetExpression("return dal.Delete(Query)"));
                mDelete.ReturnType = IIntReturn;
                ctdService.Members.Add(mDelete);
            }
            #endregion 

            #region [ Validation Methods ]
            CodeMemberMethod cmVal = this.CreateServiceMethodBase("ValidateNew");
            cmVal.Parameters.Add(cpEntityQuery);
            cmVal.Statements.AddRange(GraphAttributeSet.BuildValidationSetStatement(table));
            cmVal.Statements.Add(new CodeSnippetExpression("return bResult"));
            cmVal.ReturnType = new CodeTypeReference("Boolean");
            //ctdService.Members.Add(cmVal);
            #endregion 

            return ctdService;
        }

        public CodeTypeDeclaration BuildDomainServiceInterface(String DataLayerNamespace, String DataBaseName,TSQL.Table table)
        {
            String FullEntityTypeName = DataLayerNamespace + "." + DataBaseName + "." + table.Name;
            CodeTypeDeclaration ctdInterface = new CodeTypeDeclaration("I" + table.Name + "DomainService");
            CodeTypeReference IEnumEntity = new CodeTypeReference("IEnumerable<" + FullEntityTypeName + ">");
            CodeTypeReference IIntReturn = new CodeTypeReference("System.Int32");
            CodeTypeReference IEntity = new CodeTypeReference(FullEntityTypeName);
            CodeTypeReference IEntityOrder = new CodeTypeReference("Func<" + FullEntityTypeName + ",Object>");
            CodeTypeReference IEntityRule = new CodeTypeReference("Func<" + FullEntityTypeName + ",Int32>"); 

            CodeParameterDeclarationExpression cpEntityQuery = new CodeParameterDeclarationExpression(IEntity,"Query");
            CodeParameterDeclarationExpression cpEntityRule = new CodeParameterDeclarationExpression(IEntityRule, "BusinessRule");
            CodeParameterDeclarationExpression cpEntityOrderBy = new CodeParameterDeclarationExpression(IEntityOrder, "Orderby"); 
            CodeParameterDeclarationExpression cpSkip = new CodeParameterDeclarationExpression(new CodeTypeReference("Int32"),"Skip");
            CodeParameterDeclarationExpression cpTake = new CodeParameterDeclarationExpression(new CodeTypeReference("Int32"),"Take");

            // Build Interface
            ctdInterface.Attributes = MemberAttributes.Public;
            ctdInterface.IsInterface = true;
            ctdInterface.CustomAttributes.Add(new CodeAttributeDeclaration("ServiceContract"));

            
            CodeMemberMethod mSearch = this.CreateServiceInterfaceMethodBase("Search");
            mSearch.Parameters.AddRange(new CodeParameterDeclarationExpression[] { cpEntityQuery, cpSkip, cpTake });
            mSearch.ReturnType = IEnumEntity;

            CodeMemberMethod mFullSearch = this.CreateServiceInterfaceMethodBase("FullSearch");
            mFullSearch.Parameters.AddRange(new CodeParameterDeclarationExpression[] { cpEntityQuery,cpEntityOrderBy, cpSkip, cpTake });
            mFullSearch.ReturnType = IEnumEntity;

            CodeMemberMethod mUpdate = this.CreateServiceInterfaceMethodBase("Update");
            mUpdate.Parameters.Add(cpEntityQuery);
            mUpdate.ReturnType = IIntReturn;

            CodeMemberMethod mInsert = this.CreateServiceInterfaceMethodBase("Insert");
            mInsert.Parameters.Add(cpEntityQuery);
            mInsert.ReturnType = IIntReturn;

            CodeMemberMethod mDelete = this.CreateServiceInterfaceMethodBase("Delete");
            mDelete.Parameters.Add(cpEntityQuery);
            mDelete.ReturnType = IIntReturn;

            CodeMemberMethod mRule = this.CreateServiceInterfaceMethodBase("Rule");
            mRule.Parameters.Add(cpEntityRule);
            mRule.ReturnType = IIntReturn;

            ctdInterface.Members.Add(mSearch);
            ctdInterface.Members.Add(mFullSearch);
            ctdInterface.Members.Add(mUpdate);
            ctdInterface.Members.Add(mInsert);
            ctdInterface.Members.Add(mDelete);
            ctdInterface.Members.Add(mRule);

            return ctdInterface;
        }

        private CodeMemberMethod CreateServiceInterfaceMethodBase(String MethodName)
        {
            CodeMemberMethod m = new CodeMemberMethod();

            m.Attributes = MemberAttributes.Public;
            m.Name = MethodName;
            m.CustomAttributes.Add(new CodeAttributeDeclaration("OperationContract"));

            CodeAttributeDeclaration cadFault = new CodeAttributeDeclaration(new CodeTypeReference("FaultContract")); 
            cadFault.Arguments.Add(new CodeAttributeArgument(null, new CodeSnippetExpression("typeof(FaultException)")));

            m.CustomAttributes.Add(cadFault); 
            
            return m;
        }

        private CodeMemberMethod CreateServiceMethodBase(String MethodName)
        {
            CodeMemberMethod m = new CodeMemberMethod();

            m.Attributes = MemberAttributes.Public;
            m.Name = MethodName;

            return m;
        }
        
        /// <summary>
        /// Create Domain ACtion Method Code via Managed Template
        /// <remarks>return Type.Method(Parameter) </remarks>
        /// </summary>
        /// <returns></returns>
        private CodeSnippetExpression FuncAction(String Type,String Method, String Parameter)
        {
        		CodeSnippetExpression cse = new CodeSnippetExpression();
                String ActionCode = String.Format("return {0}.{1}({2})",Type,Method,Parameter);
                cse = new CodeSnippetExpression(ActionCode);
                return cse;
        }
    }
}
