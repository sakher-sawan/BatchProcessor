using System;
using System.Runtime.InteropServices;


namespace BatchProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation
                                                  .IsOSPlatform(OSPlatform.Windows);

            


            string basePath = args[0] ?? (isWindows ? "F:\\" : "/mnt/f/");



        }
    }
}