# Stable
- JLib package
    - [Automated Configuration discovery](JLib/Configuration/Jlib.Configuration%20Documentation.md)
    - [Exception Helper](Jlib/Exceptions/Jlib.Exceptions%20Documentation.md)
    - [Simplified Reflection (TypeCache)](Jlib/Reflection/Jlib.Reflection%20Documentation.md)
    - [\<\<partially unstable>>ValueTypes](JLib/ValueTypes/Jlib.ValueTypes%20Documentation.md)
- [JLib.DataGeneration package](JLib.DataGeneration/JLib.DataGeneration%20Documentation.md)
- [JLib.HotChocolate package](JLib.HotChocolate/JLib.HotChocolate%20Documentation.md)
- [JLib.EfCore package](JLib.EfCore/JLib.EfCore%20Documentation.md)

## pre-release
these modules are still under development and will change in the future
- [Automated Automapper Profiles](JLib/Automapper/Jlib.Automapper%20Documentation.md)
- [Helper](Jlib/helper/Jlib.Helper%20Documentation.md)
- [ValueTypes](JLib/ValueTypes/Jlib.ValueTypes%20Documentation.md)
- [Abstracted Datasources](JLib/Data/Jlib.Data%20Documentation.md)
    - [Authoriation of Abstracted Datasources](JLib/Data/Authorization/Jlib.Data.Authorization%20Documentation.md)

# Create Version: 
dotnet build --version-suffix [Version] --no-incremental -c Release
git tag v[Version]
git push --tags
dotnet pack --version-suffix [Version] -c Release -o .\bin\[Version]
cd .\bin\[Version]
dotnet nuget push * --source https://api.nuget.org/v3/index.json --skip-duplicate --api-key [key]

# Add new Package:
to mark an assembly as Package, add it to the PackageAssembly tag in [Directory.Build.Props](.\Directory.Build.Props)