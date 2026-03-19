#pragma once
#include "_Network.h"
#include "_Debugging.h"

#include <string>
#include "Node.h"
#include "PacketHeader.h"



// Based on our C# version this was stuffed into every type of server/client instance and included security key data so we could always have the same structure everywhere
class PacketHelper {

public:
	Node* Self;
	SocketHandler sock;

	PacketHelper() = default;
	PacketHelper(Node* Peer, SocketHandler sock) : Self(Peer), sock(sock) { }

	// Change IsEncryptionEnabled back to true after we add encryption
	int SendUTF8Packet(std::string UTF8Data, PacketActionType Type = PacketActionType::PacketData, bool IsEncryptionEnabled = false, PacketEncryptionType EncryptionType = PacketEncryptionType::NONE) {
		int bytesSent = -1;

		if (IsEncryptionEnabled) {
			// Doesnt exist yet
		}
		else
		{
			PacketHeader header = PacketHeader::CreateHeader(UTF8Data.length(), Type, EncryptionType);

			// Packs the data with the header
			std::vector<uint8_t> packet = header.Serialize();
			packet.insert(packet.end(), UTF8Data.begin(), UTF8Data.end());

			bytesSent = send(sock, reinterpret_cast<const char*>(packet.data()), packet.size(), 0);

			if (bytesSent > 0) {
				
				for (char c : packet) {
					std::cout << std::hex << std::setw(2) << std::setfill('0')
						<< static_cast<int>(static_cast<unsigned char>(c)) << " ";
				}
				std::cout << std::endl;
				Debugger::WriteLine("Sent: " + header.ToJSON() + "\n");
			}
			
		}
		
		return bytesSent;
	}

	std::vector<char> ReceivePacket(PacketHeader& Header) {
		return PacketHeader::ReceivePacketTCP(sock, Header);
	}
};