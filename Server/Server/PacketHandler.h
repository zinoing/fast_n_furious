#pragma once
#include "Singleton.h"

class Client;

class PacketHandler : public Singleton<PacketHandler> {
    public:
        void handlePacket(Client& client, char* packetToHandle);
};