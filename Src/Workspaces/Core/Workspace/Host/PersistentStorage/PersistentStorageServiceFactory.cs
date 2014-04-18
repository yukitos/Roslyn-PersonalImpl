﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Default)]
    internal class PersistentStorageServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IPersistentStorageService singleton = new Service();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return singleton;
        }

        private class Service : IPersistentStorageService
        {
            private readonly IPersistentStorage storage = new NoOpPersistentStorage();

            public IPersistentStorage GetStorage(Solution solution)
            {
                return storage;
            }
        }
    }
}