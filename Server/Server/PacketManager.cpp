#define _CRT_SECURE_NO_WARNINGS
#include "PacketManager.h"
#include "RoomManager.h"
#include "Client.h"
#include "HolePunchingHandler.h"
#include "Server.h"

PacketManager::PacketManager() {}

PacketManager::~PacketManager() {}

char* PacketManager::createPacket(int size, PacketType tag, char* detail)
{
	char* packet = new char[size];
    int offset = 0;

    packet[offset] = size;
    offset += 1;

    packet[offset] = (int)tag;
    offset +=1;

    if (size-2 != 0) {
        memcpy(packet + offset, detail, size-2);
        offset += sizeof(detail);
    }

	return packet;  
}

char* PacketManager::encodeRoomSetting(RoomSetting& roomSetting)
{
    char* packet = new char[ROOMSETTING_SIZE];
    int offset = 0;

    memcpy(packet + offset, roomSetting.roomID, sizeof(roomSetting.roomID));
    offset += sizeof(roomSetting.roomID);

    memcpy(packet + offset, roomSetting.roomName, sizeof(roomSetting.roomName));
    offset += sizeof(roomSetting.roomName);

    packet[offset] = (char)roomSetting.maximumCapacity;
    offset += sizeof(char);

    packet[offset] = (char)roomSetting.isPublic;
    offset += sizeof(char);

    memcpy(packet + offset, roomSetting.roomPassword, sizeof(roomSetting.roomPassword));

    return packet;
}

char* PacketManager::encodeRoomSettingWithoutPassword(RoomSetting& roomSetting)
{
    char* packet = new char[ROOMSETTING_WITHOUT_PASSWORD_SIZE];
    int offset = 0;

    memcpy(packet + offset, roomSetting.roomID, sizeof(roomSetting.roomID));
    offset += sizeof(roomSetting.roomID);

    memcpy(packet + offset, roomSetting.roomName, sizeof(roomSetting.roomName));
    offset += sizeof(roomSetting.roomName);

    packet[offset] = (char)roomSetting.maximumCapacity;
    offset += sizeof(char);

    packet[offset] = (char)roomSetting.isPublic;
    offset += sizeof(char);

    return packet;
}

EndPoint PacketManager::decodeEndPoint(char* endPoint) {
    char* ipAddr = strtok(endPoint, ":");
    char* portStr = strtok(NULL, "\0");

    string fullEP = (string)ipAddr + ":" + (string)portStr;
    EndPoint ep = EndPoint(ipAddr, portStr, fullEP);
    return ep;
}

unsigned int PacketManager::decodePort(char* message) {
    unsigned int port = 0;
    int offset = 2;
    char portBytes[2];
    memcpy(portBytes, message + offset, 2);
    memcpy(&port, portBytes, 2);
    return port;
}

char* PacketManager::encodeSuperPeer(Client& superPeer) {
    int maximumIPAddressSize = 17;
    int packetSize = maximumIPAddressSize * 2;
    char* packet = new char[packetSize];
    memset(packet, 0, packetSize);
    int offset = 0;

    // encode private end point
    {
        char* privateIpAddress = new char[15];
        strcpy(privateIpAddress, superPeer.getUDPSockPrivateEP().ipAddress.c_str());
        memcpy(packet + offset, privateIpAddress, 15);
        offset += 15;

        u_short udpPort = superPeer.getUDPSockPrivateUdpPort();
        memcpy(packet + offset, &udpPort, 2);
        offset += sizeof(u_short);

        cout << "super peer private ip" << superPeer.getUDPSockPrivateEP().ipAddress << endl;
    }

    // encode public end point
    {
        char* publicIpAddress = new char[15];
        strcpy(publicIpAddress, superPeer.getUDPSockPublicEP().ipAddress.c_str());
        memcpy(packet + offset, publicIpAddress, 15);
        offset += 15;

        u_short udpPort = superPeer.getUDPSockPublicUdpPort();
        memcpy(packet + offset, &udpPort, 2);
        offset += sizeof(u_short);

        cout << "super peer public ip" << superPeer.getUDPSockPublicEP().ipAddress << endl;
    }
    return packet;
}

char* PacketManager::encodeClientsUDPAddress(vector<Client*> clients) {
    int numOfClients = (int)clients.size();
    int idSize = 4;
    int maximumIPAddressSize = 17;
    int totalPacketSize = numOfClients * (idSize + maximumIPAddressSize * 2);

    char* totalPacket = new char[totalPacketSize];
    memset(totalPacket, 0, totalPacketSize);
    int totalOffset = 0;

    for (int i = 0; i < numOfClients; i++) {
        int offset = 0;
        char* packet = new char[idSize + maximumIPAddressSize * 2];
        memset(packet, 0, idSize + maximumIPAddressSize * 2);

        // encode client id
        const int id = clients[i]->getClientID();
        memcpy(packet + offset, &id, sizeof(int));
        offset += sizeof(int);
        
        // encode private end point
        {
            // encode private ip address
            char* privateIpAddress = new char[15];
            memset(privateIpAddress, 0, 15);
            strcpy(privateIpAddress, clients[i]->getUDPSockPrivateEP().ipAddress.c_str());
            memcpy(packet + offset, privateIpAddress, 15);
            offset += 15;

            // encode private port
            u_short privateUdpPort = clients[i]->getUDPSockPrivateUdpPort();
            memcpy(packet + offset, &privateUdpPort, sizeof(u_short));
            offset += sizeof(u_short);
        }

        // encode public end point
        {
            // encode public ip address
            char* publicIpAddress = new char[15];
            memset(publicIpAddress, 0, 15);
            strcpy(publicIpAddress, clients[i]->getUDPSockPublicEP().ipAddress.c_str());
            memcpy(packet + offset, publicIpAddress, 15);
            offset += 15;

            // encode public port
            u_short publicUdpPort = clients[i]->getUDPSockPublicUdpPort();
            memcpy(packet + offset, &publicUdpPort, sizeof(u_short));
            offset += sizeof(u_short);
        }

        memcpy(totalPacket + totalOffset, packet, idSize + maximumIPAddressSize * 2);
        totalOffset += offset;
    }
    return totalPacket;
}

char* PacketManager::extractFromPacket(int startIndex, int size, char* packet) {
    // char* returnBytes = new char[size - startIndex];
    char* returnBytes = new char[size];

    byte* byteMessage = static_cast<byte*>(static_cast<void*>(packet));
    byte* currentByte = byteMessage;
    currentByte += sizeof(byte) * startIndex;
    memcpy(returnBytes, currentByte, size);

    return returnBytes;
}
