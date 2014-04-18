﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageServiceFactory(GetType(ICodeGenerationService), LanguageNames.VisualBasic)>
    Friend Class VisualBasicCodeGenerationServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeGenerationService(provider)
        End Function
    End Class
End Namespace
