﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    /// <summary>
    /// Use this attribute to declare a <see cref="IWorkspaceService"/> implementation for inclusion in a <see cref="MefHostServices"/>.
    /// </summary>
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportWorkspaceServiceAttribute : ExportAttribute
    {
        /// <summary>
        /// The assembly qualified name of the service's type.
        /// </summary>
        public string ServiceType { get; private set; }

        /// <summary>
        /// The layer that the service is specified for; ServiceLayer.Default, etc.
        /// </summary>
        public string Layer { get; private set; }

        /// <summary>
        /// Declares a <see cref="IWorkspaceService"/> implementation for inclusion in a <see cref="MefHostServices"/>.
        /// </summary>
        /// <param name="serviceType">The type that will be used to retreive the service from a <see cref="HostWorkspaceServices"/>.</param>
        /// <param name="layer">The layer that the service is specified for; ServiceLayer.Default, etc.</param>
        public ExportWorkspaceServiceAttribute(Type serviceType, string layer = ServiceLayer.Default)
            : base(typeof(IWorkspaceService))
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }

            if (layer == null)
            {
                throw new ArgumentNullException("layer");
            }

            this.ServiceType = serviceType.AssemblyQualifiedName;
            this.Layer = layer;
        }
    }
}