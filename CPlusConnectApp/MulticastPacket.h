#pragma once
#include <cstdint>
#include <vector>

// Serialization 
#include "json.hpp"
#include "base64.h"
#include <guiddef.h>
#include "guid.hpp"


enum MulticastAction : uint8_t {
	Join, Leave, Data
};


struct MulticastPacket {
    xg::Guid SenderId;
	std::vector<uint8_t> Data;
	MulticastAction Action;

    // Default constructor
    MulticastPacket() : Action(MulticastAction::Join) {
        memset(&SenderId, 0, sizeof(GUID));
    }

    // Parameterized constructor
    MulticastPacket(const xg::Guid& _SenderId, const std::vector<uint8_t>& _data, MulticastAction _actionType) {
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
            Packet = *result;
            delete result; // Deletes the ptr
            return true;
        }
    }

    static MulticastPacket* FromJson(const std::string& Json) {
        // Parse to nlohmann::ordered_json

        using ordered = nlohmann::ordered_json;

        // Creates new packet, Or else it will crash
        MulticastPacket* p = new MulticastPacket();

        try {
            ordered parsed = ordered::parse(Json);

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

    //static bool TryFromJson(const nlohmann::json& j, MulticastPacket& packet) {
    //
    //    try 
    //    {
    //        packet = MulticastPacket::FromJson(j);
    //        return true;
    //    }
    //    catch (const std::exception& e) { printf("TryFromJson Error: %s\n", e.what());  return false; }
    //}

    //static MulticastPacket FromJson(const nlohmann::json& j) {
    //    MulticastPacket p;

    //    try {
    //        // Get string from JSON, then convert to GUID
    //        std::string guidStr = j.at("SenderId").get<std::string>();
    //        p.SenderId = xg::Guid(guidStr);

    //        std::string base64Data = j.at("Data").get<std::string>();
    //        std::string decodedStr = base64_decode(base64Data);

    //        
    //        std::vector<uint8_t> decodedData(decodedStr.begin(), decodedStr.end());
    //        p.Data = std::move(decodedData);
    //        p.Action = static_cast<MulticastAction>(j.at("Action").get<int>());
    //    }
    //    catch (const std::exception& e) { printf("TryFromJson Error: %s\n", e.what());   }

    //    return p;
    //}

    std::string ToJson() {
        return nlohmann::ordered_json{
            {"SenderId", GuidToString()},  // Convert GUID to string
            {"Data", Data},                 // vector<uint8_t> works directly
            {"Action", static_cast<int>(Action)}
        }.dump().c_str();
    }

};

