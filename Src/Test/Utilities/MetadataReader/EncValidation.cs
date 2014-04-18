﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using Xunit;

namespace Roslyn.Test.Utilities
{
    internal static class EncValidation
    {
        internal static void VerifyModuleMvid(int generation, MetadataReader previousReader, MetadataReader currentReader)
        {
            var previousModule = previousReader.GetModuleDefinition();
            var currentModule = currentReader.GetModuleDefinition();

            Assert.Equal(previousReader.GetGuid(previousModule.Mvid), currentReader.GetGuid(currentModule.Mvid));

            Assert.Equal(generation - 1, previousModule.Generation);
            Assert.Equal(generation, currentModule.Generation);

            if (generation == 1)
            {
                Assert.True(previousModule.GenerationId.IsNil);
                Assert.True(previousModule.BaseGenerationId.IsNil);

                Assert.False(currentModule.GenerationId.IsNil);
                Assert.True(currentModule.BaseGenerationId.IsNil);
            }
            else
            {
                Assert.False(currentModule.GenerationId.IsNil);
                Assert.False(currentModule.BaseGenerationId.IsNil);

                Assert.Equal(previousReader.GetGuid(previousModule.GenerationId), currentReader.GetGuid(currentModule.BaseGenerationId));
            }

            Assert.NotEqual(default(Guid), currentReader.GetGuid(currentModule.GenerationId));
        }
    }
}
