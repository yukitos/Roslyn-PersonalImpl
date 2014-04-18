// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingStrongNameProvider : DesktopStrongNameProvider
        {
            private readonly TouchedFileLogger logger;

            public LoggingStrongNameProvider(ImmutableArray<string> keyFileSearchPaths, TouchedFileLogger logger)
                : base(keyFileSearchPaths)
            {
                this.logger = logger;
            }

            internal override bool FileExists(string fullPath)
            {
                if (logger != null && fullPath != null)
                {
                    this.logger.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }

            internal override byte[] ReadAllBytes(string fullPath)
            {
                if (logger != null)
                {
                    this.logger.AddRead(fullPath);
                }

                return base.ReadAllBytes(fullPath);
            }
        }
    }
}
