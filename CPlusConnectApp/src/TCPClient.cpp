#include "TCPClient.h"

#include <iostream>
#include "UTF8Helper.h"
#include "PacketAuthentication.h"
#include "ChaCha.h"
#include "CryptUtils.h"
#include "PacketEncrypted.h"

#include "HeartBeat.h"
//class HeartBeat;

bool TCPClient::Connect(const char* IP, int Port, bool IsBlocking) {

	//  Validate Port here
	if (!NetUtil::IsValidPort(Port)) {
		Debugger::WriteLine("Failed to validate port");
		return false;
	}

	// Create Socket
	if (!NetUtil::TryCreateSocket(AF_INET, SOCK_STREAM, 0, sock)) {
		Debugger::WriteError("Failed to create socket: ");
		return false;
	}

	if (!IsBlocking) {
		// Set NON-BLOCKING mode
#ifdef _WIN32
		u_long mode = 1;
		ioctlsocket(sock, FIONBIO, &mode);
#else
		int flags = fcntl(udpSocket, F_GETFL, 0);
		fcntl(udpSocket, F_SETFL, flags | O_NONBLOCK);
#endif
	}

	// Validate IP then try connect
	auto ip = NetUtil::ParseIP(IP);
	switch (ip.type) {
	case NetUtil::IPType::IPv4:

		serverAddr.sin_family = AF_INET;
		serverAddr.sin_addr.s_addr = ip.v4.S_un.S_addr;
		serverAddr.sin_port = htons(Port);
		if (connect(sock, (struct sockaddr*)&serverAddr, sizeof(serverAddr)) != 0) {
			char ipStr[INET_ADDRSTRLEN];
			inet_ntop(AF_INET, &(ip.v4.S_un.S_addr), ipStr, INET_ADDRSTRLEN);
			Debugger::WriteLine("IP: \"" + std::string(ipStr) + " " + "Port: \"" + std::to_string(Port) + "\"");
			Debugger::WriteError("Failed to connect"); //WSAGetLastError()
			closesocket(sock);
			IsClientConnected = false;
		}
		else {
			Debugger::WriteLine("Sucessfully connected to server!");
			IsClientConnected = true;
		}


		// Handle valid connection here (IE store server address for future use, set up recv thread, etc)
		//OnConnected.InvokeAsync(sock);
		break;
	case NetUtil::IPType::IPv6:
		// Implement this later, for now just return false since we dont support IPv6 yet (using sockaddr_storage should be good)
		Debugger::WriteError("IPv6 is not supported yet");
		IsClientConnected = false;
		break;
	case NetUtil::IPType::None:
		Debugger::WriteError("Invalid IP address: " + std::string(IP));
		IsClientConnected = false;
	}

	// Send manual SYN here


	//Console.WriteLine($"[Client] Server: {Client.RemoteEndPoint} - Me: {NetworkUtils.GetLocalLanIp()}:{((IPEndPoint)Client.LocalEndPoint).Port} - ClientId: {Self.PeerId}");
	Debugger::WriteLine("[Client] Server: " + std::string(IP) + " - Me: [N/A] - ClientId: " + Self->PeerId.str().c_str());

	Packer = PacketHelper(Self, sock);
	Heartbeat = HeartBeat(Self, &Packer);
	return IsClientConnected;
}

