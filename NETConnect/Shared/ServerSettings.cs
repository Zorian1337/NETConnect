using NETConnect.Shared.Packet.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETConnect.Shared
{
    /// <summary>
    /// 
    /// </summary>
    public class ServerSettings
    {
        public bool IsAuthenticated = false;

        // THIS WAS ERRORED BECAUSE OF MISSING TYPE REFERENCE, BUT I ASSUME IT WAS NEVER USED ANYWAY
        //public EncryptionType KeysExcangedSuccessfully { get; set; }


        /// <summary>
        /// This controls all encryption, if set to false, its off everywhere.
        /// </summary>
        public bool IsUsingEncryption = false;

    }
}
