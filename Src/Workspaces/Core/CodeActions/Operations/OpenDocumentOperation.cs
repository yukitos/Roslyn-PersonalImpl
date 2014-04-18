// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A code action operation for requesting a document be opened in the host environment.
    /// </summary>
    public sealed class OpenDocumentOperation : CodeActionOperation
    {
        private readonly DocumentId documentId;

        public OpenDocumentOperation(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException("documentId");
            }

            this.documentId = documentId;
        }

        public DocumentId DocumentId
        {
            get { return this.documentId; }
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            if (workspace.CanOpenDocuments)
            {
                workspace.OpenDocument(this.documentId, activate: false);
            }
        }
    }
}