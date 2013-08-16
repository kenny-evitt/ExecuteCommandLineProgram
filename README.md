## Overview of the ExecuteCommandLineProgram Library

ExecuteCommandLineProgram is a .NET (4.5) class library for executing command line programs.

Example usage:

	CommandLineProgramProcessResult result =
		CommandLineProgramProcess.RunProgram(
			@"C:\Program Files (x86)\SomeFolder\SomeProgram.exe",				// Path of executable program
			@"C:\Program Files (x86)\SomeFolder\",								// Path of working directory
			String.Format(@"""{0}""", filePathThatNeedsToBeQuotedArgument),		// Command line arguments
			10 * 60 * 1000);													// Timeout, in milliseconds
			
	string standardError = result.StandardError;
	string standardOutput = result.StandardOutput;
	int exitCode = result.ExitCode;