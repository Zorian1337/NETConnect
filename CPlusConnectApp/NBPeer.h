#pragma once

#include "_Network.h"

#include <thread>
#include <atomic>

#include "Node.h"
#include "ThreadPool.h"

class NBPeer
{
private:
	std::thread mainThread;


public:
	std::atomic<bool> IsMainThreadRunning { true };

	NBPeer();

	// Container of all our peers
	Node Peer;
	ThreadPool threadPool;
	


	void Start();
};

