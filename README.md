## Overview of the ExecuteCommandLineProgram Library

ExecuteCommandLineProgram is a .NET (4) class library for executing command line programs.

The code was copied and slightly modified from [this answer](http://stackoverflow.com/a/4587739/173497) 
to the Stack Overflow question
[How to capture Shell command output in C#?](http://stackoverflow.com/questions/4587415/how-to-capture-shell-command-output-in-c).

[This answer](http://stackoverflow.com/a/3308697/173497) to the Stack Overflow question
[run interactive command line exe using c#](http://stackoverflow.com/questions/3308500/run-interactive-command-line-exe-using-c-sharp)
provided the necessary resource info for me to add support for supporting standard-input input.

Example usage:

    CommandLineProgramProcessResult result =
        CommandLineProgramProcess.RunProgram(
            @"C:\Program Files (x86)\SomeFolder\SomeProgram.exe",             // Path of executable program
            @"C:\Program Files (x86)\SomeFolder\",                            // Path of working directory
            String.Format(@"""{0}""", filePathThatNeedsToBeQuotedArgument),   // Command line arguments
            10 * 60 * 1000);                                                  // Timeout, in milliseconds
            
    string standardError = result.StandardError;
    string standardOutput = result.StandardOutput;
    int exitCode = result.ExitCode;

Example usage for supplying input via standard input:

    CommandLineProgramProcessResult result =
        CommandLineProgramProcess.RunProgram(
            @"C:\Program Files (x86)\SomeFolder\SomeProgram.exe",             // Path of executable program
            @"C:\Program Files (x86)\SomeFolder\",                            // Path of working directory
            String.Format(@"""{0}""", filePathThatNeedsToBeQuotedArgument),   // Command line arguments
            10 * 60 * 1000,                                                   // Timeout, in milliseconds
            inputData);                                                       // Standard-input input, as a string
            
    string standardError = result.StandardError;
    string standardOutput = result.StandardOutput;
    int exitCode = result.ExitCode;
