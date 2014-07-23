// This code was copied from http://stackoverflow.com/a/4587739/173497

namespace ExecuteCommandLineProgram
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    public class CommandLineProgramProcess
    {
        private CommandLineProgramProcess() { }

        /// <summary>
        /// Executes a command-line program, specifying a maximum time to wait
        /// for it to complete.
        /// </summary>
        /// <param name="command">
        /// The path to the program executable.
        /// </param>
        /// <param name="workingDirectory">
        /// The path of the working directory for the process executing the program executable.
        /// </param>
        /// <param name="args">
        /// The command-line arguments for the program.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the subprocess to complete, in milliseconds.
        /// </param>
        /// <returns>
        /// A <see cref="CommandLineProgramProcessResult"/> containing the results of
        /// running the program.
        /// </returns>
        public static CommandLineProgramProcessResult RunProgram(string command, string workingDirectory, string args, int timeout)
        {
            return RunProgram(command, workingDirectory, args, timeout, null);
        }

        /// <summary>
        /// Executes a command-line program, specifying a maximum time to wait
        /// for it to complete.
        /// </summary>
        /// <param name="command">
        /// The path to the program executable.
        /// </param>
        /// <param name="workingDirectory">
        /// The path of the working directory for the process executing the program executable.
        /// </param>
        /// <param name="args">
        /// The command-line arguments for the program.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the subprocess to complete, in milliseconds.
        /// </param>
        /// <param name="input">
        /// Input to be sent via standard input.
        /// </param>
        /// <returns>
        /// A <see cref="CommandLineProgramProcessResult"/> containing the results of
        /// running the program.
        /// </returns>
        public static CommandLineProgramProcessResult RunProgram(string command, string workingDirectory, string args, int timeout, string input)
        {
            bool timedOut = false;
            ProcessStartInfo pinfo = new ProcessStartInfo(command);
            pinfo.Arguments = args;
            pinfo.UseShellExecute = false;
            pinfo.CreateNoWindow = true;
            pinfo.WorkingDirectory = workingDirectory;
            pinfo.RedirectStandardInput = true;
            pinfo.RedirectStandardOutput = true;
            pinfo.RedirectStandardError = true;
            Process process = Process.Start(pinfo);
            ProcessStream processStream = new ProcessStream();
            StreamWriter inputStreamWriter = process.StandardInput;
            Encoding standardInputEncoding = inputStreamWriter.Encoding;

            try
            {
                if (!String.IsNullOrEmpty(input))
                    inputStreamWriter.Write(input);

                inputStreamWriter.Close();
                processStream.Read(process);
                process.WaitForExit(timeout);
                processStream.Stop();

                if (!process.HasExited)
                {
                    // OK, we waited until the timeout but it still didn't exit; just kill the process now
                    timedOut = true;

                    try
                    {
                        process.Kill();
                        processStream.Stop();
                    }
                    catch { }

                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                process.Kill();
                processStream.Stop();
                throw ex;
            }
            finally
            {
                processStream.Stop();
            }

            TimeSpan duration = process.ExitTime - process.StartTime;
            float executionTime = (float)duration.TotalSeconds;

            CommandLineProgramProcessResult result = new CommandLineProgramProcessResult(
                executionTime,
                processStream.StandardOutput.Trim(),
                processStream.StandardError.Trim(),
                process.ExitCode,
                timedOut,
                standardInputEncoding);

            return result;
        }
    }

    /// <summary>
    /// Represents the result of executing a command-line program.
    /// </summary>
    public class CommandLineProgramProcessResult
    {
        readonly float _executionTime;
        private readonly Encoding _standardInputEncoding;
        readonly string _standardOutputString;
        readonly string _standardErrorString;
        readonly int _exitCode;
        readonly bool _hasTimedOut;

        internal CommandLineProgramProcessResult(float executionTime, string stdout, string stderr, int exitCode, bool timedOut, Encoding standardInputEncoding)
        {
            this._executionTime = executionTime;
            this._standardOutputString = stdout;
            this._standardErrorString = stderr;
            this._exitCode = exitCode;
            this._hasTimedOut = timedOut;
            _standardInputEncoding = standardInputEncoding;
        }

        /// <summary>
        /// Gets the total wall time that the subprocess took, in seconds.
        /// </summary>
        public float ExecutionTime
        {
            get { return _executionTime; }
        }

        /// <summary>
        /// Gets the output that the subprocess wrote to its standard output stream.
        /// </summary>
        public string StandardOutput
        {
            get { return _standardOutputString; }
        }

        /// <summary>
        /// Gets the output that the subprocess wrote to its standard error stream.
        /// </summary>
        public string StandardError
        {
            get { return _standardErrorString; }
        }

        public Encoding StandardInputEncoding
        {
            get { return _standardInputEncoding; }
        }

        /// <summary>
        /// Gets the subprocess's exit code.
        /// </summary>
        public int ExitCode
        {
            get { return _exitCode; }
        }

        /// <summary>
        /// Gets a flag indicating whether the subprocess was aborted because it
        /// timed out.
        /// </summary>
        public bool HasTimedOut
        {
            get { return _hasTimedOut; }
        }
    }

    internal class ProcessStream
    {
        /*
         * Class to get process stdout/stderr streams
         * Author: SeemabK (seemabk@yahoo.com)
         * Usage:
            //create ProcessStream
            ProcessStream myProcessStream = new ProcessStream();
            //create and populate Process as needed
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "myexec.exe";
            myProcess.StartInfo.Arguments = "-myargs";

            //redirect stdout and/or stderr
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.RedirectStandardError = true;

            //start Process
            myProcess.Start();
            //connect to ProcessStream
            myProcessStream.Read(ref myProcess);
            //wait for Process to end
            myProcess.WaitForExit();

            //get the captured output :)
            string output = myProcessStream.StandardOutput;
            string error = myProcessStream.StandardError;
         */
        private Thread _standardOutputReaderThread;
        private Thread _standardErrorReaderThread;
        private Process _process;
        private string _standardOutputString = "";
        private string _standardErrorString = "";

        public string StandardOutput
        {
            get { return _standardOutputString; }
        }

        public string StandardError
        {
            get { return _standardErrorString; }
        }

        public ProcessStream()
        {
            Init();
        }

        public void Read(Process process)
        {
            try
            {
                Init();
                _process = process;

                if (_process.StartInfo.RedirectStandardOutput)
                {
                    _standardOutputReaderThread = new Thread(new ThreadStart(ReadStandardOutput));
                    _standardOutputReaderThread.Start();
                }

                if (_process.StartInfo.RedirectStandardError)
                {
                    _standardErrorReaderThread = new Thread(new ThreadStart(ReadStandardError));
                    _standardErrorReaderThread.Start();
                }

                int readTimeout = 1 * 60 * 1000; // one minute

                if (_standardOutputReaderThread != null)
                    _standardOutputReaderThread.Join(readTimeout);

                if (_standardErrorReaderThread != null)
                    _standardErrorReaderThread.Join(readTimeout);

            }
            catch { }
        }

        private void ReadStandardOutput()
        {
            if (_process == null)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();
                string line = null;
                
                while ((line = _process.StandardOutput.ReadLine()) != null)
                {
                    sb.Append(line);
                    sb.Append(Environment.NewLine);
                }
                
                _standardOutputString = sb.ToString();
            }
            catch { }
        }

        private void ReadStandardError()
        {
            if (_process == null)
                return;

            try
            {
                StringBuilder sb = new StringBuilder();
                string line = null;
                
                while ((line = _process.StandardError.ReadLine()) != null)
                {
                    sb.Append(line);
                    sb.Append(Environment.NewLine);
                }

                _standardErrorString = sb.ToString();
            }
            catch { }
        }

        private void Init()
        {
            _standardErrorString = "";
            _standardOutputString = "";
            _process = null;
            Stop();
        }

        public void Stop()
        {
            try {
                if (_standardOutputReaderThread != null)
                    _standardOutputReaderThread.Abort();
            }
            catch { }
            
            try {
                if (_standardErrorReaderThread != null)
                    _standardErrorReaderThread.Abort();
            }
            catch { }

            _standardOutputReaderThread = null;
            _standardErrorReaderThread = null;
        }
    }
}
