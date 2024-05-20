#pragma once
#include "Room.h"
#include "Singleton.h"
#include <vector>
using namespace std;

class Server;
class PacketManager;
class RoomManager;

class RoomManager : public Singleton<RoomManager> {
private:
public:
    RoomManager();

    ~RoomManager();

    void updateRooms(Client& client);

    void createRoom(Client& client, RoomSetting& roomSetting);

    void destroyRoom(char* roomID);

    vector<Room*> getRooms();

    Room* findRoom(char* roomID);

    void enterRoom(Client& client, char* roomID);

    RoomSetting recvRoomSetting(char* packetToHandle);

private:
    int roomId;
    vector<Room*> rooms;
    mutex roomMutex;
};