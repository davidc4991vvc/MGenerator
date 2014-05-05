﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MGenerator;
using MGenerator.Library;
using IniParser;

namespace MGenerator.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Reading Generation Info File \n");
            FileIniDataParser parser = new FileIniDataParser();
            IniData genData = parser.LoadFile("generation.ini");
            // TODO: Implement Functionality Here
            Console.WriteLine("Generation Info File Read \n");
            foreach (SectionData genSection in genData.Sections)
            {
                Console.WriteLine(String.Format("Analyzing {0} \n", genSection.SectionName));
                Console.WriteLine(String.Format("=============================================== \n", genSection.SectionName));
                GenerationInfo info = new GenerationInfo();
                info.CompanyName = genSection.Keys["companyname"];
                info.DataNameSpace = genSection.Keys["datanamespace"];
                info.FolderPath = genSection.Keys["path"];
                info.ServerName = genSection.Keys["server"];
                MGenerator.Tools.Generator.GenerateFromDatabase(info);
                Console.WriteLine(String.Format("=============================================== \n", genSection.SectionName));
                Console.WriteLine(String.Format("done  \n", genSection.SectionName));
                Console.WriteLine(String.Format("=============================================== \n", genSection.SectionName));
            }

            Console.Write("Press any key to continue . . . ");
            Console.ReadKey(true);
        }
    }
}