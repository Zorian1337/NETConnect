#pragma once

#include "_Debugging.h"

#include <chrono>



//#include "Node.h" // - Need forward declaration to access our Node
class Node;
class PacketHelper;

class HeartBeat {
	
public:
	Node* Self;
	PacketHelper* Helper; 

	HeartBeat() = default;
	explicit HeartBeat(Node* Peer, PacketHelper* Packer) : Self(Peer), Helper(Packer) {}
	
	// I dont really understand setting time on c++ new to it and all 
	std::chrono::system_clock::time_point LastBeatAt;
	std::chrono::system_clock::time_point LastPulseAt;

	int TimeoutAfterInSeconds = 120;
	int PulseAtInSeconds = 10;
	int PulseCooldown = 10;

	bool FirstBeat = true;
	bool IsEnabled = true;

	//TimeSpan LastLatency;
	//PingTracker PingLog;


	bool IsTimeout() {
		if (!IsEnabled) return true;

		if (FirstBeat) return true;
		
		auto now = std::chrono::system_clock::now();
		auto timeoutDuration = std::chrono::seconds(TimeoutAfterInSeconds);

		if (now >= LastBeatAt + timeoutDuration) { Debugger::WriteLine("heartBeat Timed out"); return true; }
		else return false;
	}

	void SetLastBeat() {
		// Gets current time in UTC time (its supposed to be UTC time)
		auto now = std::chrono::system_clock::now();

		// Sets current times - this stuff probably needs fixed later but it partially works 
		LastBeatAt = now;
		LastPulseAt = now;
	}

	bool TrySendHeartBeat(bool& IsDisconnected);
	bool SendPing(bool& IsDisconnected);
};