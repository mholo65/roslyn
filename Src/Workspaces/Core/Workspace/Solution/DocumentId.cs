﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// An identifier that can be used to retrieve the same <see cref="Document"/> across versions of the
    /// workspace.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    [Serializable]
    public class DocumentId : IEquatable<DocumentId>
    {
        public ProjectId ProjectId { get; private set; }
        public Guid Id { get; private set; }

        private string debugName;

        private DocumentId(ProjectId projectId, string debugName)
        {
            this.ProjectId = projectId;
            this.Id = Guid.NewGuid();
            this.debugName = debugName;
        }

        /// <summary>
        /// Creates a new <see cref="DocumentId"/> instance.
        /// </summary>
        /// <param name="projectId">The project id this document id is relative to.</param>
        /// <param name="debugName">An optional name to make this id easier to recognize while debugging.</param>
        public static DocumentId CreateNewId(ProjectId projectId, string debugName = null)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException("projectId");
            }

            return new DocumentId(projectId, debugName);
        }

        internal string GetDebuggerDisplay()
        {
            return string.Format("({0}, #{1} - {2})", this.GetType().Name, this.Id, this.debugName);
        }

        public override string ToString()
        {
            return GetDebuggerDisplay();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as DocumentId);
        }

        public bool Equals(DocumentId other)
        {
            // Technically, we don't need to check project id.
            return
                !ReferenceEquals(other, null) &&
                this.Id == other.Id &&
                this.ProjectId == other.ProjectId;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.ProjectId, this.Id.GetHashCode());
        }

        public static bool operator ==(DocumentId left, DocumentId right)
        {
            return EqualityComparer<DocumentId>.Default.Equals(left, right);
        }

        public static bool operator !=(DocumentId left, DocumentId right)
        {
            return !(left == right);
        }
    }
}