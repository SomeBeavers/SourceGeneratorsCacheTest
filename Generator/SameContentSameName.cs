using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator
{
    [Generator]
    public class SameContentSameName : ISourceGenerator
    {
        private const string attributeText = @"
using System;
namespace SameContentSameName
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class SameContentSameNameAttribute : Attribute
    {
        public SameContentSameNameAttribute()
        {
        }
        public string PropertyName { get; set; }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // add the attribute text
            context.AddSource("SameContentSameNameAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            CSharpParseOptions options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            Compilation compilation = context.Compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName("SameContentSameName.SameContentSameNameAttribute");
            INamedTypeSymbol notifySymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            // loop over the candidate fields, and keep the ones that are actually annotated
            List<IFieldSymbol> fieldSymbols = new List<IFieldSymbol>();
            foreach (FieldDeclarationSyntax field in receiver.CandidateFields)
            {
                SemanticModel model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    IFieldSymbol fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                    {
                        fieldSymbols.Add(fieldSymbol);
                    }
                }
            }

            // group the fields by class, and generate the source
            foreach (IGrouping<INamedTypeSymbol, IFieldSymbol> group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                string classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
                string classSource2 = ProcessClass2(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);

                Thread.Sleep(20000);

                context.AddSource($"{group.Key.Name}_SameContentSameName.cs", SourceText.From(classSource, Encoding.ASCII));
                context.AddSource($"{group.Key.Name}_SameContentSameName_Big.cs", SourceText.From(classSource2, Encoding.ASCII));
                //context.AddSource("AutoNotifyAttributeNew", SourceText.From(attributeTex2t, Encoding.UTF8));
            }
        }

                private string ProcessClass2(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

            //// if the class doesn't implement INotifyPropertyChanged already, add it
            //if (!classSymbol.Interfaces.Contains(notifySymbol))
            //{
            //    source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
            //    source.Append("public int fakeField;");
            //}

            for (int i = 0; i < 1000; i++)
            {
                source.Append($"public int fakeField{i};");
                source.Append($@"
public int FakeField{i}Prop
{{
    get 
    {{
        return this.fakeField{i};
        int fakeField1{i} = 1;
    }}

    set
    {{
        this.fakeField{i} = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(FakeField{i}Prop)));
    }}
}}

");
            }

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            source.Append("} }");
            return source.ToString();
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            source.Append($@"
public {fieldType} {propertyName} 
{{
    get 
    {{
        return this.{fieldName};
        int {fieldName}1 = 1;
    }}

    set
    {{
        this.{fieldName} = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof({propertyName})));
    }}
}}

");

            string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }

        }


        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

            // if the class doesn't implement INotifyPropertyChanged already, add it
            if (!classSymbol.Interfaces.Contains(notifySymbol))
            {
                source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");
                source.Append("public int fakeField;");
            }

            source.Append($@"
public int FakeFieldProp
{{
    get 
    {{
        return this.fakeField;
        int fakeField1 = 1;
    }}

    set
    {{
        this.fakeField = value;
        this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(FakeFieldProp)));
    }}
}}

");

            source.Append("} }");
            return source.ToString();
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new List<FieldDeclarationSyntax>();

            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    CandidateFields.Add(fieldDeclarationSyntax);
                }
            }
        }
    }
}
