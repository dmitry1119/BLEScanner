﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BLEDriverInfPatch
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: BLEDriverInfPatch <ComputerName> <DevicePID> <DeviceVID>");
                return 1;
            }
            var computerName = args[0];
            var deviceVid = args[1];
            var devicePid = args[2];

            var sourceInfPathPatterns = new[] {
                new { Path=@"\C$\Windows\Inf\BTH_MC.inf", DeviceName="BthUsb" },
                new { Path=@"\C$\Windows\Inf\BTH.inf", DeviceName="BthUsbGeneric" },
            };

            var sourceInfPathPattern = sourceInfPathPatterns.FirstOrDefault(pattern => File.Exists(@"\\" + computerName + pattern.Path));
            if( sourceInfPathPattern == null)
            {
                Console.Error.WriteLine("Error: Can not access to the source INF file. Please re-check the ComputerName is correct.");
                return 1;
            }

            var sectionPattern = new Regex(@"\[([^\]]+)\]");
            var valuePattern = new Regex(@"([^=]+)=(.*)");

            var sectionsToIgnoreContents = new[]
            {
                "SourceDisksNames",
                "SourceDisksFiles",
                "DestinationDirs",
            };

            using (var reader = new StreamReader(sourceInfPathPattern.Path))
            using(var writer = new StreamWriter("BCM20702.inf"))
            {
                var currentSection = "";
                foreach (var line in reader.ReadLines())
                {
                    var sectionMatch = sectionPattern.Match(line);
                    if (sectionMatch.Success)
                    {
                        currentSection = sectionMatch.Groups[1].Value;
                        if (currentSection.EndsWith(".Copy") ||
                            currentSection.EndsWith(".CopyFiles") ||
                            currentSection.EndsWith(".CopyFilesOnly"))
                        {
                            // Skip any .Copy section
                        }
                        else
                        {
                            writer.WriteLine(line);
                        }
                        // Add target adapter device ID to GenericAdapter section.
                        if (currentSection == "GenericAdapter.NTarm")
                        {
                            writer.WriteLine($@"Generic Bluetooth Adapter={sourceInfPathPattern.DeviceName}, USB\Vid_{deviceVid}&Pid_{devicePid}");
                        }
                    }
                    else
                    {
                        if (currentSection.EndsWith(".Copy") ||
                            currentSection.EndsWith(".CopyFiles") ||
                            currentSection.EndsWith(".CopyFilesOnly") ||
                            sectionsToIgnoreContents.Contains(currentSection))
                        {
                            // Skip any .Copy section contents
                        }
                        else
                        {
                            var valueMatch = valuePattern.Match(line);
                            if (valueMatch.Success && valueMatch.Groups[1].Value == "CopyFiles")
                            {
                                // Skip a CopyFiles value.
                            }
                            else
                            {
                                writer.WriteLine(line);
                            }
                        }
                    }
                    
                }
            }

            return 0;
        }
    }

    static class TextReaderExtensions
    {
        public static IEnumerable<string> ReadLines(this TextReader self)
        {
            while (true)
            {
                var line = self.ReadLine();
                if (line == null)
                {
                    break;
                }
                yield return line.TrimEnd();
            }
        }
    }
}
