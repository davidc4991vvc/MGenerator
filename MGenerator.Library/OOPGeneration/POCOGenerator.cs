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

    public class POCOGenerator
    {
        public CodeTypeDeclaration BuildPoc(TableViewBase rootModel)
        {
            CodeTypeDeclaration ctd = new CodeTypeDeclaration();
            ConstructorGraph ctorGraph = new ConstructorGraph();

            ctd.Name = rootModel.Name;
            ctd.TypeAttributes = System.Reflection.TypeAttributes.Public;
            ctd.Attributes = MemberAttributes.Public;
            ctd.IsPartial = true;
            CodeAttributeDeclaration cad_DataContract = new CodeAttributeDeclaration(" System.Runtime.Serialization.DataContractAttribute");
            ctd.CustomAttributes.Add(cad_DataContract);
            
            // Members 
            foreach (Column c in rootModel.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);

                CodeMemberField c_field = mGraph.GetField();
                CodeMemberProperty c_prop = mGraph.GetProperty();
                
                CodeAttributeDeclaration cad_DataMember = new CodeAttributeDeclaration("System.Runtime.Serialization.DataMemberAttribute");
                c_prop.CustomAttributes.Add(cad_DataMember);

                ctd.Members.Add(c_field);
                ctd.Members.Add(c_prop);
            }

            foreach (CodeConstructor cc in ctorGraph.GraphConstructors(rootModel))
            {
                ctd.Members.Add(cc);
            }

            return ctd; 
        }

        public CodeTypeDeclaration BuildPoco(Table table)
        {
            CodeTypeDeclaration ctd     = new CodeTypeDeclaration();
            ConstructorGraph ctorGraph  = new ConstructorGraph();

            ctd.Name            = table.Name;
            ctd.TypeAttributes  = System.Reflection.TypeAttributes.Public;
            ctd.Attributes      = MemberAttributes.Public;
            ctd.IsPartial       = true;

            // Members 
            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                ctd.Members.Add(mGraph.GetField());
                ctd.Members.Add(mGraph.GetProperty());
            }

            foreach (CodeConstructor cc in ctorGraph.GraphConstructors(table))
            {
                ctd.Members.Add(cc);
            }

            ctd.Members.AddRange(AddMethods(table));
            return ctd;
        }

        public CodeTypeDeclaration BuildPoco(View view)
        {
            CodeTypeDeclaration ctd = new CodeTypeDeclaration();
            ConstructorGraph ctorGraph = new ConstructorGraph();

            ctd.Name = view.Name;
            ctd.TypeAttributes = System.Reflection.TypeAttributes.Public;
            ctd.Attributes = MemberAttributes.Public;
            ctd.IsPartial = true;

            // Members 
            foreach (Column c in view.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                ctd.Members.Add(mGraph.GetField());
                ctd.Members.Add(mGraph.GetProperty());
            }
             
            // View only Gets One Constructor
            ctd.Members.Add(ctorGraph.GraphConstructors(view)[0]);

            ctd.Members.AddRange(AddMethods(view));

            return ctd;
        }

        public CodeTypeMemberCollection AddMethods(TableViewTableTypeBase baseobj)
        {
            CodeTypeMemberCollection ctmc = new CodeTypeMemberCollection();
            ctmc.Add(BuildIsDirtyMethod(baseobj));
            return ctmc;
        }

        public CodeMemberMethod BuildIsDirtyMethod(TableViewTableTypeBase baseobj)
        {
               CodeMemberMethod cmd = new CodeMemberMethod();
               cmd.Name = "IsDirty";
               cmd.Attributes = MemberAttributes.Public;
               cmd.ReturnType = new CodeTypeReference("System.Boolean");

               CodeStatementCollection cssDirtyCode = GraphAttributeSet.BuildValidationSetStatement(baseobj);

               foreach (CodeStatement cs in GraphAttributeSet.BuildValidationSetStatement(baseobj))
               {
                   cmd.Statements.Add(cs);
               }
               return cmd;
        }
    }
}
