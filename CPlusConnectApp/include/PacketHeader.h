#pragma once
#include <cstdint> // used to get our uint16_t types
#include "Endian.h"
#include <vector>
#include <iterator>

#include "_Network.h"
#include "NetUtil.h"
#include <chrono>
#include "json.hpp"

//namespace PacketActionType {
//
//
//
//}
//
//namespace PacketEncodingType {
//
//
//}

enum PacketActionType : uint16_t {
    Empty = 0,
    //HeartBeat
    Ping, Pong,
    // Authentication
    SYN, SYNAck, ACK, Ready,
    // Previously called Data, but its name was conflicting with another other
    PacketData,
    Voice,

    // This section is detected to errors
    EmptyEncryptedPacket,

    /// <summary>
    /// Sent when the remote party wants to form a p2p network
    /// </summary>
    P2PInt,

    /// <summary>
    /// Signals to the remote party that a peer has joined, and forwards their information
    /// </summary>
    PeerJoin,

    /// <summary>
    /// Signals when a peer has been shared from another peer (it gets discovery but doesnt have to be connected)
    /// </summary>
    PeerShared,

    /// <summary>
    /// Signals to the remote party that a peer has left, and forwards their information
    /// </summary>
    PeerLeave, Disconnect

};

// This is used to detect what data form the received data is in, for now it should be in UTF8, JSON and BINARY, 
// but what do I know either way, this will be used to later convert the data from one type to another so we can convert it properly for use (news flash its not used because its not needed),
// data is ALWAYS sent as UTF8 bytes right now, packaged in JSON, bytes should be BINARY therefore I need to redo this whole Encoding Type Enum system,
// System needs to be dynamic where we can do whatever we want and it'll still work
enum PacketEncodingType : uint16_t {
    UTF8,
    JSON,
    XML,
    BINARY
};

//const char* ToString(PacketEncodingType type)
//{
//    switch (type)
//    {
//        case PacketEncodingType::UTF8: return "UTF8";
//        case PacketEncodingType::JSON: return "JSON";
//        case PacketEncodingType::XML: return "XML";
//        case PacketEncodingType::BINARY: return "BINARY";
//        default: return "Unknown";
//    }
//}

// Settings that a server can set as a requirement for communication to connected peers
enum PacketEncryptionType : uint16_t {
	NONE = 0,
	AES, // Doesnt even exist in C# yet
	RSA,
	ChaCha20Poly1305
};

//const char* ToString(PacketEncryptionType type)
//{
//    switch (type)
//    {
//    case PacketEncryptionType::NONE: return "NONE";
//    case PacketEncryptionType::AES: return "AES";
//    case PacketEncryptionType::RSA: return "RSA";
//    case PacketEncryptionType::ChaCha20Poly1305: return "ChaCha20Poly1305";
//    default: return "Unknown";
//    }
//}


class PacketHeader {

public:
    PacketHeader() = default;
    explicit PacketHeader(int32_t ByteLength, PacketActionType PacketAction,  PacketEncryptionType EncryptionType) : ByteLength(ByteLength), PacketAction(PacketAction), EncryptionType(EncryptionType) {
        //this->ByteLength = ByteLength;
        //this->PacketAction = PacketAction;
        ////this->EncodingType = EncodingType;
        //this->EncryptionType = EncryptionType;
        //this->SentAt = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()).count();
    }

    // Packet member visualization
    int32_t ByteLength = -1;                                                     // 4 bytes
	PacketActionType PacketAction = PacketActionType::Empty;                     // 2 bytes
    PacketEncodingType EncodingType = PacketEncodingType::BINARY;                // 2 bytes
    PacketEncryptionType EncryptionType = PacketEncryptionType::NONE;            // 2 bytes
    int64_t SentAt;                                                          // 8 bytes  // need to init this later but we arent using it yet but SOON!

    // Manually written out the size of our header length to automatically grab it later
    static constexpr int HeaderSize = 18; // Makes this compile time constant (originally used const), Updating this dynamically based on everything in our header would be nice but for right now manually keeping it the same is okay


