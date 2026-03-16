// Header file for Network headers I will need

#pragma once

//#include "NetUtil.h" // Cannot include this here as it causes a circular dependency issue

#ifdef _WIN32
#include <winsock2.h> // Windows Sockets API
#include <ws2tcpip.h> // TCP/IP extensions for Windows Sockets
#pragma comment(lib, "Ws2_32.lib")

// Iphlpapi is used for getting local IP address and other network related information on Windows
#include <iphlpapi.h> // apprently this is needed for the linker as pragma once wont work "-liphlpapi"
#pragma comment(lib, "iphlpapi.lib")

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

#ifdef linux // this is just a placeholder for now, I will need to figure out the correct preprocessor directive for linux and ps4 later
#include <ifaddrs.h> // use for ps4 but needs to be tested on ps4
#endif