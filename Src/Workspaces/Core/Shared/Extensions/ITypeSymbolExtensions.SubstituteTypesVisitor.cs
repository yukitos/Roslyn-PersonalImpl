﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class SubstituteTypesVisitor<TType1, TType2> : SymbolVisitor<ITypeSymbol>
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            // private readonly Compilation compilation;
            private readonly IDictionary<TType1, TType2> map;
            private readonly ITypeGenerator typeGenerator;

            internal SubstituteTypesVisitor(
                IDictionary<TType1, TType2> map,
                ITypeGenerator typeGenerator)
            {
                this.map = map;
                this.typeGenerator = typeGenerator;
            }

            public override ITypeSymbol DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            private ITypeSymbol VisitType(ITypeSymbol symbol)
            {
                TType2 converted;
                if (symbol is TType1 && map.TryGetValue((TType1)symbol, out converted))
                {
                    return converted;
                }

                return symbol;
            }

            public override ITypeSymbol VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return symbol;
            }

            public override ITypeSymbol VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                return VisitType(symbol);
            }

            public override ITypeSymbol VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.IsAnonymousType)
                {
                    return symbol;
                }

                // If we don't even have any type arguments, then there's nothing to do.
                var allTypeArguments = symbol.GetAllTypeArguments().ToList();
                if (allTypeArguments.Count == 0)
                {
                    return symbol;
                }

                // If we have a containing type, make sure its type arguments are updated as well.
                var updatedContainingType = symbol.ContainingType == null
                    ? null
                    : symbol.ContainingType.Accept(this);

                // If our containing type changed, then find us again in the new containing type.
                if (updatedContainingType != symbol.ContainingType)
                {
                    symbol = updatedContainingType.GetTypeMembers(symbol.Name, symbol.Arity).First(m => m.TypeKind == symbol.TypeKind);
                }

                var substitutedArguments = symbol.TypeArguments.Select(t => t.Accept(this)).ToArray();
                if (symbol.TypeArguments.SequenceEqual(substitutedArguments))
                {
                    return symbol;
                }

                return typeGenerator.Construct(symbol.OriginalDefinition, substitutedArguments);
            }

            public override ITypeSymbol VisitArrayType(IArrayTypeSymbol symbol)
            {
                var elementType = symbol.ElementType.Accept(this);
                if (elementType != null && elementType.Equals(symbol.ElementType))
                {
                    return symbol;
                }

                return typeGenerator.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override ITypeSymbol VisitPointerType(IPointerTypeSymbol symbol)
            {
                var pointedAtType = symbol.PointedAtType.Accept(this);
                if (pointedAtType != null && pointedAtType.Equals(symbol.PointedAtType))
                {
                    return symbol;
                }

                return typeGenerator.CreatePointerTypeSymbol(pointedAtType);
            }
        }
    }
}