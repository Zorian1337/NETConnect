#pragma once

#include "_Debugging.h"
#include "_Network.h" // Include common network headers, and defines SocketHandler for each platform
#include <thread>
#include "guid.hpp"
#include "ThreadPool.h"

// cant use this here due to circular dependency issues smh c++ is getting on my nerves
#include "Multicast.h"
#include "TCPServer.h"
#include "TCPClient.h"
#include "UDPServer.h" // Generate UDPClient that can connect directly to UDPServer

//class Multicast;
//class UDPServer;
//class TCPServer;
//class TCPClient;



class Node
{
private:
	std::thread multicastThread;
	bool multicastRunning = false;
	ThreadPool threadPool;

public:
	Node() : threadPool(std::thread::hardware_concurrency()) 
	{
	
	}

	ThreadPool& GetThreadPool() { return threadPool; }


	xg::Guid PeerId = xg::newGuid(); // Needs to defined per platform in preprocessers (current: GUID_WINDOWS=1)
	Multicast LAN     {this}; 
	TCPServer TServer {this};
	UDPServer UServer {this};
	TCPClient TClient {this};
	//UDPClient UClient // - We currently use UDPClient to store client data for UDPServer, not allowing it to connect to other servers.


	// Starts Multicast and TCP Server, for peer discovery and direct connections respectively. We can start the UDP server later when we want to implement direct UDP communication.
	//bool StartPeerV1() {
	//	// Start multicast in a separate thread
	//	multicastRunning = true;
	//	multicastThread = std::thread([this]() {
	//		if (!LAN.StartMulticastServer("235.69.4.20", 50420)) {
	//			//DEBUG_PRINT("Multicast server failed");
	//			multicastRunning = false;
	//		}
	//		});


	//	std::this_thread::sleep_for(std::chrono::milliseconds(100));

	//	// Start TCP Server on any available port (0 means any port, and the OS will assign one [not sure if this will work on c++ works on c#])
	//	if (TServer.StartServer(0)) {
	//		// After server and LAN started, send a multicast packet to the multicast group to announce our presence, and include our IP:Port so other peers can connect to us.
	//		
	//		// This isnt working here*
	//		std::string IPPort = TServer.GetHostIPPort(); 
	//		Debugger::WriteLine(IPPort);
	//		LAN.SendPacket(std::vector<uint8_t>(IPPort.begin(), IPPort.end()), MulticastAction::Join);

	//		// Auto false if it fails to start, we can also implement retry logic here if we want.
	//		return false;
	//	}
	//	return true;
	//}


};

