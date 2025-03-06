using System;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using DataBlocks.Migrations;
using ScheMigrator.Migrations;

namespace ScheMigrator;

public class DDLGeneratorTask : Microsoft.Build.Utilities.Task
{
    [Required]
    public string AssemblyPath { get; set; }

    [Required]
    public string OutputPath { get; set; }
    [Required]
    public string Implementation { get; set; }
    public string Schema { get; set; } = "public";

    [Output]
    public ITaskItem[] ProcessedTypes { get; set; }

    public override bool Execute()
    {
        try
        {
            ScheModelPackage modelPackage = new ScheModelPackage();
            
            try
            {
                Log.LogMessage(MessageImportance.Normal, Path.GetFullPath(AssemblyPath));
                Type[] types;
                Assembly assembly;
                try
                {
                    // Load the target assembly using LoadFile instead of LoadFrom
                    assembly = Assembly.LoadFrom(Path.GetFullPath(AssemblyPath));
                    types = assembly.GetTypes()
                        .Where(t => t.GetCustomAttributes()
                            .Any(a => a.GetType().Name == nameof(ScheModelAttribute))).ToArray();
                }
                // catch (ReflectionTypeLoadException e)
                // {
                //     Log.LogWarningFromException(e, true);
                //     types = e.Types.Where(t => t != null)
                //         .Select<Type?, Type>(t => t)
                //         .Where(t => t.GetCustomAttributes()
                //             .Any(a => a.GetType().Name == nameof(ScheModelAttribute))).ToArray();
                // }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                    return true;
                }
                
                var processedTypes = new List<ITaskItem>();
                if (!SqlImplementation.TryParse(Implementation, out SqlImplementation sqlImplementation))
                {
                    Log.LogError($"SqlImplementation could not be parsed: {Implementation}");
                    return false;
                }

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
                            return false;
                        }
                    }
                }
                
                // Save DDL to file
                var fileName = $"{assembly.GetName().Name?.ToLower() ?? throw new Exception("Assembly Name could not be used to generate DDL Package.")}.schpkg";
                var filePath = Path.Combine(OutputPath, fileName);
                Directory.CreateDirectory(OutputPath);
                File.WriteAllBytes(filePath, modelPackage.Package());
                
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
            Log.LogError($"Error executing DDL Generation: {ex.Message}");
            return false;
        }
    }
}
