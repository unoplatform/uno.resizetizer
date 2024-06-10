using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Uno.Resizetizer
{
    internal class FileHelper
    {
        public static void WriteFileIfChanged(string fileName, TaskLoggingHelper log, Action<StreamWriter> action)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                using (var writer = File.CreateText(tempFile))
                {
                    action(writer);
                }

                if (!File.Exists(fileName)
                    || !File.ReadAllText(fileName).Equals(File.ReadAllText(tempFile)))
                {
                    if (File.Exists(fileName))
                    {
                        log.LogMessage(MessageImportance.Low, $"Updating file: {fileName}");
                        File.Delete(fileName);
                    }

                    File.Move(tempFile, fileName);
                }
                else
                {
                    log.LogMessage(MessageImportance.Low, $"Skipping unchanged {fileName}");
                }

            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch
                {
                    // ignore errors
                }
            }
        }
    }
}
