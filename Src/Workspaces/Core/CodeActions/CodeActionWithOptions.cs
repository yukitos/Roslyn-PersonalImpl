﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeAction"/> that can vary with user specified options.
    /// </summary>
    public abstract class CodeActionWithOptions : CodeAction
    {
        /// <summary>
        /// Gets the options to use with this code action.
        /// This method is gauranteed to be called on the UI thread.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An implementation specific object instance that holds options for applying the code action.</returns>
        public abstract object GetOptions(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the <see cref="CodeActionOperation"/>'s for this <see cref="CodeAction"/> given the specified options.
        /// </summary>
        /// <param name="options">An object instance returned from a prior call to <see cref="GetOptions(CancellationToken)"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync(object options, CancellationToken cancellationToken)
        {
            var operations = await this.ComputeOperationsAsync(options, cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                operations = await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return operations;
        }

        /// <summary>
        /// Override this method to compute the operations that implement this <see cref="CodeAction"/>.
        /// </summary>
        /// <param name="options">An object instance returned from a call to <see cref="GetOptions(CancellationToken)"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        protected abstract Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken);
    }
}