namespace ScheMigrator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                Console.WriteLine($"Arguments received: {string.Join(" ", args)}");  // Debug logging
                
                var generator = new DDLGenerator();
                var assemblyPaths = new List<string>();
                
                // Parse command line arguments
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-a":
                        case "--assembly":
                            if (i + 1 < args.Length)
                            {
                                assemblyPaths.AddRange(args[++i].Split(';'));
                            }
                            break;
                        case "-s":
                        case "--schemigrator":
                            if (i + 1 < args.Length)
                            {
                                generator.ScheMigratorAssemblyPath = args[++i];
                            }
                            break;
                        case "-o":
                        case "--output":
                            if (i + 1 < args.Length)
                            {
                                generator.OutputPath = args[++i];
                            }
                            break;
                        case "-i":
                        case "--implementation":
                            if (i + 1 < args.Length)
                            {
                                generator.Implementation = args[++i];
                                Console.WriteLine($"Implementation set to: {generator.Implementation}");  // Debug logging
                            }
                            break;
                        case "--schema":
                            if (i + 1 < args.Length)
                            {
                                generator.Schema = args[++i];
                            }
                            break;
                        case "-v":
                        case "--verbose":
                            generator.Verbose = true;
                            break;
                        case "-h":
                        case "--help":
                            PrintHelp();
                            return 0;
                        default:
                            Console.Error.WriteLine($"Unknown argument: {args[i]}");
                            PrintHelp();
                            return 1;
                    }
                }

                // Set the assembly paths after collecting all of them
                generator.AssemblyPaths = assemblyPaths.ToArray();

                // Validate required parameters
                if (generator.AssemblyPaths == null || !generator.AssemblyPaths.Any())
                {
                    Console.Error.WriteLine("Assembly path is required");
                    PrintHelp();
                    return 1;
                }
                if (string.IsNullOrEmpty(generator.ScheMigratorAssemblyPath))
                {
                    Console.Error.WriteLine("ScheMigrator assembly path is required");
                    PrintHelp();
                    return 1;
                }
                if (string.IsNullOrEmpty(generator.OutputPath))
                {
                    Console.Error.WriteLine("Output path is required");
                    PrintHelp();
                    return 1;
                }
                if (string.IsNullOrEmpty(generator.Implementation))
                {
                    Console.Error.WriteLine("Implementation is required");
                    PrintHelp();
                    return 1;
                }

                return generator.Execute() ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Fatal error: {ex.Message}");
                return 1;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Usage: DDLGenerator [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -a, --assembly <path>        Path to the assembly to process");
            Console.WriteLine("  -s, --schemigrator <path>    Path to the ScheMigrator assembly");
            Console.WriteLine("  -o, --output <path>          Output directory for generated files");
            Console.WriteLine("  -i, --implementation <name>  SQL implementation (e.g., PostgreSQL, SQLServer)");
            Console.WriteLine("  --schema <name>              Database schema name (default: public)");
            Console.WriteLine("  -v, --verbose               Enable verbose logging");
            Console.WriteLine("  -h, --help                  Show this help message");
        }
    }
}