using System.Reflection;
using ScheMigrator.Migrations;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace ScheMigrator
{
    public class DDLGenerator
    {
        public string[] TargetAssemblyPaths { get; set; }
        public string[] DependencyAssemblyPaths { get; set; }
        public string ScheMigratorAssemblyPath { get; set; }
        public string OutputPath { get; set; }
        public string Implementation { get; set; }
        public string Schema { get; set; } = "public";
        public bool Verbose { get; set; } = false;

        public bool Execute()
        {
            foreach (string assemblyPath in TargetAssemblyPaths)
            {


                try
                {
                    LogVerbose($"Starting DDLGeneratorTask with assembly: {assemblyPath}");
                    Type[] types;
                    Assembly assembly;
                    try
                    {
                        // Load the assembly with enhanced error handling and logging
                        assembly = LoadAssemblyWithDependencies(assemblyPath);

                        if (assembly == null)
                        {
                            Console.Error.WriteLine("Failed to load assembly: " + DependencyAssemblyPaths);
                            continue;
                        }

                        LogVerbose($"Successfully loaded assembly: {assembly.FullName}");

                        // Get all types and log counts for debugging
                        try
                        {
                            var allTypes = assembly.GetTypes();
                            LogVerbose($"Found {allTypes.Length} total types in assembly");
                            LogVerbose(string.Join("\n", allTypes.Select(t => t.FullName)));

                            // Fall back to name-based attribute detection
                            types = allTypes.Where(t => HasAttributeByName(t, nameof(ScheModelAttribute))).ToArray();

                            LogVerbose($"Found {types.Length} types with ScheModelAttribute");
                        }
                        catch (ReflectionTypeLoadException e)
                        {
                            Console.WriteLine($"ReflectionTypeLoadException: {e.Message}");
                            foreach (var loaderEx in e.LoaderExceptions ?? Array.Empty<Exception>())
                            {
                                Console.WriteLine($"Loader exception: {loaderEx?.Message}");
                            }

                            var loadedTypes = e.Types.Where(t => t != null).ToArray();
                            LogVerbose($"Partially loaded {loadedTypes.Length} types");

                            types = loadedTypes.Where(t => HasAttributeByName(t, nameof(ScheModelAttribute))).ToArray();
                            LogVerbose($"Found {types.Length} types with {nameof(ScheModelAttribute)}");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Unable to load assembly " + DependencyAssemblyPaths.FirstOrDefault());
                        Console.Error.WriteLine(e.ToString());
                        continue;
                    }

                    if (types.Length == 0)
                    {
                        Console.WriteLine("No types with ScheModelAttribute found in assembly.");
                        continue;
                    }

                    if (!Enum.TryParse(Implementation, out SqlImplementation sqlImplementation))
                    {
                        Console.Error.WriteLine($"SQL Implementation could not be parsed: {Implementation}");
                        continue;
                    }

                    ScheModelPackage modelPackage = new ScheModelPackage(sqlImplementation);

                    foreach (var type in types)
                    {
                        Console.WriteLine($"Processing type: {type.FullName}");

                        try
                        {
                            // Generate DDL
                            string ddl = ScheModelGenerator.GenerateModelDDL(type, sqlImplementation, Schema);

                            // Add DDL to Package
                            modelPackage.AddScript(ddl);

                            Console.WriteLine($"Generated DDL for {type.Name}");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Error processing type {type.Name}: {ex.Message}");
                            LogVerbose(ex.StackTrace);
                            continue;
                        }
                    }

                    // Save DDL to file
                    var fileName = $"{Implementation.ToLower()}-{assembly.GetName().Name?.ToLower() ?? throw new Exception("Assembly Name could not be used to generate DDL Package.")}.schpkg";
                    var filePath = Path.Combine(OutputPath, fileName);
                    Directory.CreateDirectory(OutputPath);
                    File.WriteAllBytes(filePath, modelPackage.MakePackage());

                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error executing DDL Generation: {ex.Message}");
                    LogVerbose(ex.StackTrace);
                    return false;
                }    
            }
            return true;
        }

        private bool HasAttributeByName(Type type, string attributeName)
        {
            try
            {
                var attrs = type.GetCustomAttributesData();
                LogVerbose($"Attributes found on {type.Name}:");
                LogVerbose(string.Join("\n", attrs.Select(a => a.AttributeType.Name)));
                return attrs.Any(a => a.AttributeType.Name == attributeName ||
                              a.AttributeType.Name == attributeName.Replace("Attribute", ""));
            }
            catch
            {
                // If we can't check attributes, assume it doesn't have it
                return false;
            }
        }

        private Assembly? LoadAssemblyWithDependencies(string assemblyPath)
        {
            try
            {
                 LogVerbose(
                     $"Loading assemblies from: {string.Join("\n", DependencyAssemblyPaths.Select(p => Path.GetFullPath(p)))}");

                // Load runtime assemblies, ScheMigrator assembly, and the target assembly
                string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
                var paths = new List<string>(runtimeAssemblies)
                {
                    // Add ScheMigrator assembly first so it's available for attribute detection
                    ScheMigratorAssemblyPath
                };
                
                // Then add the target assembly
                foreach (var p in DependencyAssemblyPaths)
                {
                    paths.Add(p);
                }
                
                LogVerbose(string.Join("\n", paths.Select(p => Path.GetFullPath(p))));

                var resolver = new PathAssemblyResolver(paths);
                MetadataLoadContext context = new MetadataLoadContext(resolver);

                // Load ScheMigrator assembly first to ensure attribute type is available
                var scheMigratorAssembly = context.LoadFromAssemblyPath(ScheMigratorAssemblyPath);
                LogVerbose($"Loaded ScheMigrator assembly: {scheMigratorAssembly.FullName}");

                // Then load the target assembly
                foreach (var p in DependencyAssemblyPaths)
                {
                    LogVerbose($"Loading assembly {p}: {context.LoadFromAssemblyPath(p) != null}");
                }
                
                //LogVerbose(context.GetAssemblies().Select(a => a.GetName().Name).Aggregate((a, b) => $"{a}, {b}"));
                Assembly assembly = context.LoadFromAssemblyPath(assemblyPath);

                LogVerbose($"Successfully loaded assembly: {assembly.FullName}");
                return assembly;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load assembly {assemblyPath}: {ex.Message}");
                LogVerbose(ex.StackTrace);
                return null;
            }
        }

        private void LogVerbose(string message)
        {
            if (Verbose)
            {
                Console.WriteLine(message);
            }
        }
    }
}