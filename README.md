# Xtansia.IvtPatch

An MSBuild task to work around the issue of ILLink always trimming `InternalsVisibleTo` attributes which are "unresolvable" (https://github.com/dotnet/runtime/issues/92582).
This issue manifests itself when using libraries such as Utf8Json which uses `InternalsVisibleTo` to expose internal types to assemblies dynamically generated at runtime.
The task will copy the `InternalsVisibleTo` attributes from the original assembly to the trimmed assembly after ILLink has run.

## Usage

Add the following to your project file:

```xml
<Project>
    <ItemGroup>
        <PackageReference Include="Xtansia.IvtPatch" Version="1.0.0" />
    </ItemGroup>
    
    <ItemGroup>
        <IvtPatchAssembly Include="Utf8Json" />
    </ItemGroup>
</Project>
```