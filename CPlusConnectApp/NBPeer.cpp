#include "NBPeer.h"



NBPeer::NBPeer() : threadPool(std::thread::hardware_concurrency()), Peer()
{

}

void NBPeer::Start() {
	mainThread = std::thread([this]() {
		// This is where we will start our Node and all the servers and stuff, but for now we will just print a message to indicate it started.
		printf("NBPeer started!\n");

		while (IsMainThreadRunning) 
		{
			// Start our servers without listeners

			if (!Peer.LAN.IsServerRunning) {
				// Start our multicast server if its not already
				Peer.LAN.StartMulticastServer("235.69.4.20", 50420, false);
			}

			// Starting with our Multicast for now
			//if (!Peer.TServer.IsServerRunning) {
			//	// Start our tcp server if its not already
			//	Peer.TServer.StartServer(0, false);
			//}
			

			// Check our servers for new data.

			Peer.LAN.Server.sock.rec

			std::this_thread::sleep_for(std::chrono::microseconds(100));
		}
	});
}