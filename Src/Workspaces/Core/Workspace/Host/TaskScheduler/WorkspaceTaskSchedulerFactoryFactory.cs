﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Default)]
    internal class WorkspaceTaskSchedulerFactoryFactory : IWorkspaceServiceFactory
    {
        private readonly WorkspaceTaskSchedulerFactory singleton = new WorkspaceTaskSchedulerFactory();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return singleton;
        }
    }
}
