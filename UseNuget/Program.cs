using System;
using SameContentSameName;

namespace UseNuget
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public partial class A
    {
        [SameContentSameName] public string s = "";
    }

    
}

class NoError { }
