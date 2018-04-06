using System;
using System.Configuration;
using System.ServiceModel;
using System.Threading.Tasks;

namespace OperatingSystemAnalyst
{
    public class WindowsService : IService
    {
        private ServiceHost _server = null;
        private string Role;
        private string ServiceName;

        public void Start()
        {
            Console.WriteLine("Started service");
            
            Role = ConfigurationManager.AppSettings["Role"].ToString();
            ServiceName = ConfigurationManager.AppSettings["ServiceName"].ToString();

            try
            {
                OperatingSystemAnalyst kl = new OperatingSystemAnalyst();
                kl.Mode = ScanMode.FILE;
                kl.Enabled = true;
                Console.ReadLine();
                kl.Flush2File();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void _server_Faulted(object sender, EventArgs e)
        {
            Console.WriteLine("Arrêt non prévus du client ...");
        }

        private void _server_Closing(object sender, EventArgs e)
        {
            Console.WriteLine("Fermeture du client ...");
        }

        public void Stop()
        {
            if (this._server != null)
            {
                this._server.Close();
            }
            Console.WriteLine("Stopped service");
        }
    }
}
