#include "Room.h"
#include "RoomManager.h"
#include "Stdfax.h"
#include <string>

Room::Room(Client& client, const RoomSetting& roomSetting)
    :roomState(RoomState::IN_LOBBY), roomSetting(new RoomSetting()), superPeer(nullptr), scouter(new SuperPeerScouter())
{
    memcpy(this->roomSetting, &roomSetting, ROOMSETTING_SIZE);
    players.push_back(&client);
}

Room::~Room() {
    delete roomSetting;
    roomSetting = nullptr;
    players.clear();
    superPeer = nullptr;
}

RoomSetting& Room::getRoomSetting() {
    return *roomSetting;
}

char* Room::getRoomID() {
    return roomSetting->roomID;
}

vector<Client*> Room::getPlayers() {
    return players;
}

void Room::removePlayer(Client* clientToRemove) {
    players.erase(remove_if(players.begin(), players.end(), [&](Client* client) {
        return client->getClientID() == clientToRemove->getClientID(); 
        }));
}

bool Room::checkFull()
{
    return (players.size() == roomSetting->maximumCapacity);
}

bool Room::checkPassword(char* roomPassword) const {
    return (strncmp(roomSetting->roomPassword, roomPassword, 4) == 0);
}


void Room::enter(Client& client)
{
    players.push_back(&client);
    return;
}

bool Room::checkAllPlayersUDPSet() {
    for (int i = 0; i < players.size(); i++) {
        if (players[i]->getUDPSockPublicUdpPort() == 0)
            return false;
        if (players[i]->getUDPSockPrivateUdpPort() == 0)
            return false;
    }
    return true;
}

bool Room::checkAllPlayersResponsiveToPing() {
    for (int i = 0; i < players.size(); i++) {
        if (players[i]->getPingResponse() == false)
            return false;
    }
    return true;
}

bool Room::checkAllPlayersReady() {
    for (int i = 0; i < players.size(); i++) {
        if (players[i]->isReadyToPlay() == false)
            return false;
    }
    return true;
}


Client* Room::getSuperPeer() {
    return superPeer;
}

void Room::setSuperPeer(Client* client) {
    superPeer = client;
}

RoomState Room::getRoomState() {
    return roomState;
}

void Room::setRoomState(RoomState state) {
    roomState = state;
}


SuperPeerScouter* Room::getScouter() {
    return scouter;
}
