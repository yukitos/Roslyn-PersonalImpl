﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="ILanguageService"/> implementation for inclusion in a <see cref="MefHostServices"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportLanguageServiceAttribute : ExportAttribute
    {
        /// <summary>
        /// The assembly qualified name of the service's type.
        /// </summary>
        public string ServiceType { get; private set; }

        /// <summary>
        /// The language that the service is target for; LanguageNames.CSharp, etc.
        /// </summary>
        public string Language { get; private set; }

        /// <summary>
        /// The layer that the service is specified for; ServiceLayer.Default, etc.
        /// </summary>
        public string Layer { get; private set; }

        /// <summary>
        /// Declares a <see cref="ILanguageService"/> implementation for inclusion in a <see cref="MefHostServices"/>.
        /// </summary>
        /// <param name="type">The type that will be used to retrieve the service from a <see cref="HostLanguageServices"/>.</param>
        /// <param name="language">The language that the service is target for; LanguageNames.CSharp, etc.</param>
        /// <param name="layer">The layer that the service is specified for; ServiceLayer.Default, etc.</param>
        public ExportLanguageServiceAttribute(Type type, string language, string layer = ServiceLayer.Default)
            : base(typeof(ILanguageService))
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            this.ServiceType = type.AssemblyQualifiedName;
            this.Language = language;
            this.Layer = layer;
        }
    }
}