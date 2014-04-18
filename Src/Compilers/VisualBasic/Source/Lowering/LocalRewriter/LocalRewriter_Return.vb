﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend NotInheritable Class LocalRewriter
        Public Overrides Function VisitReturnStatement(node As BoundReturnStatement) As BoundNode
            Dim rewritten = RewriteReturnStatement(node)

            If ShouldGenerateUnstructuredExceptionHandlingResumeCode(node) Then
                rewritten = RegisterUnstructuredExceptionHandlingResumeTarget(node.Syntax, rewritten, canThrow:=node.ExpressionOpt IsNot Nothing)
            End If

            Return MarkStatementWithSequencePoint(rewritten)
        End Function

        ''' <summary>
        ''' Rewrites Return as a GoTo is needed (if not the last statement in a method)
        ''' </summary>
        Private Function RewriteReturnStatement(node As BoundReturnStatement) As BoundStatement
            node = DirectCast(MyBase.VisitReturnStatement(node), BoundReturnStatement)

            If inExpressionLambda Then
                ' In expression tree lambdas, we just want to translate a direct return, not a jump.
                ' Remove function local system and label to indicate a direct return.
                node = node.Update(node.ExpressionOpt, Nothing, Nothing)
            ElseIf Not node.IsEndOfMethodReturn Then

                If node.ExpressionOpt IsNot Nothing Then

                    Debug.Assert(node.FunctionLocalOpt IsNot Nothing)

                    Dim functionLocal = node.FunctionLocalOpt

                    If functionLocal IsNot Nothing Then

                        If currentMethodOrLambda.IsAsync Then
                            ' For Async method bodies we don't rewrite Return statements into GoTo's to the method's 
                            ' epilogue in AsyncRewriter, but rather rewrite them to proper jumps to the exit label of 
                            ' MoveNext() method of the generated state machine; we keep the node unmodified to be 
                            ' properly handled by AsyncRewriter; note that this also ensures 'IsEndOfMethodReturn' 
                            ' function works fine on BoundReturnStatement
                            Return node
                        End If

                        ' This is a return in a function.  Rewrite is as
                        '   returnValue = expr
                        '   jump exitlabel
                        '
                        Dim boundFunctionLocal = New BoundLocal(node.Syntax, functionLocal, functionLocal.Type)

                        Dim syntaxNode As VisualBasicSyntaxNode = node.Syntax

                        Dim assignment As BoundStatement = New BoundExpressionStatement(
                                                                syntaxNode,
                                                                New BoundAssignmentOperator(
                                                                    syntaxNode,
                                                                    boundFunctionLocal,
                                                                    node.ExpressionOpt,
                                                                    suppressObjectClone:=True,
                                                                    type:=functionLocal.Type
                                                                )
                                                            )
                        Dim jump As BoundStatement = New BoundGotoStatement(syntaxNode, node.ExitLabelOpt, Nothing)
                        Return New BoundStatementList(syntaxNode, ImmutableArray.Create(assignment, jump))
                    End If
                Else
                    Debug.Assert(node.FunctionLocalOpt Is Nothing)

                    ' This is a return in a sub. Rewrite as 
                    ' jump exitlabel
                    Return New BoundGotoStatement(node.Syntax, node.ExitLabelOpt, Nothing)
                End If

            ElseIf Me.currentMethodOrLambda.IsAsync AndAlso (Me.Flags And RewritingFlags.AllowEndOfMethodReturnWithExpression) = 0 Then

                ' This is a synthesized end-of-method return, in case it is inside Async method/lambda it needs
                ' to be rewritten so it does not return any value. Reasoning: all Return statements will be 
                ' rewritten into GoTo to the value return label of the MoveNext() method rather than exit label 
                ' of THIS method, so this return is only reachable for the code that falls through the block; 
                ' in which case the function is supposed to return the default value of the return type, wich is 
                ' exaclty what will happen in this case;
                '
                ' Also note that Async methods are lowered twice and this handling is only to be done as the 
                ' first pass; which is guarded by RewritingFlags.AllowEndOfMethodReturnWithExpression flag
                node = node.Update(Nothing, Nothing, Nothing)
            End If

            ' This is the return which is the last statement in a Sub node or a return from a function . There is no need to rewrite it
            ' There must not be a label symbol or a function local symbol.
            Debug.Assert(node.ExitLabelOpt Is Nothing)
            Debug.Assert(node.FunctionLocalOpt Is Nothing)
            Return node

        End Function
    End Class
End Namespace
