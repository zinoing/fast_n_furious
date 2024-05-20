#include "SuperPeerScouter.h"
#include "PacketManager.h"
#include "Server.h"
#include "Room.h"

char* SuperPeerScouter::roomID = nullptr;

SuperPeerScouter::SuperPeerScouter() : superPeerOn(false) {};
SuperPeerScouter::~SuperPeerScouter() {};

void SuperPeerScouter::sendPing(Room& room) {
	if (superPeerOn)
		return;
	vector<Client*> players = room.getPlayers();
	for_each(players.begin(), players.end(), [&](Client* player) {
		// Server::Instance().sendMessage(*player, PacketManager::Instance().createPacket(2, PacketType::REQ_PING));
		player->sendMessage(PacketManager::Instance().createPacket(2, PacketType::REQ_PING));
		});
	superPeerOn = true;
}

void SuperPeerScouter::scoutSuperPeer(Client& client, Room& roomToFind) {
	if (client.getConnectionStatus() == ConnectionStatus::DISCONNECTED)
		return;

	this->scoutMutex.lock();
	if (roomToFind.getSuperPeer() == nullptr) {
		roomToFind.setSuperPeer(&client);
		
		vector<Client*> players = roomToFind.getPlayers();
		for_each(players.begin(), players.end(), [&](Client* player) {
				// 모든 peer들에게 superPeer 정보 전송
			    player->sendMessage(PacketManager::Instance().createPacket(36,
									PacketType::S_INFORM_SUPER_PEER, PacketManager::Instance().
									encodeSuperPeer(*roomToFind.getSuperPeer())));

				// 모든 peer들에게 peer group 내의 peer들의 정보 전송
				player->sendMessage(PacketManager::Instance().createPacket(2 + roomToFind.getPlayers().size() * 38,
									PacketType::S_INFORM_CLIENTS_INFORMATIONS,
									PacketManager::Instance().encodeClientsUDPAddress(roomToFind.getPlayers())));
			});

		printf("**************Here comes new super peer, port num: %d\n", client.getUDPSockPublicUdpPort());
		this->scoutMutex.unlock();
		return;
	}
	this->scoutMutex.unlock();
}

void SuperPeerScouter::setSuperPeerOn(bool value) {
	superPeerOn = value;
}