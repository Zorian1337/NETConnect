#include "Multicast.h"
#include "Node.h" // safe to include node here but not in the .h if we are trying to have it as a reference

#include <thread>
#include <chrono>


//Multicast::Multicast(Node& Peer) : Self(Peer), Server(Peer) {
//	//Server.OnUDPDataReceived.Subscribe(this, &Multicast::OnDataReceived);
//}

int Multicast::SendPacket(const std::vector<uint8_t>& _data, MulticastAction _actionType) {
	MulticastPacket packet(Self->PeerId, _data, _actionType);
	Debugger::WriteLine("sending multicast join packet");
	int bytesSent = SendToAll(packet.ToJson());
	return bytesSent;
}

void Multicast::CheckForPackets() {
	if (!IsServerRunning || Server.sock == INVALID_SOCKET) return;

	char buffer[4096];
	UDPClient client;
	sockaddr_in clientAddr;
	socklen_t addrLen = sizeof(clientAddr);

	int bytesRead = recvfrom(Server.sock, buffer, sizeof(buffer), 0, (struct sockaddr*)&clientAddr, &addrLen);

	if (bytesRead > 0) {
		// Corrects network order so we can read the IP and port
		inet_ntop(AF_INET, &clientAddr.sin_addr, client.IP, 16);
		client.Port = ntohs(clientAddr.sin_port);
		client.addr = clientAddr;

		std::vector<uint8_t> data(buffer, buffer + bytesRead);
		//Debugger::WriteLine(UTF8Helper::ToString(data).c_str()); // _Debugger not included

		Self->GetThreadPool().enqueue([this, client, data]() { 
			this->OnDataReceived(client, data);
		});
	}
}

void Multicast::OnDataReceived(UDPClient Client, std::vector<uint8_t> data) {
	

	// Attempt to parse from MulticastPacket as this is a Multicast, so all data should be in this form anyway.
	MulticastPacket packet;
	if (!MulticastPacket::TryFromJson(UTF8Helper::ToString(data), packet)) {
		//printf("packet failed to be read\n");
		return;
	}

	// Ignore packets from self
	if (Self->PeerId == packet.SenderId) return;

	// for now we are reading this here as we have no other use for the multicast
	printf("%s received ->\n	%s\n", Self->PeerId.str().c_str(), UTF8Helper::ToString(data).c_str());


	//printf("Packet: %s\n", packet.ToJson().c_str());
	//printf("data received as a test\n");

	 // blocked for debugging

	printf("[%s] -> sent packet data\n", packet.SenderId.str().c_str());

	switch (packet.Action)
	{
		// JOIN
		case 0: {
			// Make sure this client is new before we add them to our list
			if (Self->HasPeer(packet.SenderId)) return; 

			// Parse our data for our client (IP:Port)
			std::string Data = UTF8Helper::ToString(packet.Data);
			std::vector<std::string> Addr = UTF8Helper::Split(Data, ':');

			// Only allow the size of 2 as we NEED these two pieces
			if (Addr.size() == 2) {
				// Handle our new peer
				std::string IP = Addr[0];
				std::string Port = Addr[1];

				// Log new peer and print they joined for now
				printf("Peer [%s:%s] joined the system.\n", IP.c_str(), Port.c_str());


				// Tell our server they need to connect to the new peer as a client
				
				auto client = std::unique_ptr<TCPClient>(new TCPClient(Self, packet.SenderId));
				
				//std::unique_ptr<TCPClient> client(new TCPClient(Peer));

				if (client->Connect(IP.c_str(), NetUtil::ParsePort(Port.c_str()))) {
					//std::this_thread::sleep_for(std::chrono::milliseconds(5000));
					// Announce SYN for auth
					//int sent = client->Packer.SendUTF8Packet("", PacketActionType::SYN);
					//printf("sent bytes, %i\n", sent);


					Self->Clients.push_back(std::move(client));
					printf("[MULTICAST] C++ Server connected to C# server as a client...\n");
				}
			}




			break;
		}
		// Leave
		case 1: break;
		case 2: break;
	}
}