void TCPClient::CheckForPackets() {
	if (!IsClientConnected || sock == INVALID_SOCKET) return;

	int bytesSent;
	char buffer[4096]; // Set correct buffer size later (this will be horrible being spammed into existance, maybe store it on the client class itself)
	PacketHeader Header;

	// Drops all empty packets immediately - if we drop packets here we can never read any actual data, drop after reading so we can do it all at once
	std::vector<uint8_t> Packet = Packer.ReceiveUTF8Packet(Header);
	//std::vector<char> Packet = Packer.ReceivePacket(header);
 
	//Continue until authentication is complete
	if (!IsAuthenticated) IsAuthenticating = true;
	if (IsAuthenticating) {
		//Debugger::WriteLine("authenticating");

		// Send server SYN 
		if (IsFirstConnect) {
			//Debugger::WriteLine("first connect");
			
			// Generate ChaChaKey before we send SYN
			Packer.EncryptionKeys.ChaChaKey = CryptUtils::GenerateRandom(32);

			// Send PeerId included with quotes to make it json readable (fails to read without it) (Make custom json creator using template and arss) ToJson(args) - to pass string names and the data 
			bytesSent = Packer.SendUTF8Packet("\"" + Self->PeerId.str() + "\"", PacketActionType::SYN, false, PacketEncryptionType::NONE);
			IsFirstConnect = false; // this isnt being set to false 
			//Debugger::WriteLine("IsFirstConnect value: " + std::to_string(IsFirstConnect));
			//Debugger::WriteLine("IsFirstConnect address: " + std::to_string((uint64_t)&IsFirstConnect));
		}

		std::string JSON;
		PacketAuthentication Auth;
		PacketEncrypted EncryptedPacket;
		if (Header.PacketAction != PacketActionType::Empty) {
			Debugger::WriteLine("[Client] packet received - [" + std::to_string(Header.PacketAction) + "]");
			switch (Header.PacketAction)
			{
				case PacketActionType::SYNAck: // Receive server RSAKey, then send a ChaCha key back that we make
					Debugger::WriteLine("[Client] received [SYNAck]");

					// Data here should be of type PacketAuthentication(json format) -contains server RSAPubKey
					JSON = UTF8Helper::ToString(Packet);
					//Debugger::WriteLine("[Client] [SYNack] - json: " + JSON);


					if (PacketAuthentication::TryFromJson(JSON, Auth)) 
					{
						Debugger::WriteLine("[Client] server auth packet received successfully");

						// Sets server RSA Key
						Packer.EncryptionKeys.SetRemoteRSAKey(Auth.KeyData);
						//Debugger::WriteLine("[Client] set remote RSAPublicKey");
						Auth = PacketAuthentication(PacketEncryptionType::ChaCha20Poly1305, Packer.EncryptionKeys.ChaChaKey);
						JSON = Auth.ToJson();

						// Originally for encrypting then sending the encrypted packet
						//const auto& encrypted = PacketEncrypted::Encrypt(Packer, UTF8Helper::ToVector(JSON), PacketEncryptionType::RSA, true);
						//Packer.SendPacket(encrypted, PacketActionType::ACK, false, PacketEncryptionType::RSA);

						// Here we want to pass the unencrypted version of Auth and have it auto encrypted
						// Later handle results of this 
						Packer.SendEncryptedPacket(UTF8Helper::ToVector(JSON), PacketEncryptionType::RSA, PacketActionType::ACK, false);
						Debugger::WriteLine("[Client] sent encrypted ChaChaKey using RSA");
					}
				break;
				case PacketActionType::ACK: // Here we need to parse our JSON into PacketEncrypted from the server, and verify our ChaChaKey with the one they sent, then Send a server ready
					Debugger::WriteLine("[Client] received [ACK]");

					JSON = UTF8Helper::ToString(Packet);

					if (PacketEncrypted::TryFromJson(JSON, EncryptedPacket)) {
						Debugger::WriteLine("encrypted packet detected");

						// Here we should be decrypting from our local key
						const auto& decrypted = EncryptedPacket.Decrypt(Packer.EncryptionKeys, false, true);

						JSON = UTF8Helper::ToString(decrypted);
						Debugger::WriteLine("Decrypted Encrypted Packet -> " + JSON);

						// Compare KeyData Hash then send server ready - we're skipping hash for now
						Packer.SendUTF8Packet("<READY>", PacketActionType::Ready, false);
					}

				break;
				case Ready:
					IsAuthenticating = false;
					IsAuthenticated = true;
					Debugger::WriteLine("[Client] [Ready] Connection authenticated with Server");

					// We are returning here because we already received the message, any extra handling
					return; 
				break;
			}
		}



		// Prevent continuation only if its not completed in this loop
		if (!IsAuthenticated) return;// continue; - I think we can use return again but im not sure
	}

	// Handle heartbeat here
	bool IsDisconnected; 
	// I got no clue how this times out if if returned true from TrySendHeartBeat (thats supposed to mean it was sent successfully) - this might be backwards
	// Looking at the c# version again, it does have a ! after the method so I think its working that way
	if ((!Heartbeat.TrySendHeartBeat(IsDisconnected)) && IsDisconnected && !Heartbeat.FirstBeat) {
		// Requires something to cancel the loop (probably got a bool for IsServerRunning)
		IsClientConnected = false;

		// Requires us to create a state of the server/clients operational mode

		// Output Client timed out
		Debugger::WriteLine("[Client] Timed out");
		return;
	}

	// Skip null packets
	if (Header.PacketAction == PacketActionType::Empty) return;

	// Everything past this is encrypted**
	std::string JSON = UTF8Helper::ToString(Packet);

	Debugger::WriteLine("[Client] received => " + JSON);

	PacketEncrypted EncryptedPacket;
	std::vector<uint8_t> decrypted;
	if (PacketEncrypted::TryFromJson(JSON, EncryptedPacket) && EncryptedPacket.TryDecrypt(Packer.EncryptionKeys, decrypted, true, true)) {
		JSON = UTF8Helper::ToString(decrypted);
		Debugger::WriteLine("Decrypted Data successfully -> " + JSON);
		HandleUTF8Packet(Header, decrypted);
	}
	else Debugger::WriteError("Failed to decrypt data");
	
	return; // cut this off for now, probably will use it again for testing idk either way everything should be routed through this handler


	// testing sending encrypted messages
	Packer.SendUTF8Packet("testing encrypted messaging!!", PacketActionType::PacketData, true, PacketEncryptionType::ChaCha20Poly1305);
}

// named it this just so we can prepare for the future, since there will be other types of data being sent 
void TCPClient::HandleUTF8Packet(PacketHeader Header, std::vector<uint8_t> Packet) {
	// Handle ping/pong here for now 
	switch (Header.PacketAction) {
	case PacketActionType::Ping: Packer.SendUTF8Packet("", PacketActionType::Pong); break;
	}

}