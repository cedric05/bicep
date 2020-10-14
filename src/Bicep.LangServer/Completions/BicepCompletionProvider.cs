// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Bicep.Core;
using Bicep.Core.Extensions;
using Bicep.Core.SemanticModel;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Bicep.LanguageServer.Completions
{
    public class BicepCompletionProvider : ICompletionProvider
    {
        public IEnumerable<CompletionItem> GetFilteredCompletions(SemanticModel model, BicepCompletionContext context)
        {
            return GetDeclarationCompletions(context)
                .Concat(GetSymbolCompletions(model, context))
                .Concat(GetDeclarationTypeCompletions(context))
                .Concat(GetObjectPropertyNameCompletions(model, context))
                .Concat(GetObjectPropertyValueCompletions(model, context));
        }

        private IEnumerable<CompletionItem> GetDeclarationCompletions(BicepCompletionContext completionContext)
        {
            if (completionContext.Kind.HasFlag(BicepCompletionContextKind.DeclarationStart))
            {
                yield return CompletionItemFactory.CreateKeywordCompletion(LanguageConstants.ParameterKeyword, "Parameter keyword");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ParameterKeyword, "Parameter declaration", "param ${1:Identifier} ${2:Type}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ParameterKeyword, "Parameter declaration with default value", "param ${1:Identifier} ${2:Type} = ${3:DefaultValue}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ParameterKeyword, "Parameter declaration with default and allowed values", @"param ${1:Identifier} ${2:Type} {
  default: $3
  allowed: [
    $4
  ]
}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ParameterKeyword, "Parameter declaration with options", @"param ${1:Identifier} ${2:Type} {
  $0
}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ParameterKeyword, "Secure string parameter", @"param ${1:Identifier} string {
  secure: true
}");

                yield return CompletionItemFactory.CreateKeywordCompletion(LanguageConstants.VariableKeyword, "Variable keyword");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.VariableKeyword, "Variable declaration", "var ${1:Identifier} = $0");

                yield return CompletionItemFactory.CreateKeywordCompletion(LanguageConstants.ResourceKeyword, "Resource keyword");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ResourceKeyword, "Resource with defaults", @"resource ${1:Identifier} 'Microsoft.${2:Provider}/${3:Type}@${4:Version}' = {
  name: $5
  location: $6
  properties: {
    $0
  }
}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ResourceKeyword, "Child Resource with defaults", @"resource ${1:Identifier} 'Microsoft.${2:Provider}/${3:ParentType}/${4:ChildType}@${5:Version}' = {
  name: $6
  properties: {
    $0
  }
}");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ResourceKeyword, "Resource without defaults", @"resource ${1:Identifier} 'Microsoft.${2:Provider}/${3:Type}@${4:Version}' = {
  name: $5
  $0
}
");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.ResourceKeyword, "Child Resource without defaults", @"resource ${1:Identifier} 'Microsoft.${2:Provider}/${3:ParentType}/${4:ChildType}@${5:Version}' = {
  name: $6
  $0
}");

                yield return CompletionItemFactory.CreateKeywordCompletion(LanguageConstants.OutputKeyword, "Output keyword");
                yield return CompletionItemFactory.CreateSnippetCompletion(LanguageConstants.OutputKeyword, "Output declaration", "output ${1:Identifier} ${2:Type} = $0");
            }
        }

        private IEnumerable<CompletionItem> GetSymbolCompletions(SemanticModel model, BicepCompletionContext completionContext) =>
            completionContext.Kind == BicepCompletionContextKind.None
                ? GetAccessibleSymbols(model).Select(sym => sym.ToCompletionItem())
                : Enumerable.Empty<CompletionItem>();

        private IEnumerable<CompletionItem> GetDeclarationTypeCompletions(BicepCompletionContext completionContext)
        {
            // local function
            IEnumerable<CompletionItem> GetPrimitiveTypeCompletions() =>
                LanguageConstants.DeclarationTypes.Values.Select(CompletionItemFactory.CreateTypeCompletion);


            if (completionContext.Kind.HasFlag(BicepCompletionContextKind.ParameterType))
            {
                return GetPrimitiveTypeCompletions().Concat(GetParameterTypeSnippets());
            }

            if (completionContext.Kind.HasFlag(BicepCompletionContextKind.OutputType))
            {
                return GetPrimitiveTypeCompletions();
            }

            return Enumerable.Empty<CompletionItem>();
        }


        private static IEnumerable<CompletionItem> GetParameterTypeSnippets()
        {
            yield return CompletionItemFactory.CreateSnippetCompletion("secureObject", "Secure object", @"object {
  secure: true
}");

            yield return CompletionItemFactory.CreateSnippetCompletion("secureString", "Secure string", @"string {
  secure: true
}");
        }

        private static IEnumerable<Symbol> GetAccessibleSymbols(SemanticModel model)
        {
            var accessibleSymbols = new Dictionary<string, Symbol>();

            // local function
            void AddAccessibleSymbols(IDictionary<string, Symbol> result, IEnumerable<Symbol> symbols)
            {
                foreach (var declaration in symbols)
                {
                    if (result.ContainsKey(declaration.Name) == false)
                    {
                        result.Add(declaration.Name, declaration);
                    }
                }
            }

            AddAccessibleSymbols(accessibleSymbols, model.Root.AllDeclarations
                .Where(decl => decl.NameSyntax.IsValid && !(decl is OutputSymbol)));

            AddAccessibleSymbols(accessibleSymbols, model.Root.ImportedNamespaces
                .SelectMany(kvp => kvp.Value.Descendants.OfType<FunctionSymbol>()));
            return accessibleSymbols.Values;
        }

        private IEnumerable<CompletionItem> GetObjectPropertyNameCompletions(SemanticModel model, BicepCompletionContext context)
        {
            if (context.Kind.HasFlag(BicepCompletionContextKind.PropertyName) == false || context.Object == null)
            {
                return Enumerable.Empty<CompletionItem>();
            }

            // in order to provide completions for property names,
            // we need to establish the type of the object first
            var declaredType = model.GetDeclaredType(context.Object);
            if (declaredType == null)
            {
                return Enumerable.Empty<CompletionItem>();
            }

            var specifiedPropertyNames = context.Object.ToKnownPropertyNames();

            // exclude read-only properties as they can't be set
            // exclude properties whose name has been specified in the object already
            return GetProperties(declaredType)
                .Where(p => p.Flags.HasFlag(TypePropertyFlags.ReadOnly) == false && specifiedPropertyNames.Contains(p.Name) == false)
                .Select(CompletionItemFactory.CreatePropertyCompletion);
        }

        private static IEnumerable<TypeProperty> GetProperties(TypeSymbol type)
        {
            switch (type)
            {
                case ObjectType objectType:
                    return objectType.Properties.Values;

                case DiscriminatedObjectType discriminated:
                    return discriminated.DiscriminatorProperty.AsEnumerable();

                default:
                    return Enumerable.Empty<TypeProperty>();
            }
        }

        private IEnumerable<CompletionItem> GetObjectPropertyValueCompletions(SemanticModel model, BicepCompletionContext context)
        {
            if (context.Kind.HasFlag(BicepCompletionContextKind.PropertyValue) == false || context.Property == null)
            {
                return Enumerable.Empty<CompletionItem>();
            }

            var declaredType = model.GetDeclaredType(context.Property);
            return GetPropertyValueCompletions(declaredType);
        }

        private static IEnumerable<CompletionItem> GetPropertyValueCompletions(TypeSymbol? propertyType)
        {
            switch (propertyType)
            {
                case PrimitiveType primitive:
                default:
                    return Enumerable.Empty<CompletionItem>();
            }
        }
    }
}