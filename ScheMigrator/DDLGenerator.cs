using System;
using Microsoft.Build.Framework;

namespace ScheMigrator;

public class DDLGenerator : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem[] CompileFiles { get; set; }

    [Required]
    public string AttributeName { get; set; }

    [Output]
    public ITaskItem[] ProcessedFiles { get; set; }

    public override bool Execute()
    {
        var processedFiles = new List<ITaskItem>();

        foreach (var file in CompileFiles)
        {
            Log.LogMessage(MessageImportance.Normal, $"Processing file: {file.ItemSpec}");
            
            var sourceText = File.ReadAllText(file.ItemSpec);
            var tree = CSharpSyntaxTree.ParseText(sourceText);
            var root = tree.GetRoot();

            // Find all class declarations
            var classDeclarations = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classDeclarations)
            {
                // Check if the class has our target attribute
                if (HasTargetAttribute(classDeclaration, AttributeName))
                {
                    ProcessClass(classDeclaration);
                    
                    var taskItem = new TaskItem(file.ItemSpec);
                    taskItem.SetMetadata("ProcessedClass", classDeclaration.Identifier.Text);
                    taskItem.SetMetadata("Namespace", GetNamespace(classDeclaration));
                    processedFiles.Add(taskItem);
                }
            }
        }

        ProcessedFiles = processedFiles.ToArray();
        return !Log.HasLoggedErrors;
    }

    private bool HasTargetAttribute(ClassDeclarationSyntax classDeclaration, string attributeName)
    {
        return classDeclaration.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr => attr.Name.ToString() == attributeName ||
                        attr.Name.ToString() == attributeName + "Attribute");
    }

    private string GetNamespace(ClassDeclarationSyntax classDeclaration)
    {
        // Walk up the syntax tree to find the namespace
        var namespaceDeclaration = classDeclaration.Parent as NamespaceDeclarationSyntax;
        return namespaceDeclaration?.Name.ToString() ?? string.Empty;
    }

    private void ProcessClass(ClassDeclarationSyntax classDeclaration)
    {
        // Add your custom processing logic here
        // You have access to the full syntax tree for analysis
        
        // Example: Process methods with specific attributes
        var methods = classDeclaration.DescendantNodes()
            .OfType<MethodDeclarationSyntax>();
            
        foreach (var method in methods)
        {
            Log.LogMessage(MessageImportance.Normal, 
                $"Found method {method.Identifier.Text} in class {classDeclaration.Identifier.Text}");
            // Add your method processing logic here
        }
    }
}