    static PacketHeader CreateHeader(int32_t ByteLength, PacketActionType PacketAction, PacketEncryptionType EncryptionType) {
        // This exists becaues intelisense doesnt detect our constructor 

        PacketHeader header(ByteLength, PacketAction, EncryptionType);
        header.SentAt = std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch()).count();
        return header; // need to make this packet header better later on all versions of this application giving it unlimited scope (encoding type is never used)
    }

    // Reads stream, Tries to output PacketHeader if big enough, if not returns false.
    static bool HasValidHeaderTCP(SocketHandler sock, PacketHeader& Header) {
        
        // Checks for our packet header size
        if (!NetUtil::IsDataAvailableV2(sock, HeaderSize, 50)) {
            // Not big enough to be our packet header

            // Return empty header by default
            Header = PacketHeader(0, PacketActionType::Empty, PacketEncryptionType::NONE);
            return false;
        }

        char TempBuffer[HeaderSize];

        // Receives the buffer big enough to be our HeaderPacket
        int bytesRead = recv(sock, TempBuffer, sizeof(TempBuffer), 0);

        // Verify there werent any errors - Missing from C# (not really but its at least not done this way)
        if (bytesRead == HeaderSize && ValidateHeader(reinterpret_cast<const uint8_t*>(TempBuffer), bytesRead, Header)) return true;
        else return false;
    }

    static std::vector<char> ReceivePacketTCP(SocketHandler sock, PacketHeader& outHeader) {
        // Initialize output
        outHeader = PacketHeader{};
        int bytesRead;

        // Check if socket is valid
        if (sock == INVALID_SOCKET) {
        
            Debugger::WriteError("ReceivePacketTCP: InvalidSocket");
            return {};
        }

        // Check if we have a valid header
        if (!HasValidHeaderTCP(sock, outHeader)) {
            //Debugger::WriteError("ReceivePacketTCP: Header isnt valid");
            return {};
        }

        Debugger::WriteLine("HEADER IS VALID");

        // Create buffer for the data payload
        //std::vector<uint8_t> buffer(outHeader.ByteLength); - I want this as char for now
        std::vector<char> buffer(outHeader.ByteLength);

        // Receive the actual data
        //reinterpret_cast<char*>(buffer.data())
        bytesRead = recv(sock, buffer.data(),outHeader.ByteLength, 0);

        // Check if we received the expected amount
        if (bytesRead == outHeader.ByteLength) return buffer;  
        else return {};  
    }

    // Reads from our provided buffer, then output a valid header to pull the rest of the network message
    static bool ValidateHeader(const uint8_t* data, const size_t length, PacketHeader& Header) {
        
        // Make another version of this to allow for smaller than the header size or more than it to account for either 
        // #1 Optional data sets in header to save room 
        // #2 Passing all data received into here and filtering out header data vs packet data 

        if (length != HeaderSize) return false;

        // Converts char into uint8 then gets the data in a vector for use
        const uint8_t* bytes = reinterpret_cast<const uint8_t*>(data);
        std::vector<uint8_t> span(bytes, bytes + length);
        size_t offset = 0;

        // Reads our data as Little Endian then updates the Offset and continues until the end to gather all our data
        // We can improve this later but for right now I havent even tested if it works yet
        // Also: Endian header might need updated as it could be an existing header elsewhere and cause issues with others (Ex: PS4 potentially)

        Header.ByteLength = Endian::ReadInt32LittleEndian(bytes + offset, 4);
        offset += 4;

        uint16_t action = Endian::ReadUInt16LittleEndian(bytes + offset, 2);
        Header.PacketAction = static_cast<PacketActionType>(action);
        offset += 2;

        uint16_t encoding = Endian::ReadUInt16LittleEndian(bytes + offset, 2);
        Header.EncodingType = static_cast<PacketEncodingType>(encoding);
        offset += 2;

        uint16_t encryption = Endian::ReadUInt16LittleEndian(bytes + offset, 2);
        Header.EncryptionType = static_cast<PacketEncryptionType>(encryption);
        offset += 2;

        Header.SentAt = Endian::ReadInt64LittleEndian(bytes + offset, 8);
        offset += 8;

        return true;
    }

    // Packs this header into bytes used as the front portion of a message, 
    // that all communications will eventually use to distingish between data, 
    // can be smaller than our maxium header size in the future if need be
    std::vector<uint8_t> Serialize() {
        std::vector<uint8_t> packet;
        packet.reserve(this->HeaderSize);

        auto byLen = Endian::WriteInt32LittleEndian(static_cast<int32_t>(this->ByteLength));
        packet.insert(packet.end(), byLen.begin(), byLen.end());

        auto Action = Endian::WriteUInt16LittleEndian(static_cast<uint16_t>(this->PacketAction));
        packet.insert(packet.end(), Action.begin(), Action.end());

        auto Encoding = Endian::WriteUInt16LittleEndian(static_cast<uint16_t>((this->EncodingType)));
        packet.insert(packet.end(), Encoding.begin(), Encoding.end());

        auto Encryption = Endian::WriteUInt16LittleEndian(static_cast<uint16_t>(this->EncryptionType));
        packet.insert(packet.end(), Encryption.begin(), Encryption.end());

        auto Sent = Endian::WriteInt64LittleEndian(static_cast<int64_t>(this->SentAt));
        packet.insert(packet.end(), Sent.begin(), Sent.end());

        return packet;
    }

    std::string ToJSON() {
        return nlohmann::ordered_json{
        {"ByteLength", std::to_string(ByteLength)},
        {"PacketAction", static_cast<int>(PacketAction)},
        {"EncodingType", static_cast<int>(EncodingType)},
        {"EncryptionType", static_cast<int>(EncryptionType)},
        {"SentAt", std::to_string(SentAt)}
        }.dump().c_str();
    }
};