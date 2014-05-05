using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;

namespace MGenerator.Tools.SqlGeneration
{
    public class ColumnComparer : IComparer<Column>
    {
        #region [ Methods ]

        public int Compare(Column x, Column y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    return string.Empty.CompareTo(string.Empty);
                }
                return y.Name.CompareTo(string.Empty);
            }
            return x.Name.CompareTo(y.Name);
        }

        #endregion
    }
}
