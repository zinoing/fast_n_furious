#pragma once
#include "Singleton.h"
#include "Client.h"

class Server;
struct RoomSetting;

enum class PacketType
{
    C_REQ_ID = 90,
    C_REQ_CREATE,
    C_REQ_ENTER,
    C_REQ_LEAVE,
    C_ANS_PRIVATE_EP,
    C_READY_TO_PLAY,

    S_ANS_ID,
    S_ANS_CREATE,
    S_ANS_ENTER,
    S_ANS_WRONG_PASSWORD,
    S_ANS_LEAVE,
    S_REQ_PRIVATE_EP,
    S_INFORM_CLIENTS_INFORMATIONS,
    S_INFORM_SUPER_PEER,
    S_REQ_PLAY,
    S_REQ_DESTROY_ROOM_IN_LOBBY,
    SP_REQ_GAME_OVER,

    // Packet types for P2P network
    C_REQ_TIME,
    SP_ANS_TIME,

    P_ANS_CAR_SETTING_COMPLETED,
    P_UPDATE_CAR_STATE,

    SP_INFORM_CAR_START_POSITION,
    SP_INFORM_OTHER_CAR_START_POSITION,
    SP_REQ_START_PLAY,

    C_REQ_WIN,
    SP_ALERT_GAME_OVER,
    S_ANS_GAME_OVER,

    REQ_PING,
    ANS_PING,

    SP_REQ_HB,
    C_ANS_HB,
    SP_INFORM_LOST_PEER,
    C_INFORM_LOST_SP
};

class PacketManager : public Singleton< PacketManager>
{
private:
public:
    PacketManager();
	~PacketManager();

	char* createPacket(int size, PacketType tag, char* detail = nullptr);
	char* encodeRoomSetting(RoomSetting& roomSetting);
	char* encodeRoomSettingWithoutPassword(RoomSetting& roomSetting);
    EndPoint decodeEndPoint(char* packet);
    unsigned int decodePort(char* packet);
    char* encodeSuperPeer(Client& superPeer);
    char* encodeClientsUDPAddress(vector<Client*> clients);
	char* extractFromPacket(int startIndex, int size, char* packet);
};