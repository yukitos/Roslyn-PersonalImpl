﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
    <ExportLanguageServiceFactory(GetType(ICodeCleanerService), LanguageNames.VisualBasic)>
    Partial Friend Class VisualBasicCodeCleanerServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(provider As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicCodeCleanerService()
        End Function
    End Class
End Namespace
