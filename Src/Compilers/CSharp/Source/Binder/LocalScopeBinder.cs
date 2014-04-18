﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalScopeBinder : Binder
    {
        private ImmutableArray<LocalSymbol> locals;
        private ImmutableArray<LabelSymbol> labels;

        protected readonly MethodSymbol Owner;

        internal LocalScopeBinder(Binder next)
            : this(null, next)
        {
        }

        internal LocalScopeBinder(MethodSymbol owner, Binder next)
            : this(owner, next, next.Flags)
        {
        }

        internal LocalScopeBinder(MethodSymbol owner, Binder next, BinderFlags flags)
            : base(next, flags)
        {
            this.Owner = owner;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return this.Owner ?? Next.ContainingMemberOrLambda;
            }
        }

        internal sealed override ImmutableArray<LocalSymbol> Locals
        {
            get
            {
                if (this.locals.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.locals, BuildLocals(), default(ImmutableArray<LocalSymbol>));
                }

                return this.locals;
            }
        }

        protected virtual ImmutableArray<LocalSymbol> BuildLocals()
        {
            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal sealed override ImmutableArray<LabelSymbol> Labels
        {
            get
            {
                if (this.labels.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref this.labels, BuildLabels(), default(ImmutableArray<LabelSymbol>));
                }

                return this.labels;
            }
        }

        protected virtual ImmutableArray<LabelSymbol> BuildLabels()
        {
            return ImmutableArray<LabelSymbol>.Empty;
        }

        private SmallDictionary<string, LocalSymbol> lazyLocalsMap;
        private SmallDictionary<string, LocalSymbol> LocalsMap
        {
            get
            {
                if (this.lazyLocalsMap == null && this.Locals.Length > 0)
                {
                    this.lazyLocalsMap = BuildMap(this.Locals);
                }

                return this.lazyLocalsMap;
            }
        }

        private SmallDictionary<string, LabelSymbol> lazyLabelsMap;
        private SmallDictionary<string, LabelSymbol> LabelsMap
        {
            get
            {
                if (this.lazyLabelsMap == null && this.Labels.Length > 0)
                {
                    this.lazyLabelsMap = BuildMap(this.Labels);
                }

                return this.lazyLabelsMap;
            }
        }

        private static SmallDictionary<string, TSymbol> BuildMap<TSymbol>(ImmutableArray<TSymbol> array)
            where TSymbol : Symbol
        {
            Debug.Assert(array.Length > 0);

            var map = new SmallDictionary<string, TSymbol>();

            // NOTE: in a rare case of having two symbols with same name the one closer to the array's start wins.
            for (int i = array.Length - 1; i >= 0; i--)
            {
                var symbol = array[i];
                map[symbol.Name] = symbol;
            }

            return map;
        }

        protected ImmutableArray<LocalSymbol> BuildLocals(SyntaxList<StatementSyntax> statements)
        {
            ArrayBuilder<LocalSymbol> locals = null;
            foreach (var statement in statements)
            {
                var innerStatement = statement;

                // drill into any LabeledStatements -- atomic LabelStatements have been bound into
                // wrapped LabeledStatements by this point
                while (innerStatement.Kind == SyntaxKind.LabeledStatement)
                {
                    innerStatement = ((LabeledStatementSyntax)innerStatement).Statement;
                }

                if (innerStatement.Kind == SyntaxKind.LocalDeclarationStatement)
                {
                    var decl = (LocalDeclarationStatementSyntax)innerStatement;
                    if (locals == null)
                    {
                        locals = ArrayBuilder<LocalSymbol>.GetInstance();
                    }

                    foreach (var vdecl in decl.Declaration.Variables)
                    {
                        var localSymbol = SourceLocalSymbol.MakeLocal(
                            this.Owner,
                            this,
                            decl.Declaration.Type,
                            vdecl.Identifier,
                            vdecl.Initializer,
                            decl.IsConst ? LocalDeclarationKind.Constant
                                         : decl.IsFixed ? LocalDeclarationKind.Fixed
                                                        : LocalDeclarationKind.Variable);
                        locals.Add(localSymbol);
                    }
                }
            }

            if (locals != null)
            {
                return locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        protected void BuildLabels(SyntaxList<StatementSyntax> statements, ref ArrayBuilder<LabelSymbol> labels)
        {
            foreach (var statement in statements)
            {
                var stmt = statement;
                while (stmt.Kind == SyntaxKind.LabeledStatement)
                {
                    var labeledStatement = (LabeledStatementSyntax)stmt;
                    if (labels == null)
                    {
                        labels = ArrayBuilder<LabelSymbol>.GetInstance();
                    }

                    var labelSymbol = new SourceLabelSymbol(this.Owner, labeledStatement.Identifier);
                    labels.Add(labelSymbol);
                    stmt = labeledStatement.Statement;
                }
            }
        }

        /// <summary>
        /// Call this when you are sure there is a local declaration on this token.  Returns the local.
        /// </summary>
        protected override SourceLocalSymbol LookupLocal(SyntaxToken nameToken)
        {
            LocalSymbol result = null;
            if (LocalsMap != null && LocalsMap.TryGetValue(nameToken.ValueText, out result))
            {
                if (result.IdentifierToken == nameToken) return (SourceLocalSymbol)result;

                // in error cases we might have more than one declaration of the same name in the same scope
                foreach (var local in this.Locals)
                {
                    if (local.IdentifierToken == nameToken)
                    {
                        return (SourceLocalSymbol)local;
                    }
                }
            }

            return base.LookupLocal(nameToken);
        }

        protected override void LookupSymbolsInSingleBinder(
            LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(options.AreValid());
            Debug.Assert(result.IsClear);

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                var labelsMap = this.LabelsMap;
                if (labelsMap != null)
                {
                    LabelSymbol labelSymbol;
                    if (labelsMap.TryGetValue(name, out labelSymbol))
                    {
                        result.MergeEqual(LookupResult.Good(labelSymbol));
                    }
                }
                return;
            }

            var localsMap = this.LocalsMap;
            if (localsMap != null && (options & LookupOptions.NamespaceAliasesOnly) == 0)
            {
                LocalSymbol localSymbol;
                if (localsMap.TryGetValue(name, out localSymbol))
                {
                    result.MergeEqual(originalBinder.CheckViability(localSymbol, arity, options, null, diagnose, ref useSiteDiagnostics, basesBeingResolved));
                }
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            Debug.Assert(options.AreValid());

            if ((options & LookupOptions.LabelsOnly) != 0)
            {
                if (this.LabelsMap != null)
                {
                    foreach (var label in this.LabelsMap)
                    {
                        result.AddSymbol(label.Value, label.Key, 0);
                    }
                }
            }
            else if (this.LocalsMap != null && options.CanConsiderLocals())
            {
                foreach (var local in this.LocalsMap)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(local.Value, options, null))
                    {
                        result.AddSymbol(local.Value, local.Key, 0);
                    }
                }
            }
        }

        private bool ReportConflictWithLocal(LocalSymbol local, Symbol newSymbol, string name, Location newLocation, DiagnosticBag diagnostics)
        {
            // Quirk of the way we represent lambda parameters.                
            SymbolKind newSymbolKind = (object)newSymbol == null ? SymbolKind.Parameter : newSymbol.Kind;

            if (newSymbolKind == SymbolKind.ErrorType) return true;

            if (newSymbolKind == SymbolKind.Local)
            {
                if (this.Locals.Contains((LocalSymbol)newSymbol) && newLocation.SourceSpan.Start >= local.Locations[0].SourceSpan.Start)
                {
                    // A local variable named '{0}' is already defined in this scope
                    diagnostics.Add(ErrorCode.ERR_LocalDuplicate, newLocation, name);
                    return true;
                }
            }

            if (newSymbolKind == SymbolKind.Local || newSymbolKind == SymbolKind.Parameter)
            { 
                // A local or parameter named '{0}' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                diagnostics.Add(ErrorCode.ERR_LocalIllegallyOverrides, newLocation, name);
                return true;
            }

            if (newSymbolKind == SymbolKind.RangeVariable)
            {
                // The range variable '{0}' conflicts with a previous declaration of '{0}'
                diagnostics.Add(ErrorCode.ERR_QueryRangeVariableOverrides, newLocation, name);
                return true;
            }

            Debug.Assert(false, "what else can be declared inside a local scope?");
            return false;
        }


        internal virtual bool EnsureSingleDefinition(Symbol symbol, string name, Location location, DiagnosticBag diagnostics)
        {
            LocalSymbol existingLocal;
            var map = this.LocalsMap;
            if (map != null && map.TryGetValue(name, out existingLocal))
            {
                if (symbol == existingLocal)
                {
                    // reference to same symbol, by far the most common case.
                    return false;
                }

                return ReportConflictWithLocal(existingLocal, symbol, name, location, diagnostics);
            }

            return false;
        }
    }
}