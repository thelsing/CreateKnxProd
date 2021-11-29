using System;
using System.Collections.Generic;
using System.IO;

namespace CreateKnxProd
{
    public static class ETSPathHelper
    {
        private static string Programfolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        public static string Combine(params string[] paths)
        {
            var pathElements = new List<string>();
            pathElements.Add(Programfolder);
            pathElements.Add(CheckForETS6());
            pathElements.AddRange(paths);
            return Path.Combine(pathElements.ToArray());
        }


        private static string CheckForETS6()
        {
            if (Directory.Exists(Path.Combine(Programfolder, "ETS6")))
            {
                return "ETS6";
            }
            else
            {
                return "ETS5";
            }
        }
    }
}
