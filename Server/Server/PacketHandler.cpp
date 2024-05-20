#include "PacketHandler.h"
#include "PacketManager.h"
#include "HolePunchingHandler.h"
#include "RoomManager.h"
#include "Server.h"
#include "Client.h"

void PacketHandler::handlePacket(Client& client, char* packetToHandle) {
    if (client.getConnectionStatus() == ConnectionStatus::DISCONNECTED) {
        return;
    }

    int packetSize = packetToHandle[0];
    int tag = packetToHandle[1];

    printf("Handle %d tag\n", tag);

    switch (tag) {
    case (int)PacketType::C_REQ_ID:
    {
        char id = (char)client.getClientID();
        client.sendMessage(PacketManager::Instance().createPacket(3, PacketType::S_ANS_ID, &id));
        break;
    }
    case (int)PacketType::C_REQ_CREATE:
        RoomSetting roomSetting = RoomManager::Instance().recvRoomSetting(packetToHandle);
        RoomManager::Instance().createRoom(client, roomSetting);
        Server::Instance().multicast(PacketManager::Instance().createPacket(ROOMSETTING_WITHOUT_PASSWORD_SIZE + 2,
            PacketType::S_ANS_CREATE, PacketManager::Instance().encodeRoomSettingWithoutPassword(roomSetting)),
            ROOMSETTING_WITHOUT_PASSWORD_SIZE + 2);
        break;
    case (int)PacketType::C_REQ_ENTER:
    {
        char roomID[4] = { 0 };
        memcpy(roomID, PacketManager::Instance().extractFromPacket(2, 4, packetToHandle), 4);
        Room* roomToEnter = RoomManager::Instance().findRoom(roomID);

        if (roomToEnter == nullptr)
            break;

        if (!roomToEnter->getRoomSetting().isPublic &&
            !roomToEnter->checkPassword(PacketManager::Instance().extractFromPacket(6, 4, packetToHandle))) {
            client.sendMessage(PacketManager::Instance().createPacket(2, PacketType::S_ANS_WRONG_PASSWORD));
            RoomManager::Instance().updateRooms(client);
            break;
        }

        client.sendMessage(PacketManager::Instance().createPacket(2, PacketType::S_ANS_ENTER));
        RoomManager::Instance().enterRoom(client, roomID);

        if (roomToEnter->checkFull()) {
            for (int i = 0; i < roomToEnter->getPlayers().size(); i++) {
                roomToEnter->getPlayers()[i]->sendMessage(PacketManager::Instance().createPacket(2, PacketType::S_REQ_PRIVATE_EP));
            }
        }
        break;
    }
    case (int)PacketType::C_REQ_LEAVE:
        break;
    case (int)PacketType::C_ANS_PRIVATE_EP:
    {
        char clientID[4] = { 0 };
        memcpy(&clientID, PacketManager::Instance().extractFromPacket(2, 4, packetToHandle), 4);

        int lengthOfEndPoint = packetSize - 2 - 4;
        char* endPoint = PacketManager::Instance().extractFromPacket(6, lengthOfEndPoint, packetToHandle);
        EndPoint privateEP = PacketManager::Instance().decodeEndPoint(endPoint);
        client.setUDPSockPrivateEP(privateEP);

        cout << "id: " << client.getClientID() << " | public EP: " << client.getUDPSockPublicEP().fullEP + " | private EP: " + client.getUDPSockPrivateEP().fullEP << endl;
        cout << "NAT type is " << (int)client.getNATType() << endl;

        Room* roomToFind = RoomManager::Instance().findRoom(client.getRoomID());

        // 모든 player들의 public 및 private ep가 설정이 되었다면
        if (roomToFind->checkAllPlayersUDPSet()) {
            cout << "start send ping" << endl;
            roomToFind->getScouter()->sendPing(*roomToFind);
        }
        break;
    }
    case (int)PacketType::C_READY_TO_PLAY:
    {
        Room* roomToFind = RoomManager::Instance().findRoom(client.getRoomID());
        client.setReadyToPlay(true);

        if (roomToFind->checkAllPlayersReady()) {
            roomToFind->setRoomState(RoomState::IN_GAME);
            Server::Instance().multicast(PacketManager::Instance().createPacket(ROOMSETTING_WITHOUT_PASSWORD_SIZE + 2,
                PacketType::S_REQ_DESTROY_ROOM_IN_LOBBY,
                PacketManager::Instance().encodeRoomSettingWithoutPassword(roomToFind->getRoomSetting())),
                ROOMSETTING_WITHOUT_PASSWORD_SIZE + 2);
            client.sendMessage(PacketManager::Instance().createPacket(2, PacketType::S_REQ_PLAY));
        }
        break;
    }
    case (int)PacketType::ANS_PING:
    {
        Room* roomToFind = RoomManager::Instance().findRoom(client.getRoomID());
        roomToFind->getScouter()->scoutSuperPeer(client, *roomToFind);
        break;
    }
    case (int)PacketType::SP_REQ_GAME_OVER:
    {
        Room* roomToFind = RoomManager::Instance().findRoom(client.getRoomID());

        for (int i = 0; i < roomToFind->getPlayers().size(); i++) {
            roomToFind->getPlayers()[i]->setReadyToPlay(false);
        }

        Server::Instance().multicastInRoom(*roomToFind, PacketManager::Instance().createPacket(2, PacketType::S_ANS_GAME_OVER), 2);
        RoomManager::Instance().destroyRoom(client.getRoomID());
        RoomManager::Instance().updateRooms(client);
        break;
    }
    case (int)PacketType::C_INFORM_LOST_SP:
    {
        Room* roomToFind = RoomManager::Instance().findRoom(client.getRoomID());
        roomToFind->removePlayer(roomToFind->getSuperPeer());
        roomToFind->setSuperPeer(nullptr);
        roomToFind->getScouter()->setSuperPeerOn(false);
        roomToFind->getScouter()->sendPing(*roomToFind);
        break;
    }
    default:
        break;
    }
}