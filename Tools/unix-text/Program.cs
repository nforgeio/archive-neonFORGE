﻿//-----------------------------------------------------------------------------
// FILE:	    Program.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Neon;
using Neon.Common;

namespace UnixText
{
    /// <summary>
    /// Converts a Windows text file into one suitable for consumption on a Unix
    /// or Linux machine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Examines one or more text files specified on the command line for Unicode
    /// UTF-8 byte order markers as well as Windows style CR-LF line endings.  If any
    /// of these are present, the file will be rewritten stripping out the byte
    /// order markers (BOM) and converting to Unix style (LF only) line endings 
    /// with the file being generated using the UTF-8 encoding.
    /// </para>
    /// <para>
    /// When no files are specified, the tool simply process standard input and
    /// writes the converted text to standard output.
    /// </para>
    /// <para>
    /// https://en.wikipedia.org/wiki/Byte_order_mark
    /// </para>
    /// <para>
    /// Usage:
    /// </para>
    /// <code language="none">
    /// unix-text [OPTIONS] [FILE...]
    /// </code>
    /// <note>
    /// File name wildcards may be specified.
    /// </note>
    /// <para>
    /// The <b>-r</b> option specifies that folders should be walked recursively
    /// to process any matching files.
    /// </para>
    /// </remarks>
    public static class Program
    {
        /// <summary>
        /// Tool version number.
        /// </summary>
        public const string Version = "1.0";

        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">The list of files to be processed with optional wildcards.</param>
        public static void Main(string[] args)
        {
            var commandLine = new CommandLine(args);

            commandLine.DefineOption("-r", "--recursive");

            if (commandLine.HasHelpOption ||
                commandLine.Arguments.Length == 0 ||
                commandLine.Arguments.Contains("help", StringComparer.OrdinalIgnoreCase) ||
                commandLine.Arguments.Contains("--help", StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine(
$@"
Neon Linux Text File Converter: unix-text [v{Version}]
{Build.Copyright}

USAGE: unix-text [OPTIONS] FILE...  - Converts one or more files in place
       unix-text [OPTIONS] -        - Converts STDIN to STDOUT
       unix-text help   

OPTIONS:
    
    -r, --recursive     Process folders recursively
    --help              Print usage

DESCRIPTION:

You may pass one or more files or folders to convert files
in place.  Standard input will be processed to standard
output when no files are specified.

Converts a text file into a Unix/Linux compatible form by:

    * Removing Unicode Byte Order Markers from the beginning of 
      the file if present

    * Converting CR-LF line endings to just LF

    * Converting TABs into whitespace using 4 character
      tab stops.

");
                Program.Exit(0);
            }

            if (commandLine.Arguments[0] == "-")
            {
                // Process standard input.

                ProcessInput();
                Program.Exit(0);
            }

            // Process files

            var searchOptions = commandLine.GetOption("--recursive") == null ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            Console.WriteLine(string.Empty);

            try
            {
                var updateCount = 0;

                foreach (var arg in commandLine.Arguments)
                {
                    var path = Path.GetDirectoryName(arg);
                    var file = Path.GetFileName(arg);

                    if (path == string.Empty)
                    {
                        path = ".";
                    }

                    foreach (var item in Directory.EnumerateFiles(path, file, searchOptions))
                    {
                        if (ProcessFile(item))
                        {
                            updateCount++;
                            Console.WriteLine($"Updated: {Path.GetFullPath(item)}");
                        }
                    }

                    if (updateCount > 0)
                    {
                        Console.WriteLine($"Updated [{updateCount}] files.");
                    }
                }

                Program.Exit(0);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{NeonHelper.ExceptionError(e)}");
                Console.Error.WriteLine(string.Empty);

                Program.Exit(1);
            }
        }

        /// <summary>
        /// Exits the program returning the specified process exit code.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public static void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Process the standard input and writes the result to standard output.
        /// </summary>
        private static void ProcessInput()
        {
            // Read the file line-by-line and generate an in-memory byte representation
            // without the BOM and with Unix style line endings.

            using (var inputStream = Console.OpenStandardInput())
            {
                using (var reader = new StreamReader(inputStream))
                {
                    foreach (var line in reader.Lines())
                    {
                        Console.Write(NeonHelper.ExpandTabs(line, 4));
                        Console.Write('\n');
                    }
                }
            }
        }

        /// <summary>
        /// Processes a file by removing the Unicode byte order mark, if present, and converting
        /// CR-LF sequences into LF, if there are any.
        /// </summary>
        /// <param name="path">Path of the file to be processed.</param>
        /// <returns><c>true</c> if the file needed to be modified.</returns>
        private static bool ProcessFile(string path)
        {
            // Load the file into memory as unmodified bytes.

            var inputBytes = File.ReadAllBytes(path);

            // Read the file line-by-line and generate an in-memory byte representation
            // without the BOM and with Unix style line endings.

            using (var output = new MemoryStream(1024 * 1024))
            {
                using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read), true))
                {
                    foreach (var line in reader.Lines())
                    {
                        output.Write(Encoding.UTF8.GetBytes(NeonHelper.ExpandTabs(line, 4)));
                        output.WriteByte((byte)'\n');
                    }
                }

                // Compare the input bytes with the output and update the file if 
                // the bytes differ.

                var outputBytes = output.ToArray();

                if (outputBytes.Length != inputBytes.Length)
                {
                    File.WriteAllBytes(path, outputBytes);
                    return true;
                }

                for (int i = 0; i < outputBytes.Length; i++)
                {
                    if (inputBytes[i] != outputBytes[i])
                    {
                        File.WriteAllBytes(path, outputBytes);
                        return true;
                    }
                }
            }

             return false;
        }
    }
}
