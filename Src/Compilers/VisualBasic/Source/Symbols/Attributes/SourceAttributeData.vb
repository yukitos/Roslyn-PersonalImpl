﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class SourceAttributeData
        Inherits VisualBasicAttributeData

        Private ReadOnly m_AttributeClass As NamedTypeSymbol ' TODO - Remove attribute class. It is available from the constructor.
        Private ReadOnly m_AttributeConstructor As MethodSymbol
        Private ReadOnly m_ConstructorArguments As ImmutableArray(Of TypedConstant)
        Private ReadOnly m_NamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
        Private ReadOnly m_IsConditionallyOmitted As Boolean
        Private ReadOnly m_HasErrors As Boolean
        Private ReadOnly m_ApplicationNode As SyntaxReference

        Friend Sub New(ByVal applicationNode As SyntaxReference,
                       ByVal attrClass As NamedTypeSymbol,
                       ByVal attrMethod As MethodSymbol,
                       ByVal constructorArgs As ImmutableArray(Of TypedConstant),
                       ByVal namedArgs As ImmutableArray(Of KeyValuePair(Of String, TypedConstant)),
                       ByVal isConditionallyOmitted As Boolean,
                       ByVal hasErrors As Boolean)
            Me.m_ApplicationNode = applicationNode
            Me.m_AttributeClass = attrClass
            Me.m_AttributeConstructor = attrMethod
            Me.m_ConstructorArguments = If(constructorArgs.IsDefault, ImmutableArray(Of TypedConstant).Empty, constructorArgs)
            Me.m_NamedArguments = If(namedArgs.IsDefault, ImmutableArray.Create(Of KeyValuePair(Of String, TypedConstant))(), namedArgs)
            Me.m_IsConditionallyOmitted = isConditionallyOmitted
            Me.m_HasErrors = hasErrors
        End Sub

        Public Overrides ReadOnly Property AttributeClass As NamedTypeSymbol
            Get
                Return m_AttributeClass
            End Get
        End Property

        Public Overrides ReadOnly Property AttributeConstructor As MethodSymbol
            Get
                Return m_AttributeConstructor
            End Get
        End Property

        Public Overrides ReadOnly Property ApplicationSyntaxReference As SyntaxReference
            Get
                Return m_ApplicationNode
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonConstructorArguments As ImmutableArray(Of TypedConstant)
            Get
                Return m_ConstructorArguments
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonNamedArguments As ImmutableArray(Of KeyValuePair(Of String, TypedConstant))
            Get
                Return m_NamedArguments
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsConditionallyOmitted As Boolean
            Get
                Return m_IsConditionallyOmitted
            End Get
        End Property

        Friend Function WithOmittedCondition(isConditionallyOmitted As Boolean) As SourceAttributeData
            If Me.IsConditionallyOmitted = isConditionallyOmitted Then
                Return Me
            End If

            Return New SourceAttributeData(Me.ApplicationSyntaxReference,
                                           Me.AttributeClass,
                                           Me.AttributeConstructor,
                                           Me.CommonConstructorArguments,
                                           Me.CommonNamedArguments,
                                           isConditionallyOmitted,
                                           Me.HasErrors)
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasErrors As Boolean
            Get
                Return m_HasErrors
            End Get
        End Property

        ''' <summary>
        ''' This method finds an attribute by metadata name and signature. The algorithm for signature matching is similar to the one
        ''' in Module.GetTargetAttributeSignatureIndex. Note, the signature matching is limited to primitive types
        ''' and System.Type.  It will not match an arbitrary signature but it is sufficient to match the signatures of the current set of
        ''' well known attributes.
        ''' </summary>
        ''' <param name="description">Attribute to match.</param>
        Friend Overrides Function GetTargetAttributeSignatureIndex(targetSymbol As Symbol, description As AttributeDescription) As Integer
            If Not IsTargetAttribute(description.Namespace, description.Name, description.MatchIgnoringCase) Then
                Return -1
            End If

            Dim lazySystemType As TypeSymbol = Nothing

            Dim ctor = AttributeConstructor
            ' Ensure that the attribute data really has a constructor before comparing the signature.
            If ctor Is Nothing Then
                Return -1
            End If

            Dim parameters = ctor.Parameters
            Dim foundMatch = False

            For i = 0 To description.Signatures.Length - 1
                Dim targetSignature = description.Signatures(i)
                If targetSignature(0) <> SignatureHeader.HasThis Then
                    Continue For
                End If

                Dim parameterCount = targetSignature(1)
                If parameterCount <> parameters.Length Then
                    Continue For
                End If

                If CType(targetSignature(2), SignatureTypeCode) <> SignatureTypeCode.Void Then
                    Continue For
                End If

                foundMatch = (targetSignature.Length = 3)
                Dim k = 0
                For j = 3 To targetSignature.Length - 1
                    If k >= parameters.Length Then
                        Exit For
                    End If

                    Dim parameterType As TypeSymbol = parameters(k).Type
                    Dim specType = parameterType.GetEnumUnderlyingTypeOrSelf.SpecialType
                    Dim targetType As Byte = targetSignature(j)

                    If targetType = SignatureTypeCode.TypeHandle Then
                        j += 1

                        If parameterType.Kind <> SymbolKind.NamedType AndAlso parameterType.Kind <> SymbolKind.ErrorType Then
                            foundMatch = False
                            Exit For
                        End If

                        Dim namedType = DirectCast(parameterType, NamedTypeSymbol)
                        Dim targetInfo As AttributeDescription.TypeHandleTargetInfo = AttributeDescription.TypeHandleTargets(targetSignature(j))

                        ' Compare name and containing symbol name. Uses HasNameQualifier
                        ' extension method to avoid string allocations.
                        If Not String.Equals(namedType.MetadataName, targetInfo.Name, StringComparison.Ordinal) OrElse
                            Not namedType.HasNameQualifier(targetInfo.Namespace, StringComparison.Ordinal) Then
                            foundMatch = False
                            Exit For
                        End If

                        targetType = CByte(targetInfo.Underlying)
                    End If

                    Select Case targetType
                        Case CByte(SignatureTypeCode.Boolean)
                            foundMatch = specType = SpecialType.System_Boolean
                            k += 1

                        Case CByte(SignatureTypeCode.Char)
                            foundMatch = specType = SpecialType.System_Char
                            k += 1

                        Case CByte(SignatureTypeCode.SByte)
                            foundMatch = specType = SpecialType.System_SByte
                            k += 1

                        Case CByte(SignatureTypeCode.Byte)
                            foundMatch = specType = SpecialType.System_Byte
                            k += 1

                        Case CByte(SignatureTypeCode.Int16)
                            foundMatch = specType = SpecialType.System_Int16
                            k += 1

                        Case CByte(SignatureTypeCode.UInt16)
                            foundMatch = specType = SpecialType.System_UInt16
                            k += 1

                        Case CByte(SignatureTypeCode.Int32)
                            foundMatch = specType = SpecialType.System_Int32
                            k += 1

                        Case CByte(SignatureTypeCode.UInt32)
                            foundMatch = specType = SpecialType.System_UInt32
                            k += 1

                        Case CByte(SignatureTypeCode.Int64)
                            foundMatch = specType = SpecialType.System_Int64
                            k += 1

                        Case CByte(SignatureTypeCode.UInt64)
                            foundMatch = specType = SpecialType.System_UInt64
                            k += 1

                        Case CByte(SignatureTypeCode.Single)
                            foundMatch = specType = SpecialType.System_Single
                            k += 1

                        Case CByte(SignatureTypeCode.Double)
                            foundMatch = specType = SpecialType.System_Double
                            k += 1

                        Case CByte(SignatureTypeCode.String)
                            foundMatch = specType = SpecialType.System_String
                            k += 1

                        Case CByte(SignatureTypeCode.Object)
                            foundMatch = specType = SpecialType.System_Object
                            k += 1

                        Case CByte(SerializationTypeCode.Type)
                            If lazySystemType Is Nothing Then
                                lazySystemType = GetSystemType(targetSymbol)
                            End If

                            foundMatch = parameterType = lazySystemType
                            k += 1

                        Case Else
                            Return -1
                    End Select

                    If Not foundMatch Then
                        Exit For
                    End If
                Next

                If foundMatch Then
                    Return i
                End If
            Next

            Debug.Assert(Not foundMatch)
            Return -1
        End Function

        ''' <summary>
        ''' Gets the System.Type type symbol from targetSymbol's containing assembly.
        ''' </summary>
        ''' <param name="targetSymbol">Target symbol on which this attribute is applied.</param>
        ''' <returns>System.Type type symbol.</returns>
        Friend Overridable Function GetSystemType(targetSymbol As Symbol) As TypeSymbol
            Return targetSymbol.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type)
        End Function

    End Class

End Namespace

