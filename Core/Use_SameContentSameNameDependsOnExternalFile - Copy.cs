using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static System.Console;
using SameContentSameNameDependsOnExternalFile;

namespace GeneratedDemo
{
    class Use_SameContentSameNameDependsOnExternalFile
    {
        public static void Run()
        {
            WriteLine("## CARS");
            Cars.All.ToList().ForEach(c => WriteLine($"{c.Brand}\t{c.Model}\t{c.Year}\t{c.Cc}"));
            WriteLine("\n## PEOPLE");
            People.All.ToList().ForEach(p => WriteLine($"{p.Name}\t{p.Address}\t{p._11Age}"));
        }
    }
}