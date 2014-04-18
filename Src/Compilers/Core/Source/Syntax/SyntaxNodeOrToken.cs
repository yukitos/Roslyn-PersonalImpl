﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // Note that we do not store the token directly, we just store enough information to reconstruct it.
    // This allows us to reuse nodeOrToken as a token's parent.
    /// <summary>
    /// A wrapper for either a syntax node (<see cref="T:Microsoft.CodeAnalysis.SyntaxNode"/>) or a syntax token (<see
    /// cref="T:Microsoft.CodeAnalysis.SyntaxToken"/>).
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public struct SyntaxNodeOrToken : IEquatable<SyntaxNodeOrToken>
    {
        // In a case if we are wrapping a SyntaxNode this is the SyntaxNode itself.
        // In a case where we are wrapping a token, this is the token's parent.
        private readonly SyntaxNode nodeOrParent;

        // Green node for the token. 
        private readonly GreenNode token;

        // Used in both node and token cases.
        // When we have a node, position == nodeOrParent.Position.
        private readonly int position;

        // Index of the token among parent's children. 
        // This field only makes sense if this is a token.
        // For regular nodes it is set to -1 to distinguish from default(SyntaxToken)
        private readonly int tokenIndex;

        internal SyntaxNodeOrToken(SyntaxNode node)
            : this()
        {
            if (node != null)
            {
                Debug.Assert(!node.Green.IsList, "node cannot be a list");
                this.position = node.Position;
                this.nodeOrParent = node;
            }

            tokenIndex = -1;
        }

        internal SyntaxNodeOrToken(SyntaxNode parent, GreenNode token, int position, int index)
        {
            Debug.Assert(parent == null || !parent.Green.IsList, "parent cannot be a list");
            Debug.Assert(token != null || (parent == null && position == 0 && index == 0), "parts must form a token");
            Debug.Assert(token == null || token.IsToken, "token must be a token");
            Debug.Assert(index >= 0, "index must not be negative");
            Debug.Assert(parent == null || token != null, "null token cannot have parent");

            this.position = position;
            this.tokenIndex = index;
            this.nodeOrParent = parent;
            this.token = token;
        }

        internal string GetDebuggerDisplay()
        {
            return GetType().Name + " " + KindText + " " + ToString();
        }

        private string KindText
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.KindText;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Green.KindText;
                }
                else
                {
                    return "None";
                }
            }
        }

        /// <summary>
        /// An integer representing the language specific kind of the underlying node or token.
        /// </summary>
        public int RawKind
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.RawKind;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.RawKind;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// The language name that this node or token is syntax of.
        /// </summary>
        public string Language
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.Language;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Language;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Determines whether the underlying node or token represents a language construct that was actually parsed
        /// from source code. Missing nodes and tokens are typically generated by the parser in error scenarios to
        /// represent constructs that should have been present in the source code for the source code to compile
        /// successfully but were actually missing.
        /// </summary>
        public bool IsMissing
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.IsMissing;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.IsMissing;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The node that contains the underlying node or token in its Children collection.
        /// </summary>
        public SyntaxNode Parent
        {
            get
            {
                if (this.token != null)
                {
                    return this.nodeOrParent;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Parent;
                }
                else
                {
                    return null;
                }
            }
        }

        internal GreenNode UnderlyingNode
        {
            get
            {
                if (this.token != null)
                {
                    return this.token;
                }
                else if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Green;
                }
                else
                {
                    return null;
                }
            }
        }

        internal int Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Determines whether this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a token.
        /// </summary>
        public bool IsToken
        {
            get
            {
                return !IsNode;
            }
        }

        /// <summary>
        /// Determines whether this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a node.
        /// </summary>
        public bool IsNode
        {
            get
            {
                return this.tokenIndex < 0;
            }
        }

        /// <summary>
        /// Returns the underlying token if this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a
        /// token.
        /// </summary>
        /// <returns>
        /// The underlying token if this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a token.
        /// </returns>
        public SyntaxToken AsToken()
        {
            if (this.token != null)
            {
                return new SyntaxToken(this.nodeOrParent, this.token, this.Position, this.tokenIndex);
            }

            return default(SyntaxToken);
        }

        /// <summary>
        /// Returns the underlying node if this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a
        /// node.
        /// </summary>
        /// <returns>
        /// The underlying node if this <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is wrapping a node.
        /// </returns>
        public SyntaxNode AsNode()
        {
            if (this.token != null)
            {
                return null;
            }

            return this.nodeOrParent;
        }

        /// <summary>
        /// The list of child nodes and tokens of the underlying node or token.
        /// </summary>
        public ChildSyntaxList ChildNodesAndTokens()
        {
            return this.IsToken
                ? default(ChildSyntaxList)
                : this.nodeOrParent.ChildNodesAndTokens();
        }

        /// <summary>
        /// The absolute span of the underlying node or token in characters, not including its leading and trailing
        /// trivia.
        /// </summary>
        public TextSpan Span
        {
            get
            {
                if (this.token != null)
                {
                    return this.AsToken().Span;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Span;
                }

                return default(TextSpan);
            }
        }

        /// <summary>
        /// Same as accessing <see cref="TextSpan.Start"/> on <see cref="Span"/>.
        /// </summary>
        /// <remarks>
        /// Slight performance improvement.
        /// </remarks>
        public int SpanStart
        {
            get
            {
                if (this.token != null)
                {
                    // PERF: Inlined "this.AsToken().SpanStart"
                    return this.position + this.token.GetLeadingTriviaWidth();
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.SpanStart;
                }

                return 0; //default(TextSpan).Start
            }
        }

        /// <summary>
        /// The absolute span of the underlying node or token in characters, including its leading and trailing trivia.
        /// </summary>
        public TextSpan FullSpan
        {
            get
            {
                if (this.token != null)
                {
                    return new TextSpan(Position, this.token.FullWidth);
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.FullSpan;
                }

                return default(TextSpan);
            }
        }

        /// <summary>
        /// Returns the string representation of this node or token, not including its leading and trailing
        /// trivia.
        /// </summary>
        /// <returns>The string representation of this node or token, not including its leading and trailing
        /// trivia.</returns>
        /// <remarks>The length of the returned string is always the same as Span.Length</remarks>
        public override string ToString()
        {
            if (this.token != null)
            {
                return this.token.ToString();
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the full string representation of this node or token including its leading and trailing trivia.
        /// </summary>
        /// <returns>The full string representation of this node or token including its leading and trailing
        /// trivia.</returns>
        /// <remarks>The length of the returned string is always the same as FullSpan.Length</remarks>
        public string ToFullString()
        {
            if (this.token != null)
            {
                return this.token.ToFullString();
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.ToFullString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Writes the full text of this node or token to the specified TextWriter.
        /// </summary>
        public void WriteTo(System.IO.TextWriter writer)
        {
            if (this.token != null)
            {
                this.token.WriteTo(writer);
            }
            else if (this.nodeOrParent != null)
            {
                this.nodeOrParent.WriteTo(writer);
            }
        }

        /// <summary>
        /// Determines whether the underlying node or token has any leading trivia.
        /// </summary>
        public bool HasLeadingTrivia
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.HasLeadingTrivia;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.HasLeadingTrivia;
                }

                return false;
            }
        }

        /// <summary>
        /// The list of trivia that appear before the underlying node or token in the source code and are attached to a
        /// token that is a descendant of the underlying node or token.
        /// </summary>
        public SyntaxTriviaList GetLeadingTrivia()
        {
            if (this.token != null)
            {
                return this.AsToken().LeadingTrivia;
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetLeadingTrivia();
            }

            return default(SyntaxTriviaList);
        }

        /// <summary>
        /// Determines whether the underlying node or token has any trailing trivia.
        /// </summary>
        public bool HasTrailingTrivia
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.HasTrailingTrivia;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.HasTrailingTrivia;
                }

                return false;
            }
        }

        /// <summary>
        /// The list of trivia that appear after the underlying node or token in the source code and are attached to a
        /// token that is a descendant of the underlying node or token.
        /// </summary>
        public SyntaxTriviaList GetTrailingTrivia()
        {
            if (this.token != null)
            {
                return this.AsToken().TrailingTrivia;
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetTrailingTrivia();
            }

            return default(SyntaxTriviaList);
        }

        public SyntaxNodeOrToken WithLeadingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            if (this.token != null)
            {
                return (SyntaxNodeOrToken)AsToken().WithLeadingTrivia(trivia);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.WithLeadingTrivia(trivia);
            }

            return this;
        }

        public SyntaxNodeOrToken WithLeadingTrivia(params SyntaxTrivia[] trivia)
        {
            return WithLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public SyntaxNodeOrToken WithTrailingTrivia(IEnumerable<SyntaxTrivia> trivia)
        {
            if (this.token != null)
            {
                return (SyntaxNodeOrToken)AsToken().WithTrailingTrivia(trivia);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.WithTrailingTrivia(trivia);
            }

            return this;
        }

        public SyntaxNodeOrToken WithTrailingTrivia(params SyntaxTrivia[] trivia)
        {
            return WithTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        /// <summary>
        /// Determines whether the underlying node or token or any of its descendant nodes, tokens or trivia have any
        /// diagnostics on them. 
        /// </summary>
        public bool ContainsDiagnostics
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.ContainsDiagnostics;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.ContainsDiagnostics;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a list of all the diagnostics in either the sub tree that has this node as its root or
        /// associated with this token and its related trivia. 
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public IEnumerable<Diagnostic> GetDiagnostics()
        {
            if (this.token != null)
            {
                return this.AsToken().GetDiagnostics();
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetDiagnostics();
            }

            return SpecializedCollections.EmptyEnumerable<Diagnostic>();
        }


        /// <summary>
        /// Determines whether the underlying node or token has any descendant preprocessor directives.
        /// </summary>
        public bool ContainsDirectives
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.ContainsDirectives;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.ContainsDirectives;
                }

                return false;
            }
        }

        #region Annotations 
        /// <summary>
        /// Determines whether this node or token (or any sub node, token or trivia) as annotations.
        /// </summary>
        public bool ContainsAnnotations
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.ContainsAnnotations;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.ContainsAnnotations;
                }

                return false;
            }
        }

        /// <summary>
        /// Determines whether this node or token has annotations of the specified kind.
        /// </summary>
        public bool HasAnnotations(string annotationKind)
        {
            if (this.token != null)
            {
                return this.token.HasAnnotations(annotationKind);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.HasAnnotations(annotationKind);
            }

            return false;
        }

        /// <summary>
        /// Determines whether this node or token has annotations of the specified kind.
        /// </summary>
        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            if (this.token != null)
            {
                return this.token.HasAnnotations(annotationKinds);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.HasAnnotations(annotationKinds);
            }

            return false;
        }

        /// <summary>
        /// Determines if this node or token has the specific annotation.
        /// </summary>
        public bool HasAnnotation(SyntaxAnnotation annotation)
        {
            if (this.token != null)
            {
                return this.token.HasAnnotation(annotation);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.HasAnnotation(annotation);
            }

            return false;
        }

        /// <summary>
        /// Gets all annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            if (this.token != null)
            {
                return this.token.GetAnnotations(annotationKind);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetAnnotations(annotationKind);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Gets all annotations of the specified annotation kind.
        /// </summary>
        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            if (this.token != null)
            {
                return this.token.GetAnnotations(annotationKinds);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetAnnotations(annotationKinds);
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();
        }

        /// <summary>
        /// Creates a new node or token identical to this one with the specified annotations.
        /// </summary>
        public SyntaxNodeOrToken WithAdditionalAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithAdditionalAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Creates a new node or token identical to this one with the specified annotations.
        /// </summary>
        public SyntaxNodeOrToken WithAdditionalAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.token != null)
            {
                return this.AsToken().WithAdditionalAnnotations(annotations);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.WithAdditionalAnnotations(annotations);
            }

            return this;
        }

        /// <summary>
        /// Creates a new node or token identical to this one without the specified annotations.
        /// </summary>
        public SyntaxNodeOrToken WithoutAnnotations(params SyntaxAnnotation[] annotations)
        {
            return WithoutAnnotations((IEnumerable<SyntaxAnnotation>)annotations);
        }

        /// <summary>
        /// Creates a new node or token identical to this one without the specified annotations.
        /// </summary>
        public SyntaxNodeOrToken WithoutAnnotations(IEnumerable<SyntaxAnnotation> annotations)
        {
            if (annotations == null)
            {
                throw new ArgumentNullException("annotations");
            }

            if (this.token != null)
            {
                return this.AsToken().WithoutAnnotations(annotations);
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.WithoutAnnotations(annotations);
            }

            return this;
        }

        /// <summary>
        /// Creates a new node or token identical to this one without annotations of the specified kind.
        /// </summary>
        public SyntaxNodeOrToken WithoutAnnotations(string annotationKind)
        {
            if (annotationKind == null)
            {
                throw new ArgumentNullException("annotationKind");
            }

            if (this.HasAnnotations(annotationKind))
            {
                return this.WithoutAnnotations(this.GetAnnotations(annotationKind));
            }
            else
            {
                return this;
            }
        }
        #endregion

        /// <summary>
        /// Determines whether the supplied <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is equal to this
        /// <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </summary>
        public bool Equals(SyntaxNodeOrToken other)
        {
            // index replaces position to ensure equality.  Assert if offset affects equality.
            Debug.Assert(
                (this.nodeOrParent == other.nodeOrParent && this.token == other.token && this.position == other.position && this.tokenIndex == other.tokenIndex) ==
                (this.nodeOrParent == other.nodeOrParent && this.token == other.token && this.tokenIndex == other.tokenIndex));

            return this.nodeOrParent == other.nodeOrParent &&
                   this.token == other.token &&
                   this.tokenIndex == other.tokenIndex;
        }

        /// <summary>
        /// Determines whether two <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>s are equal.
        /// </summary>
        public static bool operator ==(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>s are unequal.
        /// </summary>
        public static bool operator !=(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether the supplied <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> is equal to this
        /// <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is SyntaxNodeOrToken && Equals((SyntaxNodeOrToken)obj);
        }

        /// <summary>
        /// Serves as hash function for <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </summary>
        public override int GetHashCode()
        {
            return Hash.Combine(this.nodeOrParent, Hash.Combine(this.token, this.tokenIndex));
        }

        /// <summary>
        /// Determines if the two nodes or tokens are equivalent.
        /// </summary>
        public bool IsEquivalentTo(SyntaxNodeOrToken other)
        {
            if (this.IsNode != other.IsNode)
            {
                return false;
            }

            var thisUnderlying = this.UnderlyingNode;
            var otherUnderlying = other.UnderlyingNode;

            return (thisUnderlying == otherUnderlying) || (thisUnderlying != null && thisUnderlying.IsEquivalentTo(otherUnderlying));
        }

        /// <summary>
        /// Returns a new <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> that wraps the supplied token.
        /// </summary>
        /// <param name="token">The input token.</param>
        /// <returns>
        /// A <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> that wraps the supplied token.
        /// </returns>
        public static implicit operator SyntaxNodeOrToken(SyntaxToken token)
        {
            return new SyntaxNodeOrToken(token.Parent, token.Node, token.Position, token.Index);
        }

        /// <summary>
        /// Returns the underlying token wrapped by the supplied <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </summary>
        /// <param name="nodeOrToken">
        /// The input <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </param>
        /// <returns>
        /// The underlying token wrapped by the supplied <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </returns>
        public static explicit operator SyntaxToken(SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.AsToken();
        }

        /// <summary>
        /// Returns a new <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> that wraps the supplied node.
        /// </summary>
        /// <param name="node">The input node.</param>
        /// <returns>
        /// A <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/> that wraps the supplied node.
        /// </returns>
        public static implicit operator SyntaxNodeOrToken(SyntaxNode node)
        {
            return new SyntaxNodeOrToken(node);
        }

        /// <summary>
        /// Returns the underlying node wrapped by the supplied <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </summary>
        /// <param name="nodeOrToken">
        /// The input <see cref="T:Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </param>
        /// <returns>
        /// The underlying node wrapped by the supplied <see cref="Microsoft.CodeAnalysis.SyntaxNodeOrToken"/>.
        /// </returns>
        public static explicit operator SyntaxNode(SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.AsNode();
        }

        /// <summary>
        /// SyntaxTree which contains current SyntaxNodeOrToken.
        /// </summary>
        public SyntaxTree SyntaxTree
        {
            get
            {
                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.SyntaxTree;
                }

                return null;
            }
        }

        /// <summary>
        /// Get the location of this node or token.
        /// </summary>
        public Location GetLocation()
        {
            if (this.token != null)
            {
                return this.AsToken().GetLocation();
            }

            if (this.nodeOrParent != null)
            {
                return this.nodeOrParent.GetLocation();
            }

            return null;
        }

        #region Directive Lookup

        // Get all directives under the node and its children in source code order.
        internal IList<TDirective> GetDirectives<TDirective>(Func<TDirective, bool> filter = null)
            where TDirective : SyntaxNode
        {
            List<TDirective> directives = null;
            GetDirectives(this, filter, ref directives);
            return directives ?? SpecializedCollections.EmptyList<TDirective>();
        }

        private static void GetDirectives<TDirective>(SyntaxNodeOrToken node, Func<TDirective, bool> filter, ref List<TDirective> directives)
            where TDirective : SyntaxNode
        {
            if (node.token != null)
            {
                GetDirectives(node.AsToken(), filter, ref directives);
            }
            else if (node.nodeOrParent != null)
            {
                GetDirectives(node.nodeOrParent, filter, ref directives);
            }
        }

        private static void GetDirectives<TDirective>(SyntaxNode node, Func<TDirective, bool> filter, ref List<TDirective> directives)
            where TDirective : SyntaxNode
        {
            if (node.ContainsDirectives)
            {
                foreach (var child in node.ChildNodesAndTokens())
                {
                    GetDirectives(child, filter, ref directives);
                }
            }
        }

        private static void GetDirectives<TDirective>(SyntaxToken token, Func<TDirective, bool> filter, ref List<TDirective> directives)
            where TDirective : SyntaxNode
        {
            if (token.ContainsDirectives)
            {
                GetDirectives(token.LeadingTrivia, filter, ref directives);
                GetDirectives(token.TrailingTrivia, filter, ref directives);
            }
        }

        private static void GetDirectives<TDirective>(SyntaxTriviaList trivia, Func<TDirective, bool> filter, ref List<TDirective> directives)
            where TDirective : SyntaxNode
        {
            foreach (var tr in trivia)
            {
                if (tr.IsDirective)
                {
                    var directive = tr.GetStructure() as TDirective;
                    if (directive != null && (filter == null || filter(directive)))
                    {
                        if (directives == null)
                        {
                            directives = new List<TDirective>();
                        }

                        directives.Add(directive);
                    }
                }
                else if (tr.HasStructure)
                {
                    GetDirectives(tr.GetStructure(), filter, ref directives);
                }
            }
        }

        #endregion

        internal int Width
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.Width;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.Width;
                }

                return 0;
            }
        }

        internal int FullWidth
        {
            get
            {
                if (this.token != null)
                {
                    return this.token.FullWidth;
                }

                if (this.nodeOrParent != null)
                {
                    return this.nodeOrParent.FullWidth;
                }

                return 0;
            }
        }

        internal int EndPosition
        {
            get
            {
                return this.position + this.FullWidth;
            }
        }

        public static int GetFirstChildIndexSpanningPosition(SyntaxNode node, int position)
        {
            if (!node.FullSpan.IntersectsWith(position))
            {
                throw new ArgumentException("Must be within node's FullSpan", "position");
            }

            return GetFirstChildIndexSpanningPosition(node.ChildNodesAndTokens(), position);
        }

        internal static int GetFirstChildIndexSpanningPosition(ChildSyntaxList list, int position)
        {
            int lo = 0;
            int hi = list.Count - 1;
            while (lo <= hi)
            {
                int r = lo + ((hi - lo) >> 1);

                var m = list[r];
                if (position < m.Position)
                {
                    hi = r - 1;
                }
                else
                {
                    if (position == m.Position)
                    {
                        // If we hit a zero width node, move left to the first such node (or the
                        // first one in the list)
                        for (; r > 0 && list[r - 1].FullWidth == 0; r--)
                        {
                            ;
                        }

                        return r;
                    }
                    else if (position >= m.EndPosition)
                    {
                        lo = r + 1;
                        continue;
                    }

                    return r;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        public SyntaxNodeOrToken GetNextSibling()
        {
            var parent = this.Parent;
            if (parent == null)
            {
                return default(SyntaxNodeOrToken);
            }

            var siblings = parent.ChildNodesAndTokens();

            return siblings.Count < 8
                ? GetNextSiblingFromStart(siblings)
                : GetNextSiblingWithSearch(siblings);
        }

        public SyntaxNodeOrToken GetPreviousSibling()
        {
            if (this.Parent != null)
            {
                // walk reverse in parent's child list until we find ourself 
                // and then return the next child
                var returnNext = false;
                foreach (var child in this.Parent.ChildNodesAndTokens().Reverse())
                {
                    if (returnNext)
                    {
                        return child;
                    }

                    if (child == this)
                    {
                        returnNext = true;
                    }
                }
            }

            return default(SyntaxNodeOrToken);
        }

        private SyntaxNodeOrToken GetNextSiblingFromStart(ChildSyntaxList siblings)
        {
            var returnNext = false;
            foreach (var sibling in siblings)
            {
                if (returnNext)
                {
                    return sibling;
                }

                if (sibling == this)
                {
                    returnNext = true;
                }
            }

            return default(SyntaxNodeOrToken);
        }

        private SyntaxNodeOrToken GetNextSiblingWithSearch(ChildSyntaxList siblings)
        {
            var firstIndex = SyntaxNodeOrToken.GetFirstChildIndexSpanningPosition(siblings, this.position);

            var count = siblings.Count;
            var returnNext = false;

            for (int i = firstIndex; i < count; i++)
            {
                if (returnNext)
                {
                    return siblings[i];
                }

                if (siblings[i] == this)
                {
                    returnNext = true;
                }
            }

            return default(SyntaxNodeOrToken);
        }
    }
}