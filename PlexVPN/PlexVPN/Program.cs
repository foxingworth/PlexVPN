using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlexVPN
{
    class Program
    {
        static void Main(string[] args)
        {
            // Print information
            FileVersionInfo version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Console.WriteLine(string.Format("PlexVPN\n v{0}.{1}.{2} by foxingworth",
                version.ProductMajorPart, version.ProductMinorPart, version.ProductBuildPart));
            Console.WriteLine("----------------------\n");

            // Find appropriate gateway
            string gateway = string.Empty;
            if (args.Count() == 1)
            {
                gateway = args.First();

                Console.Write(string.Format("Using user-specified gateway {0}", gateway));
            }
            else
            {
                // Find default gateways
                List<string> gateways = new List<string>();
                var ipconfig = RunCommand("ipconfig", string.Empty);

                foreach (string line in ipconfig)
                    if (line.Contains("Default Gateway"))
                        gateways.Add(line.Substring(line.IndexOf(": ") + 2));
                gateways.RemoveAll(x => x.Equals("::"));

                gateway = gateways.First(x => x.StartsWith("1"));
                if (string.IsNullOrWhiteSpace(gateway) && gateways.Count > 0)
                    gateway = gateways.First();

                if (string.IsNullOrWhiteSpace(gateway))
                {
                    Console.WriteLine("Could not find a default gateway! Exiting...");
                    return;
                }

                Console.WriteLine(string.Format("Found {0} default gateway{1}. Using {2}\n",
                    gateways.Count, (gateways.Count != 1) ? "s" : "", gateway));
            }

            // Lookup my.plexapp.com
            IPAddress[] myplex;

            try
            {
                myplex = Dns.GetHostAddresses("my.plexapp.com");
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Could not lookup my.plexapp.com. Reason: {0}", ex.Message));
                return;
            }

            Console.WriteLine("MyPlex current exists at the following addresses:");
            foreach (IPAddress address in myplex)
                Console.WriteLine(string.Format("\t{0}", address));

            // Add routes
            Console.Write("\nAdding routes... ");
            foreach (IPAddress address in myplex)
            {
                var result = RunCommand("route", string.Format("add {0} mask 255.255.255.255 {1}", address, gateway));
                if (result.Count > 0 && result.First().StartsWith("The requested operation requires elevation."))
                {
                    Console.WriteLine("failed!\nPlease run this application as an administrator.");
                    Console.ReadLine();
                }
            }
            Console.WriteLine("done!");
            System.Threading.Thread.Sleep(5000);    // leave time to read in case the user ran this manually
        }

        static List<string> RunCommand(string command, string arguments)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo(command, arguments);

            // Don't show the window
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            // Capture the output
            startInfo.RedirectStandardOutput = true;

            // Run the command
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // Return result
            List<string> ret = new List<string>();
            while (!process.StandardOutput.EndOfStream)
                ret.Add(process.StandardOutput.ReadLine());
            return ret;
        }
    }
}
