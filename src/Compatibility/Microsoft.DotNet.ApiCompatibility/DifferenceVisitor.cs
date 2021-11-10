﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.ApiCompatibility.Abstractions;
using Microsoft.DotNet.ApiCompatibility.Extensions;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.ApiCompatibility
{
    /// <summary>
    /// The visitor that traverses the mappers' tree and gets it's differences in a <see cref="DiagnosticBag{CompatDifference}"/>.
    /// </summary>
    public class DifferenceVisitor : MapperVisitor
    {
        private readonly HashSet<CompatDifference>[] _diagnostics;

        /// <summary>
        /// Instantiates the visitor with the desired settings.
        /// </summary>
        /// <param name="rightCount">Represents the number of elements that the mappers contain on the right hand side.</param>
        public DifferenceVisitor(int rightCount = 1)
        {
            if (rightCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rightCount));
            }

            _diagnostics = new HashSet<CompatDifference>[rightCount];

            for (int i = 0; i < rightCount; i++)
            {
                _diagnostics[i] = new HashSet<CompatDifference>();
            }
        }

        /// <summary>
        /// Visits an <see cref="AssemblyMapper"/> and adds it's differences to the <see cref="DiagnosticBag{CompatDifference}"/>.
        /// </summary>
        /// <param name="assembly">The mapper to visit.</param>
        public override void Visit(AssemblyMapper assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            AddDifferences(assembly);
            base.Visit(assembly);
            // After visiting the assembly, the assembly mapper will contain any assembly load errors that happened
            // when trying to resolve typeforwarded types. If there were any, we add them to the diagnostic bag next.
            AddAssemblyLoadErrors(assembly);
        }

        /// <summary>
        /// Visits an <see cref="TypeMapper"/> and adds it's differences to the <see cref="DiagnosticBag{CompatDifference}"/>.
        /// </summary>
        /// <param name="type">The mapper to visit.</param>
        public override void Visit(TypeMapper type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            AddDifferences(type);

            if (type.ShouldDiffMembers)
            {
                base.Visit(type);
            }
        }

        /// <summary>
        /// Visits an <see cref="MemberMapper"/> and adds it's differences to the <see cref="DiagnosticBag{CompatDifference}"/>.
        /// </summary>
        /// <param name="member">The mapper to visit.</param>
        public override void Visit(MemberMapper member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            AddDifferences(member);
        }

        /// <summary>
        /// A list of <see cref="DiagnosticBag{CompatDifference}"/>.
        /// One per element compared in the right hand side.
        /// </summary>
        public IEnumerable<IEnumerable<CompatDifference>> DiagnosticCollections => _diagnostics;

        private void AddDifferences<T>(ElementMapper<T> mapper)
        {
            IReadOnlyList<IEnumerable<CompatDifference>> differences = mapper.GetDifferences();

            AddToDiagnosticCollections(differences);
        }

        private void AddAssemblyLoadErrors<T>(ElementMapper<T> mapper)
        {
            IReadOnlyList<IEnumerable<CompatDifference>> assemblyLoadErrors = mapper.GetAssemblyLoadErrors();

            AddToDiagnosticCollections(assemblyLoadErrors);
        }

        private void AddToDiagnosticCollections(IReadOnlyList<IEnumerable<CompatDifference>> diagnosticsToAdd)
        {
            if (_diagnostics.Length != diagnosticsToAdd.Count)
            {
                throw new InvalidOperationException(Resources.VisitorRightCountShouldMatchMappersSetSize);
            }

            for (int i = 0; i < diagnosticsToAdd.Count; i++)
            {
                _diagnostics[i].AddRange(diagnosticsToAdd[i]);
            }
        }
    }
}
