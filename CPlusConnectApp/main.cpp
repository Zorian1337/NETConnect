#include "main.h"
#include <iostream>
#include "Multicast.h"
#include "Windows.h";


#include "JsonHelper.h"
#include "MulticastPacket.h"
#include "NetUtil.h"

#include "TCPServer.h"
#include "Node.h"
#include "NBPeer.h"
int main() {

	//std::cout << "Hello World" << std::endl;

	printf("C++ NETConnect\n");

	// Read Key
	std::cin.get();

	NBPeer Peer;
	Peer.Start();


	//Node Peer;
	//if (Peer.StartPeer()) {
	//	// Probably register to OpenDHT here for peer discovery (im assuming)
	//}


	//for (;;) {
	//	// inf sleep 
	//	Sleep(1000);
	//}
	// BELOW THIS IS ALL EXPERIEMENTAL

	////Server.StartServer("test", 9)
	////




	//xg::Guid SenderId;
	//std::vector<uint8_t> Data;
	//MulticastAction Action;

	//JsonProperty<xg::Guid>* prop1 = new JsonProperty<xg::Guid>();
	//prop1->Name = "SenderId";
	////prop1->ValueType = 0;  // Just an example value

	//JsonRegister<MulticastPacket>::Register(prop1)



	// example
	//std::vector<JsonPropertyBase*> props;
	//props.push_back(new JsonProperty<xg::Guid*>("SenderId"));
	//props.push_back(new JsonProperty<std::vector<uint8_t>>("Data"));
	//props.push_back(new JsonProperty<MulticastAction>("Action"));

	//// Register them all together
	//JsonRegister<MulticastPacket>::Register(props);


	//std::vector<JsonPropertyBase*> prop2;
	//prop2.push_back(new JsonProperty<xg::Guid*>("SenderId3"));
	//prop2.push_back(new JsonProperty<std::vector<uint8_t>>("Data3"));
	//prop2.push_back(new JsonProperty<MulticastAction>("Action4"));


	//JsonRegister<GUID>::Register(props);


	//auto& registry = JsonRegister<MulticastPacket>::getRegistry(); // SHOW ALL REGARDLESS OF TYPE

	//// Loop through all registered classes
	//std::cout << "Testing registry - found " << registry.size() << " classes:" << std::endl;
	//std::cout << "========================================" << std::endl;

	//for (const auto& pair : registry) {
	//	// pair.first is the typeName (string)
	//	// pair.second is the JsonRegister pointer

	//	std::cout << "Class: " << pair.first << std::endl;


	//	// If you have a way to get properties, you could loop through those too
	//	//JsonRegister* reg = pair.second;
	//	std::cout << "	Property\n";
	//	 for (const auto& prop : pair.second->GetProperties()) {
	//	     std::cout << "		Name: " << prop->Name << "	" << prop->GetTypeInfo().name() << std::endl;
	//	 }
	//}

	//std::cout << "========================================" << std::endl;

	//JsonHelper::Visualize();

	//printf("items: %i", count(registry));
}