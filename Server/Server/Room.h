#pragma once
#include "Client.h"
#include "SuperPeerScouter.h"
#include <vector>

struct RoomSetting;

enum class RoomState {
    IN_LOBBY,
    IN_GAME,
};

struct RoomSetting {
    char roomID[4];
    char roomName[20];
    char maximumCapacity;
    bool isPublic;
    char roomPassword[4];
};

class Room {
private:
public:
    Room(Client& client, const RoomSetting& roomSetting);
    ~Room();
    RoomSetting& getRoomSetting();
    char* getRoomID();
    vector<Client*> getPlayers();
    void removePlayer(Client* clientToRemove);
    bool checkFull();
    bool checkPassword(char* roomPassword) const;
    bool checkAllPlayersUDPSet();
    bool checkAllPlayersResponsiveToPing();
    bool checkAllPlayersReady();
    void enter(Client& client);
    Client* getSuperPeer();
    void setSuperPeer(Client* client);
    RoomState getRoomState();
    void setRoomState(RoomState state);
    SuperPeerScouter* getScouter();
private:
    RoomState roomState;
    RoomSetting* roomSetting;
    vector<Client*> players;
    Client* superPeer;
    SuperPeerScouter* scouter;
};

