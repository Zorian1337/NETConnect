#pragma once

#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>

#pragma comment(lib, "Ws2_32.lib")
typedef SOCKET SocketHandler;

#else // Add custom DEF for either PS4 or linux individually not sure if I need to define two different sets (PS4 should use the same as regular linux)
#include <errno.h> // Linux error reports


#endif

class NetUtil
{
public:
	
	// Returns bool True | False based on if the socket was successfully created.
	// Outputs: Socket for each platform (SOCKET for Windows, int for Linux)
	static bool TryGetSocket(int af, int type, int protocol, SocketHandler& Socket) {
		Socket = CreateSocket(af, type, protocol);
		
		if (Socket == -1) return false;
		else return true;
	}

	// Returns a socket for the current platform (SOCKET for Windows, int for Linux)
	static SocketHandler CreateSocket(int af, int type, int protocol) {
		return socket(af, type, protocol);
	}

	static bool GetIPv4Address(const char* IP, struct in_addr& IPv4) {
		if (inet_pton(AF_INET, IP, &IPv4) != 1) return false;
		else return true;
	}

	static bool GetIPv6Address(const char* IP, struct in6_addr& IPv6) {
		if (inet_pton(AF_INET6, IP, &IPv6) != 1) return false;
		else return true;
	}
};