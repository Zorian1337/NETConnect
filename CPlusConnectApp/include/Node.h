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
#include <vector>


//class Multicast;
//class UDPServer;
//class TCPServer;
//class TCPClient;

// forward declaration only for tcpclient
class TCPClient;

class Node
{
private:
	std::thread multicastThread;
	bool multicastRunning = false;
	ThreadPool threadPool;

	// Makes clients thread safe
	mutable std::mutex clientsMutex;

public:
	Node() : threadPool(std::thread::hardware_concurrency()), LAN(this), TServer(this), UServer(this) {} //, TClient(this)

	ThreadPool& GetThreadPool() { return threadPool; }

	xg::Guid PeerId = xg::newGuid(); // Needs to defined per platform in preprocessers (current: GUID_WINDOWS=1)
	Multicast LAN;
	TCPServer TServer;
	UDPServer UServer;
	std::vector<std::unique_ptr<TCPClient>> Clients;

	bool HasPeer(xg::Guid RemotePeerId) const {
		// Only locks clients in this scope, so it'll autounlock after
		std::lock_guard<std::mutex> lock(clientsMutex);

		return std::any_of(Clients.begin(), Clients.end(),
			[&RemotePeerId](const auto& client) {
				return client && client->RemotePeerId == RemotePeerId;
			});
	}

};

