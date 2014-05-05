using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MGenerator.Library
{
    public enum DatabasePlatform
    {
        MSSQL
    }

    public class DatabaseSource
    {
        public String ServerName { get; set; }
        public DatabasePlatform Platform { get; set; }
        public String CatalogName { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; } 
        
    }
}
