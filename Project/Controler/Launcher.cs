using System;
using System.Configuration;
using Topshelf;

namespace OperatingSystemAnalyst
{
    public class Launcher
    {
        #region Attributes
        private Host host;
        #endregion

        #region Constructor
        public Launcher()
        {

        }
        #endregion

        #region Methods publics
        /// <summary>
        /// Configure and launch the windows service
        /// </summary>
        public void Launch()
        {
            this.host = HostFactory.New(x =>
            {
                x.Service<IService>(s =>
                {
                    s.ConstructUsing(name => new WindowsService());
                    s.WhenStarted(td => td.Start());
                    s.WhenStopped(td => td.Stop());
                    //s.WhenShutdown(td => td.Shutdown());                    
                });

                //x.StartAutomatically();
                x.RunAsLocalSystem();
                //x.SetInstanceName(ConfigurationManager.AppSettings["InstanceName"].ToString());
                x.SetDisplayName(ConfigurationManager.AppSettings["ServiceDisplayName"].ToString());
                x.SetServiceName(ConfigurationManager.AppSettings["ServiceName"].ToString());
                x.SetDescription(ConfigurationManager.AppSettings["ServiceDescription"].ToString());
            });

            this.host.Run();
        }

        /// <summary>
        /// Dispose the service host
        /// </summary>
        public void Dispose()
        {
            if (this.host != null && this.host is IDisposable)
            {
                (this.host as IDisposable).Dispose();
                this.host = null;
            }
        }
        #endregion
    }
}
