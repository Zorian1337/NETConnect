#pragma once

//#include <winsock2.h> -included in _Network.h
//#include <ws2tcpip.h> -included in _Network.h
//#pragma comment(lib, "Ws2_32.lib") -included in _Network.h
#include "_Network.h" // Includes (winsock, ws2tcpip, NetUtil, or any other net headers)
#include "NetUtil.h"

#include "Event.h"
#include "UTF8Helper.h"
#include "PacketHeader.h"
#include <vector>

#include "PacketHelper.h"
#include "json.hpp"
#include <iostream> // for hex
#include "HeartBeat.h"

class Node;
//class HeartBeat;

class TCPClient
{
public:

	// EventHandlers 

	// Servers Peer data
	Node* Self;
	// This is the peerId of the connected party
	xg::Guid RemotePeerId;

	PacketHelper Packer;

	explicit TCPClient() : Self(nullptr) {}
	explicit TCPClient(Node* Peer, xg::Guid RemotePeerId) : Self(Peer), RemotePeerId(RemotePeerId)  { }

	sockaddr_in serverAddr{};

	// Handles the socket for each platform (SOCKET for Windows, int for Linux)
	SocketHandler sock = INVALID_SOCKET;
	std::atomic<bool> IsClientConnected = false;

	std::atomic<bool> IsAuthenticating = false;
	std::atomic<bool> IsAuthenticated = false;
	std::atomic<bool> IsFirstConnect = true;
	HeartBeat Heartbeat{};

	Event<SocketHandler> OnConnected;
	Event<SocketHandler, std::vector<uint8_t>> OnDataReceived;

	// Move to .cpp so we can access Node 
	bool Connect(const char* IP, int Port, bool IsBlocking = true);

	// Moved to .cpp so we can access Node
	void CheckForPackets();
	void HandleUTF8Packet(PacketHeader Header, std::vector<uint8_t> Packet);
};

