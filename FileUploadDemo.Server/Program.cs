using System;
using System.ServiceModel;

namespace FileUploadDemoServer
{
    class Program
    {
        private static ServiceHost s_serviceHost;

        static void Main(string[] args)
        {
            try
            {
                if (s_serviceHost != null)
                    s_serviceHost.Close();
                s_serviceHost = new ServiceHost(typeof(FileUploadService));
                s_serviceHost.Open();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error opening Service Host:\n{0}", ex.Message);
            }

            Console.WriteLine("Service is running, press ENTER to shut down...");

            Console.ReadLine();

            Console.WriteLine("Shutting down...");
            
            if (s_serviceHost != null)
            {
                s_serviceHost.Close();
                s_serviceHost = null;
            }

            Console.WriteLine("Stopped.");
        }
    }
}
