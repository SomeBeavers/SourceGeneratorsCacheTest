using Microsoft.CodeAnalysis;

namespace SourceGen
{
    [Generator]
    public class TestDataGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor Warning = new DiagnosticDescriptor(
            id: "TestDataGeneratorWARNING",
            title: "SG warning",
            messageFormat: "SG warning {0}.",
            category: "TestDataGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        public void Initialize(GeneratorInitializationContext context)
        {
            // Commenting out this line make it resolve
            context.RegisterForSyntaxNotifications(() => new Pizza());
        }

        public void Execute(GeneratorExecutionContext context)
        {
          var noError = context.Compilation.GetTypeByMetadataName("NoError");
          var warn = context.Compilation.GetTypeByMetadataName("Warn");
          var warn2 = context.Compilation.GetTypeByMetadataName("Warn2");
          var xamlCs = context.Compilation.GetTypeByMetadataName("XamlCs");

          if (noError == null)
          {
            context.AddSource(
              "Pizza.cs",
              "namespace TestNS" +
              "{" +
              "public class TestCls" +
              "{" +
              "public static string TestMethod()" +
              "{" +
              "System.Console.WriteLine(\"Generator\");" +
              "}" +
              "\r\n" +
              "#if NETCOREAPP3_1\r\npublic class CoreClass\r\n{\r\n    private int Test()\r\n    {\r\n    }\r\n}\r\n#endif"+
              "}" +
              "}"
            );
          }
          else if (warn != null)
          {
              //context.ReportDiagnostic(Diagnostic.Create(Warning, Location.None, 1));
                context.AddSource(
              "Pizza.cs",
              "namespace TestNS\r\n" +
              "{\r\n" +
              "public class TestCls\r\n" +
              "{\r\n" +
              "private int unused;\r\n" +
              "private string? unused2;\r\n" +
              "public static void TestMethod()\r\n" +
              "{System.Console.WriteLine(\"Generator\");" +
              "}" +
              "\r\n" +
              "#if NETCOREAPP3_1\r\npublic class CoreClass\r\n{\r\n    private int unused;\r\n    private void Test()\r\n    {\r\n    }\r\n}\r\n#endif"+
              "\r\n" +
              "}" +
              "}"
            );
          }
          else if (warn2 != null)
          {
              //context.ReportDiagnostic(Diagnostic.Create(Warning, Location.None, 1));
                context.AddSource(
              "Pizza.cs",
              "namespace TestNS\r\n" +
              "{\r\n" +
              "public class TestCls\r\n" +
              "{\r\n" +
              "private int unused;\r\n" +
              "private string? unused2;\r\n" +
              "public static void TestMethod()\r\n" +
              "{System.Console.WriteLine(\"Generator\");" +
              "}" +
              "\r\n" +
              "#if NET5_0\r\npublic class CoreFiveClass\r\n{\r\n    private int unused;\r\n    private void Test()\r\n    {\r\n    }\r\n}\r\n#endif" +
              "\r\n" +
              "}" +
              "}"
            );
          }
          else if (xamlCs != null)
          {
                context.AddSource(
              "Pizza.cs",
              "namespace TestNS\r\n" +
              "{\r\n" +
              "public class TestCls\r\n" +
              "{\r\n" +
              "#if NotXaml\r\npublic string Prop1 { get; set; }\r\n#else" +
              "\r\npublic string Prop2 { get; set; }\r\n" +
              "#endif\r\n" +
              "private string? unused2;\r\n" +
              "public static void TestMethod()\r\n" +
              "{System.Console.WriteLine(\"Generator\");" +
              "}" +
              "\r\n" +
              "#if NotXaml\r\npublic class NotXamlClass\r\n{\r\n   public int Prop1 { get; set; } \r\n    private void Test()\r\n    {\r\n    }\r\n}\r\n#else" +
              "\r\npublic class XamlClass\r\n{\r\n    public int Prop2 { get; set; } \r\n    private void Test()\r\n    {\r\n    }\r\n}\r\n" +
              "#endif" +
              "\r\n" +
              "}" +
              "}"
            );
          }
          else
          {
            context.AddSource(
              "Pizza.cs",
              "namespace TestNS\r\n" +
              "{\r\n" +
              "public class TestCls\r\n" +
              "{\r\n" +
              "public static void TestMethod()\r\n" +
              "{System.Console.WriteLine(\"Generator\");}}}"
            );
          }
        }
    }

    public class Pizza : ISyntaxContextReceiver
    {
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
        }
    }
}