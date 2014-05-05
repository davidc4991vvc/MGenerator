using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.SqlServer;
using Microsoft.SqlServer.Management;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;


namespace MGenerator.Tools
{
    /// <summary>
    /// Convert a base data type to another base data type
    /// </summary>
    public sealed class TypeConvertor
    {
        #region [ Type System Attributes ]
        private static ArrayList _DbTypeList = new ArrayList();
        #endregion
        private struct DbTypeMapEntry
        {
            #region [ Type Attributes ]
            public Type Type;
            public DbType DbType;
            public Microsoft.SqlServer.Management.Smo.SqlDataType SqlDbType;
            public System.Xml.Schema.XmlTypeCode XSchemaType;


            #endregion
            public DbTypeMapEntry(Type type,
                                  DbType dbType,
                                  Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType,
                                  System.Xml.Schema.XmlTypeCode xSchemaType)
            {
                this.Type = type;
                this.DbType = dbType;
                this.SqlDbType = sqlDbType;
                this.XSchemaType = xSchemaType;
            }

        };


        #region Constructors

        static TypeConvertor()
        {
            DbTypeMapEntry dbTypeMapEntry = new DbTypeMapEntry(typeof(bool), DbType.Boolean, Microsoft.SqlServer.Management.Smo.SqlDataType.Bit, XmlTypeCode.Boolean);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(byte), DbType.Double, Microsoft.SqlServer.Management.Smo.SqlDataType.TinyInt, XmlTypeCode.Short);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(byte[]), DbType.Binary, Microsoft.SqlServer.Management.Smo.SqlDataType.Image, XmlTypeCode.HexBinary);
            _DbTypeList.Add(dbTypeMapEntry);

