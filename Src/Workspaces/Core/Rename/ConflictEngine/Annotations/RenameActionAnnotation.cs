﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    /// <summary>
    /// This annotation will be used by rename to mark all places where it needs to rename an identifier (token replacement) and where to 
    /// check if the semantics have been changes (conflict detection).
    /// </summary>
    /// <remarks>This annotation should be put on tokens only.</remarks>
    internal class RenameActionAnnotation : RenameAnnotation
    {
        /// <summary>
        /// The span this token occupied in the original syntax tree. Can be used to show e.g. conflicts in the UI.
        /// </summary>
        public readonly TextSpan OriginalSpan;

        /// <summary>
        /// A flag indicating whether this is a location that needs to be renamed or just tracked for conflicts.
        /// </summary>
        public readonly bool IsRenameLocation;

        /// <summary>
        /// A flag indicating if this identifier represents an accessor. E.g. get_Foo (of property Foo).
        /// </summary>
        public readonly bool IsAccessorLocation;

        /// <summary>
        /// A flag indicating whether the token at this location has the same ValueText then the original name 
        /// of the symbol that gets renamed.
        /// </summary>
        public readonly bool IsOriginalTextLocation;

        /// <summary>
        /// When replacing the annotated token this string will be appended to the token's value. This is used when renaming compiler 
        /// generated types whose names are derived from user given names (e.g. "XEventHandler" for event "X").
        /// </summary>
        public readonly string Suffix;

        /// <summary>
        /// A single dimensional array of annotations to verify after rename.
        /// </summary>
        public readonly RenameDeclarationLocationReference[] RenameDeclarationLocationReferences;

        /// <summary>
        /// States if this token is a Namespace Declaration Reference
        /// </summary>
        public readonly bool IsNamespaceDeclarationReference;

        /// <summary>
        /// States if this token is annotated as a part of the Invocation Expression that needs to be checked for the Conflicts
        /// </summary>
        public readonly bool IsInvocationExpression;

        public RenameActionAnnotation(
            TextSpan originalSpan,
            bool isRenameLocation,
            bool isAccessorLocation,
            string suffix,
            bool isOriginalTextLocation,
            RenameDeclarationLocationReference[] renameDeclarationLocations,
            bool isNamespaceDeclarationReference,
            bool isInvocationExpression)
        {
            this.OriginalSpan = originalSpan;
            this.IsRenameLocation = isRenameLocation;
            this.IsAccessorLocation = isAccessorLocation;
            this.Suffix = suffix;
            this.RenameDeclarationLocationReferences = renameDeclarationLocations;
            this.IsOriginalTextLocation = isOriginalTextLocation;
            this.IsNamespaceDeclarationReference = isNamespaceDeclarationReference;
            this.IsInvocationExpression = isInvocationExpression;
        }
    }
}
