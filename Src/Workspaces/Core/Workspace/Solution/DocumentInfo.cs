﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A class that represents all the arguments necessary to create a new document instance.
    /// </summary>
    public sealed class DocumentInfo
    {
        /// <summary>
        /// The Id of the document.
        /// </summary>
        public DocumentId Id { get; private set; }

        /// <summary>
        /// The name of the document.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The names of the logical nested folders the document is contained in.
        /// </summary>
        public IReadOnlyList<string> Folders { get; private set; }

        /// <summary>
        /// The kind of the source code.
        /// </summary>
        public SourceCodeKind SourceCodeKind { get; private set; }

        /// <summary>
        /// The file path of the document.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// The text of the document and its version.
        /// </summary>
        public TextAndVersion TextAndVersion { get; private set; }

        /// <summary>
        /// A loader that can retrieve the document text.
        /// </summary>
        public TextLoader TextLoader { get; private set; }

        /// <summary>
        /// True if the document is a side effect of the build.
        /// </summary>
        public bool IsGenerated { get; private set; }

        /// <summary>
        /// Create a new instance of a DocumentInfo.
        /// </summary>
        private DocumentInfo(
            DocumentId id,
            string name,
            IEnumerable<string> folders,
            SourceCodeKind sourceCodeKind,
            TextLoader loader,
            string filePath,
            bool isGenerated)
        {
            if (id == null)
            {
                throw new ArgumentNullException("id");
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            this.Id = id;
            this.Name = name;
            this.Folders = folders.ToImmutableListOrEmpty();
            this.SourceCodeKind = sourceCodeKind;
            this.TextLoader = loader;
            this.FilePath = filePath;
        }

        public static DocumentInfo Create(
            DocumentId id,
            string name,
            IEnumerable<string> folders = null,
            SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
            TextLoader loader = null,
            string filePath = null,
            bool isGenerated = false)
        {
            return new DocumentInfo(id, name, folders, sourceCodeKind, loader, filePath, isGenerated);
        }

        private DocumentInfo With(
            DocumentId id = null,
            string name = null,
            Optional<IEnumerable<string>> folders = default(Optional<IEnumerable<string>>),
            Optional<SourceCodeKind> sourceCodeKind = default(Optional<SourceCodeKind>),
            Optional<TextAndVersion> textAndVersion = default(Optional<TextAndVersion>),
            Optional<TextLoader> loader = default(Optional<TextLoader>),
            Optional<string> filePath = default(Optional<string>))
        {
            var newId = id ?? this.Id;
            var newName = name ?? this.Name;
            var newFolders = folders.HasValue ? folders.Value : this.Folders;
            var newSourceCodeKind = sourceCodeKind.HasValue ? sourceCodeKind.Value : this.SourceCodeKind;
            var newLoader = loader.HasValue ? loader.Value : this.TextLoader;
            var newFilePath = filePath.HasValue ? filePath.Value : this.FilePath;

            if (newId == this.Id &&
                newName == this.Name &&
                newFolders == this.Folders &&
                newSourceCodeKind == this.SourceCodeKind &&
                newLoader == this.TextLoader &&
                newFilePath == this.FilePath)
            {
                return this;
            }

            return new DocumentInfo(newId, newName, newFolders, newSourceCodeKind, newLoader, newFilePath, this.IsGenerated);
        }

        public DocumentInfo WithFolders(IEnumerable<string> folders)
        {
            return this.With(folders: new Optional<IEnumerable<string>>(folders));
        }

        public DocumentInfo WithSourceCodeKind(SourceCodeKind kind)
        {
            return this.With(sourceCodeKind: kind);
        }

        public DocumentInfo WithTextLoader(TextLoader loader)
        {
            return this.With(loader: loader);
        }

        public DocumentInfo WithFilePath(string filePath)
        {
            return this.With(filePath: filePath);
        }
    }
}