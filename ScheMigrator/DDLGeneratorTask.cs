using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using DataBlocks.Migrations;
using ScheMigrator.Migrations;
using NetBlocks.Utilities;
using System.Runtime.InteropServices;

namespace ScheMigrator;

public class DDLGeneratorTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string[] AssemblyPaths { get; set; }
    [Required]
    public string DataBlocksAssemblyPath { get; set; }

    [Required]
    public string OutputPath { get; set; }
    [Required]
    public string Implementation { get; set; }
    public string Schema { get; set; } = "public";
    
    // public string[] ReferencePaths { get; set; }
    
    // Add verbose logging option
    public bool Verbose { get; set; } = false;

    [Output]
    public ITaskItem[] ProcessedTypes { get; set; }

    public override bool Execute()
    {
        try
        {
            LogVerbose($"Starting DDLGeneratorTask with assembly: {AssemblyPaths.FirstOrDefault()}");
            Type[] types;
            Assembly assembly;
            try
            {
                // Use the ReferencePaths property
                // var referencePaths = ProcessReferencePaths();
                
                // Load the assembly with enhanced error handling and logging
                assembly = LoadAssemblyWithDependencies(AssemblyPaths);
                
                if (assembly == null)
                {
                    Log.LogError("Failed to load assembly: " + AssemblyPaths);
                    return false;
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
                    Log.LogWarning($"ReflectionTypeLoadException: {e.Message}");
                    foreach (var loaderEx in e.LoaderExceptions ?? Array.Empty<Exception>())
                    {
                        Log.LogWarning($"Loader exception: {loaderEx?.Message}");
                    }
                    
                    var loadedTypes = e.Types.Where(t => t != null).ToArray();
                    LogVerbose($"Partially loaded {loadedTypes.Length} types");
                    
                    // Try to find types with the attribute
                    types = loadedTypes.Where(t => HasAttributeByName(t, nameof(ScheModelAttribute))).ToArray();
                    LogVerbose($"Found {types.Length} types with {nameof(ScheModelAttribute)}");
                }
            }
            catch (Exception e)
            {
                Log.LogError("Unable to load assembly " + AssemblyPaths.FirstOrDefault());
                Log.LogErrorFromException(e, true);
                return false;
            }

            if (types.Length == 0)
            {
                Log.LogWarning("No types with ScheModelAttribute found in assembly.");
                return true;
            }

            var processedTypes = new List<ITaskItem>();
            if (!Enum.TryParse(Implementation, out SqlImplementation sqlImplementation))
            {
                Log.LogError($"SQL Implementation could not be parsed: {Implementation}");
                return false;
            }

            ScheModelPackage modelPackage = new ScheModelPackage(sqlImplementation);
            
            foreach (var type in types)
            {
                Log.LogMessage(MessageImportance.Normal, $"Processing type: {type.FullName}");
                
                try
                {
                    // Generate DDL
                    string ddl = ScheModelGenerator.GenerateModelDDL(type, sqlImplementation, Schema);

                    // Add DDL to Package
                    modelPackage.AddScript(ddl);

                    // Create task item for output
                    var taskItem = new TaskItem(type.FullName);
                    taskItem.SetMetadata("ProcessedModel", type.FullName);
                    taskItem.SetMetadata("SqlImplementation", Implementation);
                    processedTypes.Add(taskItem);
                        
                    Log.LogMessage(MessageImportance.Normal, $"Generated DDL for {type.Name}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"Error processing type {type.Name}: {ex.Message}");
                    LogVerbose(ex.StackTrace);
                    return false;
                }
            }
            
            // Save DDL to file
            var fileName = $"{Implementation.ToLower()}-{assembly.GetName().Name?.ToLower() ?? throw new Exception("Assembly Name could not be used to generate DDL Package.")}.schpkg";
            var filePath = Path.Combine(OutputPath, fileName);
            Directory.CreateDirectory(OutputPath);
            File.WriteAllBytes(filePath, modelPackage.MakePackage());
            
            ProcessedTypes = processedTypes.ToArray();
            return !Log.HasLoggedErrors;
        }
        catch (Exception ex)
        {
            Log.LogError($"Error executing DDL Generation: {ex.Message}");
            LogVerbose(ex.StackTrace);
            return false;
        }
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
    
    private Assembly LoadAssemblyWithDependencies(string[] assemblyPaths)
    {
        try
        {
            LogVerbose(
                $"Loading assemblies from: {string.Join("\n", assemblyPaths.Select(p => Path.GetFullPath(p)))}");

            string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");                        
            var paths = new List<string>(runtimeAssemblies);
            LogVerbose(string.Join("\n", paths.Select(p => Path.GetFullPath(p))));
            
            paths.Add(DataBlocksAssemblyPath);
            assemblyPaths.ForEach(p => paths.Add(p));            
            var resolver = new PathAssemblyResolver(paths);
            var context = new MetadataLoadContext(resolver);

            context.LoadFromAssemblyPath(DataBlocksAssemblyPath);
            Assembly assembly = context.LoadFromAssemblyPath(AssemblyPaths.First());
            
            LogVerbose($"Successfully loaded assembly: {assembly.FullName}");
            return assembly;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to load assembly {AssemblyPaths.FirstOrDefault()}: {ex.Message}");
            LogVerbose(ex.StackTrace);
            return null;
        }
    }

    private void LogVerbose(string message)
    {
        if (Verbose)
        {
            Log.LogMessage(MessageImportance.High, message);
        }
    }
}
