#pragma once

#include "_Debugging.h"
#include "_Network.h" // Include common network headers, and defines SocketHandler for each platform



class NetUtil
{
public:
	enum class IPType { None, IPv4, IPv6 };

	struct IPAddress {
		IPType type;
		union { // need to use more unions** - will need to return as sockaddr_storage either that or just have it in the connection areas specifically
			in_addr v4;
			in6_addr v6;
		};

		IPAddress() : type(IPType::None) {}
	};

	// Returns bool True | False based on if the port string was successfully parsed and valid.
	static bool TryParsePort(const char* PortStr, int& Port) {
		Port = ParsePort(PortStr);

		if (Port != -1) return true;
		else return false;
	}

	// Parses a port number from a string. Returns the port number if valid, or -1 if invalid.
	static int ParsePort(const char* Port) {
		return -1; // Implement this later, for now just return -1 to indicate invalid port
	}

	// Checks if port is within valid range (1-65535). Returns true if valid, false otherwise.
	static bool IsValidPort(int Port){
		return Port > 0 && Port <= 65535;
	}

	static IPAddress ParseIP(const char* IP) {
		IPAddress result;

		// Try IPv4
		if (inet_pton(AF_INET, IP, &result.v4) == 1) {
			result.type = IPType::IPv4;
			return result;
		}

		// Try IPv6
		if (inet_pton(AF_INET6, IP, &result.v6) == 1) {
			result.type = IPType::IPv6;
			return result;
		}

		return result; // type = None
	}

	static bool GetIPv4Address(const char* IP, in_addr& addr) {
		if (inet_pton(AF_INET, IP, &addr) != 1) return false;
		else return true;
	}

	static bool GetIPv6Address(const char* IP, struct in6_addr& IPv6) {
		if (inet_pton(AF_INET6, IP, &IPv6) != 1) return false;
		else return true;
	}


	// Returns bool True | False based on if the socket was successfully created.
	// Outputs: Socket for each platform (SOCKET for Windows, int for Linux)
	static bool TryCreateSocket(int af, int type, int protocol, SocketHandler& Socket) {
		#ifdef _WIN32
				WSADATA wsa;
				if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) {
					Debugger::WriteLine("WSAStartup failed\n");
					return false;
				}
		#endif

		Socket = CreateSocket(af, type, protocol);
		
		if (Socket == -1) return false;
		else return true;
	}

	// Returns a socket for the current platform (SOCKET for Windows, int for Linux)
	static SocketHandler CreateSocket(int af, int type, int protocol) {
		return socket(af, type, protocol);
	}


};