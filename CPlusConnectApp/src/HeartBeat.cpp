#include "HeartBeat.h"

// Global 
#include "_Debugging.h"

#include "Node.h"
#include "PacketHelper.h"

#include <chrono>


bool HeartBeat::TrySendHeartBeat(bool& IsDisconnected) {
	IsDisconnected = false;

	if (!IsEnabled) return true;

	auto now = std::chrono::system_clock::now();
	auto PulseAt = std::chrono::seconds(PulseAtInSeconds);
	auto PulseLimit = std::chrono::seconds(PulseCooldown);

	if (now >= LastBeatAt + PulseAt && now >= LastPulseAt + PulseLimit) {
		
		// Inits the HeartBeat system, Originally used to stop it dropping the connection during auth
		if (FirstBeat) { SetLastBeat(); FirstBeat = false; }

		
		return SendPing(IsDisconnected);
	}

	// By default return false
	return false;
}

bool HeartBeat::SendPing(bool& IsDisconnected) {
	IsDisconnected = false;

	if (!IsEnabled) return true;

	int bytesSent = Helper->SendUTF8Packet("<PING>", PacketActionType::Ping);
	Debugger::WriteLine("Sending ping [" + std::to_string(bytesSent) + "]");

	if (bytesSent > 0) { SetLastBeat(); return true; }
	else { IsDisconnected = true; return false; }
}