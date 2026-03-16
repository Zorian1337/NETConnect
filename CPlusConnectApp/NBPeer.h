#pragma once

#include "_Network.h"

#include <thread>
#include <atomic>

#include "Node.h"
#include "ThreadPool.h"
#include "UTF8Helper.h"

class NBPeer
{
private:
	std::thread mainThread;
	ThreadPool threadPool;

public:

	explicit NBPeer() : threadPool(std::thread::hardware_concurrency()), IsMainThreadRunning(true), Peer(new Node()) {}
	std::atomic<bool> IsMainThreadRunning;// { true };

	// Container of all our peers
	Node* Peer;// = new Node();
	
	~NBPeer() {
		IsMainThreadRunning = false;

		if (mainThread.joinable()) {
			mainThread.join();
		}
	}

	void Start() {
		mainThread = std::thread([this]() {
			// This is where we will start our Node and all the servers and stuff, but for now we will just print a message to indicate it started.
			printf("NBPeer started!\n");


			Debugger::WriteLine("Thread started, IsMainThreadRunning = " +
				std::to_string(IsMainThreadRunning.load()));
			while (IsMainThreadRunning)
			{
				// Start our servers without listeners

				if (!Peer->LAN.IsServerRunning) {
					// Start our multicast server if its not already
					if (Peer->LAN.StartMulticastServer("235.69.4.20", 50420, false)) {
						Debugger::WriteLine("Started multicast");
					}
				}

				// Starting with our Multicast for now
				if (!Peer->TServer.IsServerRunning) {
					// Start our tcp server if its not already
					if (Peer->TServer.StartServer(0, false)) {
						Debugger::WriteLine("Started tcp-server");
					}
				}


				// Check our servers for new data.

				std::this_thread::sleep_for(std::chrono::microseconds(100));
			}
			Debugger::WriteLine("out of scope[main]");
			});
	}
};

