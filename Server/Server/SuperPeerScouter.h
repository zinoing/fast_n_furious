#pragma once
#include "Singleton.h"
#include <mutex>

class PacketManager;
class Client;
class RoomManager;
class Room;

class SuperPeerScouter : public Singleton<SuperPeerScouter>
{
private:
	static char* roomID;
	bool superPeerOn;
	std::mutex pingMutex;
	std::mutex scoutMutex;
public:
	SuperPeerScouter();
	~SuperPeerScouter();

	void sendPing(Room& room);

	void scoutSuperPeer(Client& client, Room& roomToFind);

	void setSuperPeerOn(bool value);
};

