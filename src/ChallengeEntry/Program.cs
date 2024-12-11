// See https://aka.ms/new-console-template for more information
using ChallengeEntry.Solutions;
using TestEntry.Solutions;

var parsedArguments = ParseArguments(args);

switch(Convert.ToInt32(parsedArguments[CommandLineArgument.solutionToStart]))
{
    case 2: // Latest and greatest - fastest solution
        Solution2.DataPath = Convert.ToString(parsedArguments[CommandLineArgument.dataFilePath])!;
        Solution2.ThreadCount = Convert.ToInt32(parsedArguments[CommandLineArgument.threadsToUse]);
        Solution2.ReadBufferInMb = Convert.ToInt32(parsedArguments[CommandLineArgument.bufferSize]);

        await Solution2.Main(args);

        break;

    case 3: // Dictionary collisions measuring
        Solution3.DataPath = Convert.ToString(parsedArguments[CommandLineArgument.dataFilePath])!;
        Solution3.ThreadCount = Convert.ToInt32(parsedArguments[CommandLineArgument.threadsToUse]);
        Solution3.ReadBufferInMb = Convert.ToInt32(parsedArguments[CommandLineArgument.bufferSize]);

        await Solution3.Main(args);

        break;
}

Console.WriteLine($"Run test with params: {string.Join(' ', parsedArguments.Select(keyPair => $"{keyPair.Key}={keyPair.Value}"))}");

static Dictionary<CommandLineArgument, object> ParseArguments(string[] args)
{
    var arguments = new Dictionary<CommandLineArgument, object>();

    foreach (var arg in args)
    {
        string[] argParts = arg.Split('=');

        if (argParts[0].Trim().ToLower() == "-t")
            arguments.Add(CommandLineArgument.threadsToUse, argParts[1]);
        if (argParts[0].Trim().ToLower() == "-b")
            arguments.Add(CommandLineArgument.bufferSize, argParts[1]);
        if (argParts[0].Trim().ToLower() == "-dp")
            arguments.Add(CommandLineArgument.dataFilePath, argParts[1]);

        if (argParts[0].Trim().ToLower() == "-s")
            arguments.Add(CommandLineArgument.solutionToStart, argParts[1]);
    }

    ApplyDefaultValues(arguments);

    return arguments;
}

static void ApplyDefaultValues(Dictionary<CommandLineArgument, object> parsedArgs)
{
    if (!parsedArgs.ContainsKey(CommandLineArgument.threadsToUse))
        parsedArgs.Add(CommandLineArgument.threadsToUse, DefaultValues.ThreadToUse);
    if (!parsedArgs.ContainsKey(CommandLineArgument.bufferSize))
        parsedArgs.Add(CommandLineArgument.bufferSize, DefaultValues.BufferSizeInMB);
    if (!parsedArgs.ContainsKey(CommandLineArgument.solutionToStart))
        parsedArgs.Add(CommandLineArgument.solutionToStart, DefaultValues.SolutionToStart);
    if (!parsedArgs.ContainsKey(CommandLineArgument.dataFilePath))
        parsedArgs.Add(CommandLineArgument.dataFilePath, DefaultValues.DataFilePath);

    if (Convert.ToInt32(parsedArgs[CommandLineArgument.threadsToUse]) == 0)
    {
        parsedArgs[CommandLineArgument.threadsToUse] = Environment.ProcessorCount;
    }
}

enum CommandLineArgument
{
    threadsToUse = 1,
    bufferSize = 2,
    dataFilePath = 3,

    solutionToStart = 4,
}

static class DefaultValues
{
    public static int ThreadToUse = 1;
    public static int BufferSizeInMB = 8;
    public static int SolutionToStart = 2;

    //    public static string DataFilePath = @"C:\_OneBRecord\data\measurements.txt";
    public static string DataFilePath = @".\..\..\..\data\Measurements.txt";
}
