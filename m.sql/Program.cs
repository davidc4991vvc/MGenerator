using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m.sql
{
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
            string path = @"E:\github\nebula\ResourceIndex\Procedures";
            DirectoryInfo sql_dir = new DirectoryInfo(path);
            string sql = "";
            string output_path = @"E:\github\nebula\ResourceIndex\Procedures\all.sql";

            foreach (var sql_file in sql_dir.GetFiles("*.sql", SearchOption.AllDirectories))
            {
                sql = sql + String.Format("{0} \n", File.ReadAllText(sql_file.FullName).Replace("ALTER PROCEDURE","CREATE PROCEDURE"));
            }

            File.WriteAllText(output_path, sql);
        }
    }
}
