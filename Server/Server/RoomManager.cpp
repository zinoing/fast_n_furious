#include "RoomManager.h"
#include "Server.h"
#include "PacketManager.h"
#include "Stdfax.h"
#include <string>

RoomManager::RoomManager()
    : roomId(1) {}

RoomManager::~RoomManager() {}

void RoomManager::updateRooms(Client& client) {
    if (rooms.size() == 0)
        return;

    roomMutex.lock();
    for (int i = 0; i < rooms.size(); i++) {
        if (rooms[i]->getRoomState() == RoomState::IN_GAME)
            continue;

        RoomSetting roomSetting = rooms[i]->getRoomSetting();

        client.sendMessage(PacketManager::Instance().createPacket(2 + ROOMSETTING_WITHOUT_PASSWORD_SIZE,
                            PacketType::S_ANS_CREATE,
                            PacketManager::Instance().encodeRoomSetting(roomSetting)));
    }
    roomMutex.unlock();
}

void RoomManager::createRoom(Client& client, RoomSetting& roomSetting) {

    char roomId[4];
    std::sprintf(roomId, "%d", this->roomId++);
    memcpy(roomSetting.roomID, roomId, 4);

    client.setRoomID(roomId);

    Room* newRoom = new Room(client, roomSetting);
    rooms.push_back(newRoom);
    printf("room created\n");
    return;
}

void RoomManager::destroyRoom(char* roomID) {
    Room* roomToDestroy = findRoom(roomID);

    if (roomToDestroy == nullptr)
        return;

    rooms.erase(std::remove_if(rooms.begin(), rooms.end(),
        [&roomToDestroy](Room* room) {
            return room->getRoomID() == roomToDestroy->getRoomID();
            }),
        rooms.end());

    delete roomToDestroy;
    printf("room destroyed\n");
    return;
}

vector<Room*> RoomManager::getRooms() {
    return rooms;
}

Room* RoomManager::findRoom(char* roomID) {
    char roomIDStr[5]; // 널 종료 문자를 포함한 문자열을 저장할 버퍼
    sprintf(roomIDStr, "%d", *reinterpret_cast<int*>(roomID)); // 4바이트 데이터를 문자열로 변환

    for (int i = 0; i < rooms.size(); i++) {
        if (strcmp(roomIDStr, rooms[i]->getRoomID()) == 0)
            return rooms[i];
    }
    return nullptr;
}

void RoomManager::enterRoom(Client& client, char* roomID) {
    Room* foundRoom = findRoom(roomID);

    if (foundRoom == nullptr) {
        return;
    }

    if (foundRoom->checkFull()) {
        ErrorHandling("foundRoom.checkFull()");
    }

    client.setRoomID(roomID);
    foundRoom->enter(client);

    return;
}

RoomSetting RoomManager::recvRoomSetting(char* packetToHandle) {
    
    RoomSetting roomSetting;

    byte* byteMessage = static_cast<byte*>(static_cast<void*>(packetToHandle));
    byte* currentByte = byteMessage;
    currentByte += sizeof(byte) * 2; // skip size & tag

    memcpy(&roomSetting.roomID, currentByte, sizeof(roomSetting.roomID));
    currentByte += sizeof(roomSetting.roomID);

    memcpy(&roomSetting.roomName, currentByte, sizeof(roomSetting.roomName));
    currentByte += sizeof(roomSetting.roomName);

    int maxNum = static_cast<int>(*currentByte);
    roomSetting.maximumCapacity = maxNum;
    currentByte += sizeof(byte);

    memcpy(&roomSetting.isPublic, currentByte, sizeof(byte));
    currentByte += sizeof(byte);

    if (roomSetting.isPublic)
        return roomSetting;

    memcpy(&roomSetting.roomPassword, currentByte, sizeof(roomSetting.roomPassword));
    currentByte += sizeof(roomSetting.roomPassword);

    printf("received room setting\n");
    printf("roomName: %s\n", roomSetting.roomName);
    return roomSetting;
}