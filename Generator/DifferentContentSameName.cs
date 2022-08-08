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
    public class DifferentContentSameName : ISourceGenerator
    {
        private const string attributeText = @"
using System;
namespace DifferentContentSameName
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    sealed class DifferentContentSameNameAttribute : Attribute
    {
        public DifferentContentSameNameAttribute()
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
            context.AddSource("DifferentContentSameNameAttribute", SourceText.From(attributeText, Encoding.UTF8));

            // retreive the populated receiver 
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
                return;

            // we're going to create a new compilation that contains the attribute.
            // TODO: we should allow source generators to provide source during initialize, so that this step isn't required.
            var options = (context.Compilation as CSharpCompilation).SyntaxTrees[0].Options as CSharpParseOptions;
            var compilation =
                context.Compilation.AddSyntaxTrees(
                    CSharpSyntaxTree.ParseText(SourceText.From(attributeText, Encoding.UTF8), options));

            // get the newly bound attribute, and INotifyPropertyChanged
            var attributeSymbol =
                compilation.GetTypeByMetadataName("DifferentContentSameName.DifferentContentSameNameAttribute");
            var notifySymbol = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");

            // loop over the candidate fields, and keep the ones that are actually annotated
            var fieldSymbols = new List<IFieldSymbol>();
            foreach (var field in receiver.CandidateFields)
            {
                var model = compilation.GetSemanticModel(field.SyntaxTree);
                foreach (var variable in field.Declaration.Variables)
                {
                    // Get the symbol being decleared by the field, and keep it if its annotated
                    var fieldSymbol = model.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (fieldSymbol.GetAttributes().Any(ad =>
                            ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default)))
                        fieldSymbols.Add(fieldSymbol);
                }
            }

            // group the fields by class, and generate the source
            foreach (var group in fieldSymbols.GroupBy(f => f.ContainingType))
            {
                var classSource = ProcessClass(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);
                var classSource2 = ProcessClass2(group.Key, group.ToList(), attributeSymbol, notifySymbol, context);

                // timeout
                Thread.Sleep(20000);

                context.AddSource($"{group.Key.Name}_DifferentContentSameName.cs",
                    SourceText.From(classSource, Encoding.ASCII));

                // write file to disk
                context.AddSource($"{group.Key.Name}_DifferentContentSameName_Big.cs",
                    SourceText.From(classSource2, Encoding.ASCII));
            }
        }

        private string ProcessClass2(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol,
            ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return null; //TODO: issue a diagnostic that it must be top level

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            var source = new StringBuilder($@"
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

            for (var i = 0; i < 1000; i++)
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
            foreach (var fieldSymbol in fields) ProcessField2(source, fieldSymbol, attributeSymbol);

            source.Append("} }");
            return source.ToString();
        }

        private void ProcessField2(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            var fieldName = fieldSymbol.Name;
            var fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            var attributeData = fieldSymbol.GetAttributes().Single(ad =>
                ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            var propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
                //TODO: issue a diagnostic that we can't process this field
                return;

            source.Append($@"
public {fieldType} {propertyName}_Big 
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
                if (!overridenNameOpt.IsNull) return overridenNameOpt.Value.ToString();

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }
        }


        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol,
            ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
                return null; //TODO: issue a diagnostic that it must be top level

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            var source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} : {notifySymbol.ToDisplayString()}
    {{
");

            // if the class doesn't implement INotifyPropertyChanged already, add it
            if (!classSymbol.Interfaces.Contains(notifySymbol))
                source.Append("public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;");

            // create properties for each field 
            foreach (var fieldSymbol in fields) ProcessField(source, fieldSymbol, attributeSymbol);

            source.Append("} }");
            return source.ToString();
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            var fieldName = fieldSymbol.Name;
            var fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            var attributeData = fieldSymbol.GetAttributes().Single(ad =>
                ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            var overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            var propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
                //TODO: issue a diagnostic that we can't process this field
                return;

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
                if (!overridenNameOpt.IsNull) return overridenNameOpt.Value.ToString();

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }
        }

        /// <summary>
        ///     Created on demand before each generation pass
        /// </summary>
        private class SyntaxReceiver : ISyntaxReceiver
        {
            public List<FieldDeclarationSyntax> CandidateFields { get; } = new();

            /// <summary>
            ///     Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for
            ///     generation
            /// </summary>
            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // any field with at least one attribute is a candidate for property generation
                if (syntaxNode is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                    CandidateFields.Add(fieldDeclarationSyntax);
            }
        }
    }
}