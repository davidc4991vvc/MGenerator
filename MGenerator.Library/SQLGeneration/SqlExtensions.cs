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
    public class RelationObject
    {
        public string TableName { get; set; }
        public String ColumnName { get; set; } 
    }

    public static class SmoExtensions
    {
        public static IEnumerable<ForeignKey> ForeignKeys(this ForeignKeyCollection fkc)
        {
            Collection<ForeignKey> colFkey = new Collection<ForeignKey>();

            foreach (ForeignKey fk in fkc)
            {
                colFkey.Add(fk);
            }

            return colFkey;
        }
        public static IEnumerable<ForeignKeyColumn> FKeyColumns(this ForeignKeyColumnCollection fkcc)
        {
            Collection<ForeignKeyColumn> fkColumns = new Collection<ForeignKeyColumn>();

            foreach (ForeignKeyColumn fkColumn in fkcc)
            {
                fkColumns.Add(fkColumn);
            }

            return fkColumns;
        }
        public static IEnumerable<Column> GetColumns(this ForeignKeyColumnCollection fkcc)
        {
            Collection<Column> columns = new Collection<Column>();

            foreach (var fkCol in fkcc.FKeyColumns())
            {
                Column col = new Column(fkCol.Parent, fkCol.Name);
                columns.Add(col);
            }

            return columns;
        }
        public static Boolean HasPrimaryKey(this ColumnCollection cc)
        {
            Boolean bResult = false;

            foreach(Column c in cc)
            {
                if (c.InPrimaryKey == true)
                {
                    bResult = true;
                }
            }

            return bResult;
        }
        public static Boolean IsNormalized(this Table table)
        {
            return  ((table.Indexes.Count > 0) && (table.Columns.HasPrimaryKey()));
        }
        public static IEnumerable<RelationObject> GetReferenceTables(this Table t)
        {
            Collection<RelationObject> relations = new Collection<RelationObject>();

            foreach (ForeignKey fkey in t.ForeignKeys)
            {
                RelationObject rho = new RelationObject();
                rho.TableName = fkey.ReferencedTable;
                rho.ColumnName = fkey.ReferencedKey;
                relations.Add(rho);
            }

            return relations;
        }
        public static Boolean IsView(this  Microsoft.SqlServer.Management.Smo.SqlSmoObject b)
        {
            Boolean bIsVIew = false;
            
            if (b is View)
            {
                bIsVIew = true;
            }


            return bIsVIew;
        }

    }
}
