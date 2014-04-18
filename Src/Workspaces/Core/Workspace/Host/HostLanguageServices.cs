﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Per language services provided by the host environment.
    /// </summary>
    public abstract class HostLanguageServices
    {
        /// <summary>
        /// The <see cref="HostWorkspaceServices"/> that originated this language service.
        /// </summary>
        public abstract HostWorkspaceServices WorkspaceServices { get; }

        /// <summary>
        /// The name of the language
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets a language specific service provided by the host identified by the service type. 
        /// If the host does not provide the service, this method returns null.
        /// </summary>
        public abstract TLanguageService GetService<TLanguageService>() where TLanguageService : ILanguageService;

        // common services

        /// <summary>
        /// A factory for creating compilations instances.
        /// </summary>
        public virtual ICompilationFactoryService CompilationFactory
        {
            get { return this.GetService<ICompilationFactoryService>(); }
        }

        // needs some work on the interface before it can be public
        internal virtual ISyntaxTreeFactoryService SyntaxTreeFactory
        {
            get { return this.GetService<ISyntaxTreeFactoryService>(); }
        }
    }
}