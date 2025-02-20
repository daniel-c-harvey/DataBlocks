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

    [Output]
    public ITaskItem[] ProcessedTypes { get; set; }

    public override bool Execute()
    {
        try
        {
            var assembly = Assembly.LoadFrom(AssemblyPath);
            var processedTypes = new List<ITaskItem>();

            foreach (var type in assembly.GetTypes())
            {
                Log.LogMessage(MessageImportance.Normal, $"Processing type: {type.FullName}");
                var sqlAttr = type.GetCustomAttribute<ScheModelAttribute>();
                if (sqlAttr != null)
                {  
                    Log.LogMessage(MessageImportance.Normal, $"Processing model: {type.FullName}");
                    try
                    {
                        // Get SQL generator for this type
                        var generator = SqlGeneratorFactory.Build(sqlAttr.SqlImplementation);
                        
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
                        taskItem.SetMetadata("SqlImplementation", sqlAttr.SqlImplementation.ToString());
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
        catch (Exception ex)
        {
            Log.LogError($"Error executing DDLGeneratorTask: {ex.Message}");
            return false;
        }
    }
}
