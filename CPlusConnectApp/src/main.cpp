#include "main.h"
#include <iostream> // Allows for printf and ReadKey

//#include "Node.h"
//#include "UDPServer.h"
//#include "Multicast.h"

#include "NBPeer.h"

int main() {

	//std::cout << "Hello World" << std::endl;

	printf("C++ NETConnect\n");

	// Read Key
	//std::cin.get();

	//Node* Peer = new Node(); // Creates non null ptr
	//UDPServer Server { Peer };
	//Multicast LAN { Peer };

	//std::cout << Peer->PeerId.str() << std::endl;


	NBPeer Peer;
	Peer.Start();
	Debugger::WriteLine("Press Enter to stop...");
	std::cin.get();

	//
	//LAN.StartMulticastServer("235.69.4.20", 50420)
	//if (Peer->LAN.StartMulticastServer("235.69.4.20", 50420)) {
	//	//printf("[%s] Multicast server started successfully!\n",LAN.Server.Self->PeerId.str().c_str());
	//}

	//if (Peer->StartPeerV1()) {
	//
	//}
}