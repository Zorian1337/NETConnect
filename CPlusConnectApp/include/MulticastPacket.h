#pragma once
#include <cstdint>
#include <vector>

// Serialization 
#include "json.hpp"
#include "simple_base64.h"
#include <guiddef.h>
#include "guid.hpp"
#include "UTF8Helper.h"

enum MulticastAction : uint8_t {
	Join, Leave, Data
};


struct MulticastPacket {
    int Version;
    xg::Guid SenderId;
	std::vector<uint8_t> Data;
	MulticastAction Action;

    // Default constructor
    MulticastPacket() : Action(MulticastAction::Join) {
        //memset(&SenderId, 0, sizeof(GUID)); apparently this is not needed 

    }

    // Parameterized constructor
    MulticastPacket(const int _Version, const xg::Guid& _SenderId, const std::vector<uint8_t>& _data, MulticastAction _actionType) {
        Version = _Version;
        SenderId = _SenderId;
        Data = _data;
        Action = _actionType;
    }

    // Convert GUID to string
    std::string GuidToString() const {
        return SenderId.str();
    }


    // Sadly we will have to manually add this to each of our classes, as there arent really any good alternatives

    // Static method to deserialize from JSON


    
    
    static bool TryFromJson(const std::string& Json, MulticastPacket& Packet) {
    
        auto* result = FromJson(Json);

        if (result == nullptr) return false;
        else {
        
            // * Dereferences the ptr
            Packet = *result; // Returns packet here as a refernce not a pointer
            delete result; // Deletes the ptr
            return true;
        }
    }

    // NOTE I have no clue why multicast packet is a pointer (oh its to check if its null)

    static MulticastPacket* FromJson(const std::string& Json) {
        // Parse to nlohmann::ordered_json

        using ordered = nlohmann::ordered_json;

        // Creates new packet, Or else it will crash
        MulticastPacket* p = new MulticastPacket();

        try {
            ordered parsed = ordered::parse(Json);

            // Parse Version
            int Version = parsed.at("Version").get<int>();

            // Parse our SenderId
            std::string guidStr = parsed.at("SenderId").get<std::string>();
            p->SenderId = xg::Guid(guidStr);

            // Decodes base64 as for some reason c++ auto converted to that
            std::string base64Data = parsed.at("Data").get<std::string>();
            std::string decodedStr = base64_decode(base64Data);
            std::vector<uint8_t> decodedData(decodedStr.begin(), decodedStr.end());
            p->Data = std::move(decodedData);

            p->Action = static_cast<MulticastAction>(parsed.at("Action").get<int>());
            return p;
        }
        catch (const std::exception& e) { return nullptr; }
    }

    std::string ToJson() {

        return nlohmann::ordered_json{
            {"Version",Version},
            {"SenderId", GuidToString()},  // Convert GUID to string
            {"Data", base64_encode(UTF8Helper::ToString(Data))},  // Convert data to base64 as its sent that way on our c# application (when byte[] gets serialized)
            {"Action", static_cast<int>(Action)}
        }.dump().c_str();
    }

};

