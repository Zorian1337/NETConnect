using NETConnect.Encryption.Crypt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Network
{
    public class SecureSocketWrapper : IDisposable
    {
        private bool disposedValue;

        public Socket Connection { get; private set; }
        public RSAKeySize KeySize { get; private set; }


        public SecureSocketWrapper(Socket Connection, RSAKeySize KeySize) 
        { 
            this.Connection = Connection;
            this.KeySize = KeySize;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~SecureSocketWrapper()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