            // DATETIME 
            dbTypeMapEntry = new DbTypeMapEntry(typeof(DateTime), DbType.DateTime, Microsoft.SqlServer.Management.Smo.SqlDataType.DateTime, XmlTypeCode.DateTime);
            _DbTypeList.Add(dbTypeMapEntry);
            dbTypeMapEntry = new DbTypeMapEntry(typeof(DateTime), DbType.DateTime, Microsoft.SqlServer.Management.Smo.SqlDataType.DateTime2, XmlTypeCode.DateTime);
            _DbTypeList.Add(dbTypeMapEntry);
            dbTypeMapEntry = new DbTypeMapEntry(typeof(DateTime), DbType.DateTime, Microsoft.SqlServer.Management.Smo.SqlDataType.Date, XmlTypeCode.DateTime);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Decimal), DbType.Decimal, Microsoft.SqlServer.Management.Smo.SqlDataType.Decimal, XmlTypeCode.Decimal);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(double), DbType.Double, Microsoft.SqlServer.Management.Smo.SqlDataType.Float, XmlTypeCode.Float);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Guid), DbType.Guid, Microsoft.SqlServer.Management.Smo.SqlDataType.UniqueIdentifier, XmlTypeCode.Text);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Int16), DbType.Int16, Microsoft.SqlServer.Management.Smo.SqlDataType.SmallInt, XmlTypeCode.Short);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Int32), DbType.Int32, Microsoft.SqlServer.Management.Smo.SqlDataType.Int, XmlTypeCode.Int);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Int64), DbType.Int64, Microsoft.SqlServer.Management.Smo.SqlDataType.BigInt, XmlTypeCode.Long);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(object), DbType.Object, Microsoft.SqlServer.Management.Smo.SqlDataType.Variant, XmlTypeCode.Element);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(string), DbType.String, Microsoft.SqlServer.Management.Smo.SqlDataType.VarChar, XmlTypeCode.String);
            _DbTypeList.Add(dbTypeMapEntry);

            dbTypeMapEntry = new DbTypeMapEntry(typeof(Decimal), DbType.VarNumeric, Microsoft.SqlServer.Management.Smo.SqlDataType.Numeric, XmlTypeCode.Decimal);
            _DbTypeList.Add(dbTypeMapEntry);
        }


        private TypeConvertor()
        {


        }

        #endregion

        #region Methods

        /// <summary>
        /// Convert db type to .Net data type
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static Type ToNetType(DbType dbType)
        {
            DbTypeMapEntry entry = Find(dbType);
            return entry.Type;
        }

        /// <summary>
        /// Convert TSQL type to .Net data type
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static Type ToNetType(Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            Type NetType = entry.Type;

            if (NetType == typeof(Decimal))
            {
                NetType = typeof(Nullable<Decimal>);
            }

            if (NetType == typeof(Boolean))
            {
                NetType = typeof(bool?);
            }

            if (NetType == typeof(Int32))
            {
                NetType = typeof(int?);
            }

            if (NetType == typeof(Int64))
            {
                NetType = typeof(long?);
            }

            if (NetType == typeof(DateTime))
            {
                NetType = typeof(DateTime?);
            }

            return NetType;
        }
        public static Type ToBasicNetType(Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            Type NetType = entry.Type;
            return NetType;
        }


        public static Type ToNetType(XmlTypeCode xmlType)
        {
            DbTypeMapEntry entry = Find(xmlType);
            return entry.Type;
        }

        public static XmlTypeCode ToXmlType(Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            return entry.XSchemaType;
        }

        /// <summary>
        /// Convert .Net type to Db type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DbType ToDbType(Type type)
        {
            DbTypeMapEntry entry = Find(type);
            return entry.DbType;
        }

        /// <summary>
        /// Convert TSQL data type to DbType
        /// </summary>
        /// <param name="sqlDbType"></param>
        /// <returns></returns>
        public static DbType ToDbType(Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType)
        {
            DbTypeMapEntry entry = Find(sqlDbType);
            return entry.DbType;
        }

        /// <summary>
        /// Convert .Net type to TSQL data type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Microsoft.SqlServer.Management.Smo.SqlDataType ToSqlDbType(Type type)
        {
            DbTypeMapEntry entry = Find(type);
            return entry.SqlDbType;
        }

        /// <summary>
        /// Convert DbType type to TSQL data type
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public static Microsoft.SqlServer.Management.Smo.SqlDataType ToSqlDbType(DbType dbType)
        {
            DbTypeMapEntry entry = Find(dbType);
            return entry.SqlDbType;
        }

        private static DbTypeMapEntry Find(Type type)
        {
            object retObj = null;
            for (int i = 0; i < _DbTypeList.Count; i++)
            {
                DbTypeMapEntry entry = (DbTypeMapEntry)_DbTypeList[i];
                if (entry.Type == type)
                {
                    retObj = entry;
                    break;
                }
            }
            if (retObj == null)
            {
                throw
                new ApplicationException("Referenced an unsupported Type");
            }

            return (DbTypeMapEntry)retObj;
        }
        private static DbTypeMapEntry Find(DbType dbType)
        {
            object retObj = null;
            for (int i = 0; i < _DbTypeList.Count; i++)
            {
                DbTypeMapEntry entry = (DbTypeMapEntry)_DbTypeList[i];
                if (entry.DbType == dbType)
                {
                    retObj = entry;
                    break;
                }
            }
            if (retObj == null)
            {
                throw
                new ApplicationException("Referenced an unsupported DbType");
            }

            return (DbTypeMapEntry)retObj;
        }
        private static DbTypeMapEntry Find(XmlTypeCode xType)
        {
            object retObj = null;
            for (int i = 0; i < _DbTypeList.Count; i++)
            {
                DbTypeMapEntry entry = (DbTypeMapEntry)_DbTypeList[i];
                if (entry.XSchemaType == xType)
                {
                    retObj = entry;
                    break;
                }
            }
            if (retObj == null)
            {
                throw
                new ApplicationException("Referenced an unsupported DbType");
            }

            return (DbTypeMapEntry)retObj;
        }
        private static DbTypeMapEntry Find(Microsoft.SqlServer.Management.Smo.SqlDataType sqlDbType)
        {
            object retObj = new DbTypeMapEntry(typeof(System.String), DbType.String, Microsoft.SqlServer.Management.Smo.SqlDataType.VarChar, XmlTypeCode.String);
            for (int i = 0; i < _DbTypeList.Count; i++)
            {
                DbTypeMapEntry entry = (DbTypeMapEntry)_DbTypeList[i];
                if (entry.SqlDbType == sqlDbType)
                {
                    retObj = entry;
                    break;
                }
            }

            return (DbTypeMapEntry)retObj;
        }
        #endregion

        public static IEnumerable<Type> NumericTypes()
        {
            List<Type> NTypes = new List<Type>();
            NTypes.Add(typeof(System.Int16));
            NTypes.Add(typeof(System.Int32));
            NTypes.Add(typeof(System.Int64));
            NTypes.Add(typeof(System.Decimal));
            NTypes.Add(typeof(float));

            return NTypes;
        }

        public static IEnumerable<Type> NullabeTypes()
        {
            List<Type> NTypes = new List<Type>();

            NTypes.Add(typeof(System.Boolean));
            NTypes.Add(typeof(System.Decimal));
            NTypes.Add(typeof(bool));

            foreach (Type t in TypeConvertor.NumericTypes())
            {
                NTypes.Add(t);
            }

            return NTypes;
        }

        public static IEnumerable<Type> StringTypes()
        {
            List<Type> NTypes = new List<Type>();

            NTypes.Add(typeof(System.String));
            NTypes.Add(typeof(string));

            return NTypes;
        }
    }

    public class TypeSystem
    {
        public String TypeName { get; set; }
        public Type Type { get; set; }
    }
}
