#include "main.h"
#include <iostream> // Allows for printf and ReadKey

//#include "Node.h"
//#include "UDPServer.h"
//#include "Multicast.h"
#include "UTF8Helper.h"
#include "PacketHeader.h"

#include "NBPeer.h"

int main() {

	printf("C++ NETConnect\n");
	

	// Read Key
	//std::cin.get();


	NBPeer Peer;
	Peer.Start();
	Debugger::WriteLine("Press Enter to stop...");
	std::cin.get();

}