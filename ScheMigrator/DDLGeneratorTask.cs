using System;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using DataBlocks.Migrations;
using ScheMigrator.DDL;

namespace ScheMigrator;

public class DDLGeneratorTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string AssemblyPath { get; set; }

    [Required]
    public string OutputPath { get; set; }
    [Required]
    public string Implementation { get; set; }

    [Output]
    public ITaskItem[] ProcessedTypes { get; set; }

    public override bool Execute()
    {
        try
        {
            try
            {
                Log.LogMessage(MessageImportance.Normal, Path.GetFullPath(AssemblyPath));
                Type[] types;
                try
                {
                    // Load the target assembly using LoadFile instead of LoadFrom
                    var assembly = Assembly.LoadFrom(Path.GetFullPath(AssemblyPath));
                    types = assembly.GetTypes()
                        .Where(t => t.GetCustomAttributes()
                            .Any(a => a.GetType().Name == nameof(ScheModelAttribute))).ToArray();
                // Log.LogMessage(MessageImportance.Normal, assembly.GetTypes().Select(t => t.GetCustomAttributes().Select(a => a.GetType().Name)
                //         .Aggregate("", (soFar, nextItem) => $"{soFar}, {nextItem}")).Aggregate("", (soFar, nextItem) => $"{soFar}, {nextItem}"));
                // Log.LogMessage(MessageImportance.Normal, types.Length.ToString());
                }
                catch (ReflectionTypeLoadException e)
                {
                    Log.LogWarningFromException(e, true);
                    types = e.Types.Where(t => t != null)
                        .Select<Type?, Type>(t => t)
                        .Where(t => t.GetCustomAttributes()
                            .Any(a => a.GetType().Name == nameof(ScheModelAttribute))).ToArray();
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                    return true;
                }
                
                // // Load DataBlocks assembly from the same directory
                // var dataBlocksPath = Path.Combine(Path.GetDirectoryName(AssemblyPath)!, "DataBlocks.dll");
                // // Assembly dataBlocks = Assembly.LoadFrom(Path.GetFullPath(dataBlocksPath));
                // var dataBlocks = Assembly.Load(dataBlocksPath);
                // Type scheModelType = dataBlocks.GetType(nameof(ScheModelAttribute));
                
                var processedTypes = new List<ITaskItem>();

                foreach (var type in types)
                {
                    Log.LogMessage(MessageImportance.Normal, $"Processing type: {type.FullName}");
                    var sqlAttr = type.GetCustomAttributes()
                        .FirstOrDefault(a => a.GetType().Name == nameof(ScheModelAttribute));
                    if (sqlAttr != null)
                    {  
                        Log.LogMessage(MessageImportance.Normal, $"Processing model: {type.FullName}");
                        try
                        {
                            // // Get SQL implementation using reflection instead of direct cast
                            // Log.LogMessage(MessageImportance.Low, scheModelType.GetProperty(nameof(ScheModelAttribute.SqlImplementation))?.GetValue(sqlAttr)?.ToString());
                            if (!SqlImplementation.TryParse(Implementation, out SqlImplementation sqlImplementation))
                            {
                                Log.LogError($"Could not get SqlImplementation from attribute for type {type.Name}");
                                return false;
                            }
                            
                            
                            // Get SQL generator for this type
                            var generator = SqlGeneratorFactory.Build(sqlImplementation);
                            
                            // Generate DDL
                            string ddl = DDLGenerator.GenerateDDL(type, generator);
                            
                            // Save DDL to file
                            var fileName = $"{type.Name}.sql";
                            var filePath = Path.Combine(OutputPath, fileName);
                            Directory.CreateDirectory(OutputPath);
                            File.WriteAllText(filePath, ddl);

                            // Create task item for output
                            var taskItem = new TaskItem(type.FullName);
                            taskItem.SetMetadata("DDLPath", filePath);
                            taskItem.SetMetadata("SqlImplementation", Implementation.ToString());
                            processedTypes.Add(taskItem);
                            
                            Log.LogMessage(MessageImportance.Normal, $"Generated DDL for {type.Name} at {filePath}");
                        }
                        catch (Exception ex)
                        {
                            Log.LogError($"Error processing type {type.Name}: {ex.Message}");
                            return false;
                        }
                    }
                }
                
                ProcessedTypes = processedTypes.ToArray();
                return !Log.HasLoggedErrors;
            }
            catch (Exception e)
            {
                Log.LogWarningFromException(e, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Error executing DDLGeneratorTask: {ex.Message}");
            return false;
        }
    }
}
