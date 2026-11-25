using System;
using System.Collections.Generic;
using System.Text;

namespace NETConnect.Shared.Platform;

[Flags]
public enum PeerPlatform : uint
{
    Unknown = 0,
    Windows = 1 << 0,
    MacOS = 1 << 1,
    Linux = 1 << 2,   
    SteamOS = 1 << 3,   
    Android = 1 << 4,
    IOS = 1 << 5,
    PlayStation = 1 << 6,
    Xbox = 1 << 7,
    NintendoSwitch = 1 << 8,
    Esp = 1 << 9,
    Other = 1 << 10,
}