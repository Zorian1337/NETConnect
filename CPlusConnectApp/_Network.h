// Header file for Network headers I will need

#pragma once

#include "NetUtil.h" // Standard Network utilities

#ifdef _WIN32
#include <winsock2.h> // Windows Sockets API
#include <ws2tcpip.h> // TCP/IP extensions for Windows Sockets

//#include <iostream>   // Standard I/O (I dont think I really need this globally)

#pragma comment(lib, "Ws2_32.lib")
typedef SOCKET SocketHandler;
const SocketHandler INVALID_SOCKET_HANDLE = INVALID_SOCKET;
#endif

#ifdef _PS4
// Includes only networking related headers for PS4, not sure if these are all the ones I need yet (probably not)
#include "netinet/in.h" // sockaddr_in and related structures
#include "arpa/inet.h" // inet_pton and related functions

typedef int SocketHandler; // Linux Filesystem uses int for sockets
const SocketHandler INVALID_SOCKET_HANDLE = -1;
#endif