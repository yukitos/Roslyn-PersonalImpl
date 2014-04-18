﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend MustInherit Class BoundExpression

        Public ReadOnly Property IsConstant As Boolean
            Get
                Return Me.ConstantValueOpt IsNot Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property ConstantValueOpt As ConstantValue
            Get
                Return Nothing
            End Get
        End Property

        Public Overridable ReadOnly Property ExpressionSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        ' Indicates any problems with lookup/symbol binding that should be reported 
        ' via GetSemanticInfo.
        Public Overridable ReadOnly Property ResultKind As LookupResultKind
            Get
                Return LookupResultKind.Good
            End Get
        End Property

        ''' <summary>
        ''' Does expression refer to a physical memory location that can be modified?
        ''' 
        ''' Note, Dev10 uses SXF_LVALUE flag on bound nodes to represent this concept.
        ''' </summary>
        Public Overridable ReadOnly Property IsLValue As Boolean
            Get
                Return False
            End Get
        End Property

        Public Function MakeRValue() As BoundExpression
            Return MakeRValueImpl()
        End Function

        Protected Overridable Function MakeRValueImpl() As BoundExpression
            Debug.Assert(Not IsLValue)
            Return Me
        End Function

#If DEBUG Then
        Protected Sub ValidateConstantValue()
            ValidateConstantValue(Me.Type, Me.ConstantValueOpt)
        End Sub

        Protected Shared Sub ValidateConstantValue(type As TypeSymbol, constValue As ConstantValue)
            Debug.Assert(constValue Is Nothing OrElse
                         constValue.IsBad OrElse
                         type.IsValidForConstantValue(constValue))
        End Sub
#End If

    End Class

End Namespace