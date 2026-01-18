using JD.MSBuild.Fluent.Packaging;
using JD.Efcpt.Build.Definitions;

var def = DefinitionFactory.Create();

Console.WriteLine("PackageDefinition:");
Console.WriteLine($"  Id: {def.Id}");
Console.WriteLine($"  BuildProps: {(def.BuildProps != null ? "SET" : "null")}");
Console.WriteLine($"  BuildTargets: {(def.BuildTargets != null ? "SET" : "null")}");
Console.WriteLine($"  BuildTransitiveProps: {(def.BuildTransitiveProps != null ? "SET" : "null")}");
Console.WriteLine($"  BuildTransitiveTargets: {(def.BuildTransitiveTargets != null ? "SET" : "null")}");

var emitter = new MsBuildPackageEmitter();
var outputDir = args.Length > 0 ? args[0] : "output";
emitter.Emit(def, outputDir);
Console.WriteLine($"\nGenerated MSBuild assets to: {outputDir}");
