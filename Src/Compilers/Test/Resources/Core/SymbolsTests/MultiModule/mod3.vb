﻿' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'vbc /t:module /vbruntime- Mod3.vb

Public Class Class3
Sub Foo()
Dim x = {1,2}
Dim y = x.Count()
End Sub
End Class