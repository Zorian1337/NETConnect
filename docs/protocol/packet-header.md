# PacketHeader

The `PacketHeader` is the foundation of all communication in NETConnect. Every packet sent across the network contains this header, which provides routing, encryption, and control information.

---

## Overview

| Field | Type | Size | Description |
|-------|------|------|-------------|
| `Magic` | `ushort` | 2 bytes | Protocol identifier (`20035` / `0x4E43` → "NC") |
| `Version` | `byte` | 1 byte | Protocol version |
| `HeaderLength` | `byte` | 1 byte | Total header length in bytes |
| `PayloadLength` | `int` | 4 bytes | Length of payload data |
| `SentAt` | `long` | 8 bytes | Unix timestamp (milliseconds) |
| `PacketId` | `int` | 4 bytes | Unique packet identifier |
| `Type` | `PacketType` | 1 byte | Packet type |
| `Action` | `PacketAction` | 1 byte | Action to perform |
| `Encoding` | `PacketEncoding` | 1 byte | Payload encoding format |
| `Encryption` | `PacketEncryption` | 1 byte | Encryption algorithm used |
| `Route` | `PacketRoute` | 1 byte | Routing method |
| `OriginPeerId` | `Guid` | 16 bytes | Sender's peer ID |
| `LastHopId` | `Guid` | 16 bytes | Previous hop's peer ID |
| `RecipientPeerId` | `Guid` | 16 bytes | Intended recipient (`Guid.Empty` = Any or All) |
| `TTL` | `byte` | 1 byte | Time-to-live for gossip/broadcast |

**Total Header Size:** `78` bytes

---

## Preheader

The first **16 bytes** of every packet form the **Preheader**, used for fast validation **without** parsing the full header.

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0–1 | 2 | Magic | Protocol identifier |
| 2 | 1 | Version | Protocol version |
| 3 | 1 | HeaderLength | Total header size |
| 4–7 | 4 | PayloadLength | Data payload size |
| 8–15 | 8 | SentAt | Unix timestamp (ms) |

**Purpose:** Quick rejection of invalid or malformed packets before full processing.

---

## IPacketHeaderIdentifier

`IPacketHeaderIdentifier` is the foundational interface that all packet headers implement. It defines the minimum required fields for packet identification and validation.

### Required Fields
- `Magic`
- `Version`
- `HeaderLength`
- `PayloadLength`
- `SentAt`

### Why It's Important
- ✅ Enables extensibility — any class can become a packet header  
- ✅ Supports versioning — different implementations can coexist  
- ✅ Allows polymorphism — treat all headers uniformly  
- ✅ Enables testing — easy to mock for unit tests  

---

## Validation Rules

| Rule | Description |
|------|-------------|
| **Magic** | Must equal `20035` (`0x4E43`) |
| **Age** | `SentAt` must be within **60 seconds** of current time |
| **Desync** | Reject if `SentAt` is **more than 10 seconds in the future** (clock drift) |
| **TTL** | Must be `> 0` for broadcast/gossip; `0` is allowed for direct |
| **Recipient** | `Guid.Empty` = broadcast; otherwise specific peer |

### Duplicate Detection
Packets are tracked by: `OriginPeerId` + `PacketId` + `SentAt`

- ✅ Prevents replay attacks  
- ✅ Prevents duplicate processing  

### Clock Desync Handling

To protect against misconfigured or malicious clocks:

| Condition | Action |
|-----------|--------|
| `SentAt` > `CurrentTime + 10s` | ❌ Reject (future packet) |
| `SentAt` < `CurrentTime - 60s` | ❌ Reject (too old) |
| `SentAt` within `[-10s, +60s]` | ✅ Accept |

This prevents:
- Replay attacks using old packets  
- Poisoning the network with future-timestamped packets  
- Accidental issues from system clock drift  
