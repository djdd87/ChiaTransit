using IOExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChiaTransit
{
    class Program
    {
        private static string[] _args;

        private const int COLUMN_PIPE_WIDTH = 2;
        private const int COLUMN_OUTSTANDING_WIDTH = 105;
        private const int COLUMN_STATUS_WIDTH = 15;
        private const int COLUMN_PROGRESS_WIDTH = 10;

        private const string FILE_EXTENSION_PLOT = ".plot";
        private const string FILE_EXTENSION_SUFFIX_TMP = ".tmp";

        private const string ARG_DIRECTORY_DESTINATION = "--d";
        private const string ARG_DIRECTORY_DESTINATION_LONG = "--destination";
        private const string ARG_DIRECTORY_SOURCE = "--s";
        private const string ARG_DIRECTORY_SOURCE_LONG = "--source";

        private static BackgroundWorker _worker;

        private static string[] _sourceDirectories;
        private static string[] _destinationDirectories;

        private static int _nextDirectory = -1;

        private static string _separator;
        private static string _empty;
        private static string _outstandingHeader;
        private static string _completeHeader;

        private static string _activeFile;
        private static double _progress;
        private static Dictionary<string, TimeSpan> _completeFiles = new();

        private static readonly CancellationTokenSource cancellationToken = new();

        static async Task Main(string[] args)
        {
            _args = args;

            // Get the souce and destination directories
            _sourceDirectories = GetDirectories("Source", ARG_DIRECTORY_SOURCE, ARG_DIRECTORY_SOURCE_LONG);
            _destinationDirectories = GetDirectories("Destination", ARG_DIRECTORY_DESTINATION, ARG_DIRECTORY_DESTINATION_LONG);

            if (_sourceDirectories == null || _sourceDirectories.Length == 0)
            {
                return;
            }
            if (_destinationDirectories == null || _destinationDirectories.Length == 0)
            {
                return;
            }
            else if (_sourceDirectories.Any(x=> _destinationDirectories.Contains(x, StringComparer.OrdinalIgnoreCase)))
            {
                Console.WriteLine("The source and destination directories cannot be the same.");
                return;
            }

            // Configure the rows
            int totalColumnWidth = COLUMN_OUTSTANDING_WIDTH + COLUMN_PROGRESS_WIDTH + COLUMN_STATUS_WIDTH;
            _separator = " " + new string('#', totalColumnWidth + 1);
            _empty = new string(' ', COLUMN_PIPE_WIDTH) + "empty".PadRight(totalColumnWidth);
            _outstandingHeader = new string(' ', COLUMN_PIPE_WIDTH) + "outstanding".PadRight(COLUMN_OUTSTANDING_WIDTH) + "status".PadRight(COLUMN_STATUS_WIDTH) + "progress".PadRight(COLUMN_STATUS_WIDTH); 
            _completeHeader = new string(' ', COLUMN_PIPE_WIDTH) + "completed".PadRight(COLUMN_OUTSTANDING_WIDTH) + "status".PadRight(COLUMN_STATUS_WIDTH) + "time".PadRight(COLUMN_STATUS_WIDTH);

            // Configure the background worker
            _worker = new();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += _worker_DoWork;

            // Make the window nicer for Windows users
            if (OperatingSystem.IsWindows())
            {
                Console.SetWindowSize(totalColumnWidth + (COLUMN_PIPE_WIDTH * 4), 30);
            }

            // Add cancel handling
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                _worker.CancelAsync();
                Thread.Sleep(1000);
                Console.Clear();
                Console.WriteLine("Cancelling");
                cancellationToken.Cancel();
                eventArgs.Cancel = true;
            };

            await Worker();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Gets the directory from the command line arguments.
        /// </summary>
        /// <param name="friendlyName">The friendly name of the directory to fetch (used for logging).</param>
        /// <param name="args">The args to find the directory.</param>
        /// <returns>The valid directory path, or null if not specified or not found.</returns>
        private static string[] GetDirectories(string friendlyName, params string[] args)
        {
            string[] directories = GetDirectoryList(args); 
            if (directories.Length == 0)
            {
                Console.WriteLine($"No directories specified for '{friendlyName}'.");
                return null;
            }

            foreach (string directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Console.WriteLine($"{friendlyName} directory not found.");
                    return null;
                }
            }

            return directories;
        }

        /// <summary>
        /// Gets the argument for the specified --config value.
        /// </summary>
        /// <param name="configs">A list of config values to pull from the args, e.g. --source.</param>
        /// <returns>The argument or null if not found.</returns>
        private static string[] GetDirectoryList(params string[] configs)
        {
            List<string> results = new();

            bool found = false;
            foreach (string config in configs)
            {
                for (int i = 0; i < _args.Length; i++)
                {
                    string arg = _args[i];
                    if (arg.Equals(config, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                    }
                    else if (found)
                    {
                        if (arg.StartsWith("--"))
                        {
                            // We've hit a new argument, so exit
                            break;
                        }
                        else
                        {
                            results.Add(arg.Trim());
                        }
                    }
                }
            }
            return results.ToArray();
        }

        async static Task Worker()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Get all the files in the source directory
                StringBuilder output = new();

                string[] files = GetSourceFiles();
                output.AppendLine(_separator);
                output.AppendLine(_outstandingHeader);
                output.AppendLine(_separator);

                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        if (_activeFile == null)
                        {
                            _activeFile = file;
                            _progress = 0;
                            _worker.RunWorkerAsync();
                        }

                        string fileName = Path.GetFileName(file);
                        if (Path.GetExtension(fileName).Equals(".plot", StringComparison.OrdinalIgnoreCase))
                        {
                            bool isActive = file.Equals(_activeFile, StringComparison.OrdinalIgnoreCase);
                            string status = isActive ? "moving" : "queued";
                            string progress = isActive ? _progress.ToString("0.0") + "%" : "-";
                            WriteRow(output, fileName, status, progress);
                        }
                    }
                }
                else
                {
                    output.AppendLine(_empty);
                }

                output.AppendLine(new string(' ', _separator.Length));

                output.AppendLine(_separator);
                output.AppendLine(_completeHeader);
                output.AppendLine(_separator);

                if (_completeFiles.Count > 0)
                {
                    foreach (var file in _completeFiles)
                    {
                        WriteRow(output, file.Key, "success", file.Value.ToString("hh\\:mm\\:ss"));
                    }
                }
                else
                {
                    output.AppendLine(_empty);
                }

                Console.SetCursorPosition(0, 0);
                Console.Write(output.ToString());
                Console.SetCursorPosition(0, 0);
                Console.CursorVisible = false;

                await Task.Delay(2000);
            }
        }

        /// <summary>
        /// Gets all the files in the specified source directories.
        /// </summary>
        /// <returns>A list of valid plot files.</returns>
        private static string[] GetSourceFiles()
        {
            List<string> results = new();
            foreach (string source in _sourceDirectories)
            {
                results.AddRange(Directory.GetFiles(source));
            }
            return results.Where(x => Path.GetExtension(x) == FILE_EXTENSION_PLOT).ToArray();
        }

        private static void WriteRow(StringBuilder output, string fileName, string status, string progress)
        {
            int standardFileNameLength = 95;
            if (fileName.Length > standardFileNameLength)
            {
                // Reduce the filename down to the expected size
                fileName = fileName.Substring(0, 95);
            }

            // Indent the filename and make it the width of the first column
            fileName = fileName.PadLeft(standardFileNameLength + 2, ' ').PadRight(COLUMN_OUTSTANDING_WIDTH + COLUMN_PIPE_WIDTH);
        
            // Get the required spacing for the status
            status = status.PadRight(COLUMN_STATUS_WIDTH, ' ');

            progress = progress.PadRight(COLUMN_PROGRESS_WIDTH, ' ');
            output.AppendLine($"{fileName}{status}{progress}");
        }

        private static void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Add a slight delay
                Task.WaitAll(Task.Delay(5000));

                // Get the destination
                string targetDestination = CycleDestinationDirectory();

                // Copy the file to the destination (appending .tmp to prevent issues with the farmer picking it up too early and classing it as invalid)
                string fileName = Path.GetFileName(_activeFile);
                string finalDestination = Path.Combine(targetDestination, fileName);
                string destination = finalDestination + FILE_EXTENSION_SUFFIX_TMP;

                Stopwatch stopwatch = Stopwatch.StartNew();

                FileTransferManager.MoveWithProgress(_activeFile, destination, (progress) =>
                {
                    _progress = Math.Round(progress.Percentage, 1, MidpointRounding.AwayFromZero);
                }, cancellationToken.Token);

                stopwatch.Stop();

                // Remove the tmp suffix
                if (!e.Cancel && !cancellationToken.IsCancellationRequested)
                {
                    File.Move(destination, finalDestination);
                    _completeFiles.Add(fileName, stopwatch.Elapsed);
                    _progress = 0;
                    _activeFile = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Moves to the next target directory.
        /// </summary>
        /// <returns>The destination directory to copy to.</returns>
        private static string CycleDestinationDirectory()
        {
            if (_nextDirectory == -1 || _destinationDirectories.Length == 1)
            {
                // This is the first iteration, or there's no need to cycle
                _nextDirectory = 0;
            }
            else
            {
                _nextDirectory++;
                if (_nextDirectory > _destinationDirectories.Length - 1)
                {
                    // We've gone through all destination directories, so go back to the beginning.
                    _nextDirectory = 0;
                }
            }

            return _destinationDirectories[_nextDirectory];
        }
    }
}
