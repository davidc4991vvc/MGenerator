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
    public class ConstructorGraph
    {
        /// <summary>
        /// Graphs a basic Constructor
        /// </summary>
        /// <returns></returns>
        private CodeConstructor GraphBasicConstructor()
        {
            CodeConstructor cc = new CodeConstructor();
            cc.Attributes = MemberAttributes.Public;
            return cc; 
        }
        /// <summary>
        /// Graphs a Data Access Class Constructor 
        /// </summary>
        /// <returns></returns>
        public CodeConstructor GraphDalConstructor(String DatabaseName)
        {
            CodeConstructor ccDal = GraphBasicConstructor();
            // Data Access is contained in the DataAccessBase Class (Layer SuperType)
            // The Base Constructor must be overidden with the Database Name.
            ccDal.BaseConstructorArgs.Add(new CodeSnippetExpression("\"" + DatabaseName + "\"")); 
            return ccDal;
        }
        /// <summary>
        /// Graphs a Business Object Constructor
        /// </summary>
        /// <returns></returns>
        private CodeConstructor GraphBusinessObject()
        {
            CodeConstructor ccBobj = GraphBasicConstructor();

            return ccBobj;
        }
        /// <summary>
        /// Returns the list of Graphed Constructors
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public List<CodeConstructor> GraphConstructors(TableViewTableTypeBase  table)
        {
            List<CodeConstructor> Constructors = new List<CodeConstructor>();

            // Graph the Empty Constructor 
            CodeConstructor ccEmpty = this.GraphBasicConstructor();
            
            // Graph the Identity/Key Constructor 
            CodeConstructor ccKey = this.GraphIdentityConstructor(table);

            // Graph the Full Constructor 
            CodeConstructor ccFull = this.GraphFullContructor(table);

            // Add all Constructors to List 
            Constructors.Add(ccEmpty);
            Constructors.Add(ccKey);
            Constructors.Add(ccFull); 

            return Constructors;
        }
        /// <summary>
        /// Returns a Simple contructor
        /// </summary>
        /// <returns></returns>
        /// 
        public CodeConstructor GraphConstructor()
        {
            return this.GraphBasicConstructor();
        }
        /// <summary>
        /// Empty basiuc contructor
        /// </summary>
        /// <returns></returns>
        public CodeConstructor GraphEmptyContructor()
        {
            return this.GraphBasicConstructor();
        }
        /// <summary>
        /// Contains only Identity Parameters
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public CodeConstructor GraphIdentityConstructor(TableViewTableTypeBase table)
        {
            CodeConstructor ccIdentity = this.GraphBasicConstructor();

            foreach (Column c in table.Columns)
            {
                if (c.InPrimaryKey)
                {
                    MemberGraph mGraph = new MemberGraph(c);
                    ccIdentity.Parameters.Add(mGraph.GetParameter());
                    ccIdentity.Statements.Add(new CodeSnippetExpression("this." + mGraph.FieldName() + "=" + mGraph.ParameterName()));
                }
            }
            return ccIdentity;
        }
        /// <summary>
        /// Contains all Fields
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public CodeConstructor GraphFullContructor(TableViewTableTypeBase table)
        {
            CodeConstructor ccFull = this.GraphBasicConstructor();

            foreach (Column c in table.Columns)
            {
                MemberGraph mGraph = new MemberGraph(c);
                ccFull.Parameters.Add(mGraph.GetParameter());
                ccFull.Statements.Add(new CodeSnippetExpression("this." + mGraph.FieldName() + "=" + mGraph.ParameterName()));
            }

            return ccFull;
        }
    }
}
