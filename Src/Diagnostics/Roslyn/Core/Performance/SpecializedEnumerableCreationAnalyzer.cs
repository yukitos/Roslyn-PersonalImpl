﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    // TODO(naslotto): This should be updated to follow the flow of array creation expressions
    // that are eventually converted to and leave a given method as IEnumerable<T> once we have
    // the ability to do more thorough data-flow analysis in diagnostic analyzers.
    public abstract class SpecializedEnumerableCreationAnalyzer : IDiagnosticAnalyzer, ICompilationStartedAnalyzer
    {
        internal const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
        internal const string LinqEnumerableMetadataName = "System.Linq.Enumerable";
        internal const string EmptyMethodName = "Empty";

        internal const string NameForExportAttribute = "NewArrayAnalyzer";

        internal static readonly DiagnosticDescriptor UseEmptyEnumerableRule = new DiagnosticDescriptor(
            "RS0001",
            RoslynDiagnosticsResources.UseEmptyEnumerableDescription,
            RoslynDiagnosticsResources.UseEmptyEnumerableMessage,
            "Performance",
            DiagnosticSeverity.Warning);

        internal static readonly DiagnosticDescriptor UseSingletonEnumerableRule = new DiagnosticDescriptor(
            "RS0002",
            RoslynDiagnosticsResources.UseSingletonEnumerableDescription,
            RoslynDiagnosticsResources.UseSingletonEnumerableMessage,
            "Performance",
            DiagnosticSeverity.Warning);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule); }
        }

        public ICompilationEndedAnalyzer OnCompilationStarted(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            var genericEnumerableSymbol = compilation.GetTypeByMetadataName(IEnumerableMetadataName);
            if (genericEnumerableSymbol == null)
            {
                return null;
            }

            var linqEnumerableSymbol = compilation.GetTypeByMetadataName(LinqEnumerableMetadataName);
            if (linqEnumerableSymbol == null)
            {
                return null;
            }

            var genericEmptyEnumerableSymbol = linqEnumerableSymbol.GetMembers(EmptyMethodName).FirstOrDefault() as IMethodSymbol;
            if (genericEmptyEnumerableSymbol == null ||
                genericEmptyEnumerableSymbol.Arity != 1 ||
                genericEmptyEnumerableSymbol.Parameters.Length != 0)
            {
                return null;
            }

            return GetCodeBlockStartedAnalyzer(genericEnumerableSymbol, genericEmptyEnumerableSymbol);
        }

        protected abstract AbstractCodeBlockStartedAnalyzer GetCodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol);

        protected abstract class AbstractCodeBlockStartedAnalyzer : ICodeBlockStartedAnalyzer, ICompilationEndedAnalyzer
        {
            private INamedTypeSymbol genericEnumerableSymbol;
            private IMethodSymbol genericEmptyEnumerableSymbol;

            public AbstractCodeBlockStartedAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                this.genericEnumerableSymbol = genericEnumerableSymbol;
                this.genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule); }
            }

            protected abstract AbstractSyntaxAnalyzer GetSyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol);

            public ICodeBlockEndedAnalyzer OnCodeBlockStarted(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                var methodSymbol = ownerSymbol as IMethodSymbol;
                if (methodSymbol != null &&
                    methodSymbol.ReturnType.OriginalDefinition == this.genericEnumerableSymbol)
                {
                    return GetSyntaxAnalyzer(this.genericEnumerableSymbol, this.genericEmptyEnumerableSymbol);
                }

                return null;
            }

            public void OnCompilationEnded(Compilation compilation, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
            }
        }

        protected abstract class AbstractSyntaxAnalyzer : ICodeBlockEndedAnalyzer
        {
            private INamedTypeSymbol genericEnumerableSymbol;
            private IMethodSymbol genericEmptyEnumerableSymbol;

            public AbstractSyntaxAnalyzer(INamedTypeSymbol genericEnumerableSymbol, IMethodSymbol genericEmptyEnumerableSymbol)
            {
                this.genericEnumerableSymbol = genericEnumerableSymbol;
                this.genericEmptyEnumerableSymbol = genericEmptyEnumerableSymbol;
            }

            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get { return ImmutableArray.Create(UseEmptyEnumerableRule, UseSingletonEnumerableRule); }
            }

            public void OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
            }

            protected bool ShouldAnalyzeArrayCreationExpression(SyntaxNode expression, SemanticModel semanticModel)
            {
                var typeInfo = semanticModel.GetTypeInfo(expression);
                var arrayType = typeInfo.Type as IArrayTypeSymbol;

                return typeInfo.ConvertedType != null &&
                    typeInfo.ConvertedType.OriginalDefinition == this.genericEnumerableSymbol &&
                    arrayType != null &&
                    arrayType.Rank == 1;
            }

            protected void AnalyzeMemberAccessName(SyntaxNode name, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic)
            {
                var methodSymbol = semanticModel.GetSymbolInfo(name).Symbol as IMethodSymbol;
                if (methodSymbol != null &&
                    methodSymbol.OriginalDefinition == this.genericEmptyEnumerableSymbol)
                {
                    addDiagnostic(Diagnostic.Create(UseEmptyEnumerableRule, name.Parent.GetLocation()));
                }
            }

            protected static void AnalyzeArrayLength(int length, SyntaxNode arrayCreationExpression, Action<Diagnostic> addDiagnostic)
            {
                if (length == 0)
                {
                    addDiagnostic(Diagnostic.Create(UseEmptyEnumerableRule, arrayCreationExpression.GetLocation()));
                }
                else if (length == 1)
                {
                    addDiagnostic(Diagnostic.Create(UseSingletonEnumerableRule, arrayCreationExpression.GetLocation()));
                }
            }
        }
    }
}
