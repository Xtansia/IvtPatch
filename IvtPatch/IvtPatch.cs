/*
** This Source Code Form is subject to the terms of the Mozilla Public
** License, v. 2.0. If a copy of the MPL was not distributed with this
** file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace IvtPatch;

public class IvtPatch : Task
{
    [Required]
    public ITaskItem[] AssemblyPaths { get; set; }
    [Required]
    public ITaskItem[] ReferenceAssemblyPaths { get; set; }
    [Required]
    public ITaskItem OutputDirectory { get; set; }

    public override bool Execute()
    {
        var outputDirectory = Path.GetFullPath(OutputDirectory.ItemSpec);
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = new AssemblyResolver(ReferenceAssemblyPaths.Select(item => item.ItemSpec))
        };

        foreach (var assemblyPath in AssemblyPaths)
        {
            var assemblyFile = Path.GetFullPath(assemblyPath.ItemSpec);
            var assemblyFileName = Path.GetFileName(assemblyFile);
            var trimmedAssemblyFile = Path.GetFullPath(assemblyPath.GetMetadata("TrimmedAssembly"));
            var outputAssemblyFile = Path.Combine(outputDirectory, assemblyFileName);

            PatchAssembly(assemblyFile, trimmedAssemblyFile, outputAssemblyFile, readerParameters);
        }

        return true;
    }

    private static void PatchAssembly(string sourceAssemblyFile, string trimmedAssemblyFile, string outputAssemblyFile, ReaderParameters readerParameters)
    {
        var sourceAssembly = AssemblyDefinition.ReadAssembly(sourceAssemblyFile, readerParameters);
        var trimmedAssembly = AssemblyDefinition.ReadAssembly(trimmedAssemblyFile, readerParameters);
        var module = trimmedAssembly.MainModule;

        var ivtCtor = module.ImportReference(typeof(InternalsVisibleToAttribute).GetConstructor(new[] { typeof(string) }));

        var existingIvts = GetIvts(trimmedAssembly).ToImmutableHashSet();
        var newIvts = GetIvts(sourceAssembly).Where(ivt => !existingIvts.Contains(ivt));

        foreach (var ivt in newIvts)
        {
            var attr = new CustomAttribute(ivtCtor);
            attr.ConstructorArguments.Add(new CustomAttributeArgument(module.TypeSystem.String, ivt));
            trimmedAssembly.CustomAttributes.Add(attr);
        }

        trimmedAssembly.Write(outputAssemblyFile);

        return;

        IEnumerable<string> GetIvts(AssemblyDefinition assembly) =>
            assembly.CustomAttributes
                .Where(attr => attr.Constructor.FullName == ivtCtor.FullName)
                .Select(attr => attr.ConstructorArguments.First().Value.ToString());
    }
}