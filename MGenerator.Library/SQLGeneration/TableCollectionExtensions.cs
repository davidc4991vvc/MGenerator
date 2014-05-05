using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.SqlServer.Management.Smo;

namespace MGenerator.Tools.SqlGeneration
{
    public static class TableCollectionExtensions
    {
        #region [ Methods ]

        public static bool Contains(this TableCollection tables, Table table)
        {
            foreach (Table current in tables)
            {
                if (current == table)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion
    }
}
