#include "TCPClient.h"



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
	return IsClientConnected;
}

void TCPClient::CheckForPackets() {
	if (!IsClientConnected || sock == INVALID_SOCKET) return;

	int bytesSent;
	char buffer[4096]; // Set correct buffer size later (this will be horrible being spammed into existance, maybe store it on the client class itself)
	PacketHeader header;


	//Continue until authentication is complete
	if (!IsAuthenticated) IsAuthenticating = true;
	if (IsAuthenticating) {
		// Send server SYN 
		if (IsFirstConnect) {
			Debugger::WriteLine("first connect");
			//Packer.SendUTF8Packet($"{Self.PeerId.ToJSON()}", PacketActionType.SYN, false);
			bytesSent = Packer.SendUTF8Packet(Self->PeerId.str(), PacketActionType::SYN, false, PacketEncryptionType::NONE);
			IsFirstConnect = false; // this isnt being set to false 
			Debugger::WriteLine("IsFirstConnect value: " + std::to_string(IsFirstConnect));
			Debugger::WriteLine("IsFirstConnect address: " + std::to_string((uint64_t)&IsFirstConnect));
		}

		// Prevent continuation only if its not completed in this loop
		if (!IsAuthenticated) return;
	}


	std::vector<char> data = Packer.ReceivePacket(header);
	if (header.PacketAction == PacketActionType::Empty) return;
	//Debugger::WriteLine("received header: " + header.ToJSON());
	Debugger::WriteLine("C# data received ->\nSize: " + data.size());

	for (char c : data) {
		std::cout << std::hex << std::setw(2) << std::setfill('0')
			<< static_cast<int>(static_cast<unsigned char>(c)) << " ";
	}

	//Console.WriteLine($"C++ data received ->\nSize: {bytesRead} - DATA: {string.Join(" ", tempBuffer.Select(x => x.ToString("X2")))}");




	// Normal message handling




	//char buffer[4096];
	//int bytesRead;

	//bytesRead = recv(sock, buffer, sizeof(buffer), 0);

	//if (bytesRead > 0) {
	//	std::vector<uint8_t> received(buffer, buffer + bytesRead);




	//	PacketHeader::CreateHeader(received.length(), Type, EncryptionType)

	//	if (PacketHeader::ValidateHeader(received.data(), PacketHeader::HeaderSize, header)) {
	//		Debugger::WriteLine("Somehow I validated the header!!");


	//	}
	//	else {
	//		Debugger::WriteError("Failed to validate packet header");
	//	}
	//}
}