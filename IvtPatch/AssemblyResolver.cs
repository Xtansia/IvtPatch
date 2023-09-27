/*
** This Source Code Form is subject to the terms of the Mozilla Public
** License, v. 2.0. If a copy of the MPL was not distributed with this
** file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace IvtPatch;

public class AssemblyResolver : DefaultAssemblyResolver
{
    private readonly Dictionary<string, string> _assemblyPaths = new();

    public AssemblyResolver(IEnumerable<string> referencePaths)
    {
        foreach (var referencePath in referencePaths)
        {
            var fullPath = Path.GetFullPath(referencePath);
            var assemblyName = Path.GetFileNameWithoutExtension(fullPath);
            _assemblyPaths[assemblyName] = fullPath;
        }
    }

    protected override AssemblyDefinition SearchDirectory(AssemblyNameReference name, IEnumerable<string> directories, ReaderParameters parameters)
    {
        if (!_assemblyPaths.TryGetValue(name.Name, out var assemblyPath))
            return base.SearchDirectory(name, directories, parameters);

        parameters.AssemblyResolver ??= this;
        return AssemblyDefinition.ReadAssembly(assemblyPath, parameters);
    }
}