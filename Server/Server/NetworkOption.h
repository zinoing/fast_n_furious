#pragma once
#include "Stdfax.h"

struct EndPoint {
    string ipAddress;
    string portNumber;
    string fullEP;

    EndPoint() : ipAddress(""), portNumber(""), fullEP("") {}

    EndPoint(string ip, string port, string fullEP) : ipAddress(ip), portNumber(port), fullEP(fullEP) {}
};

struct EndPointInfo {
    EndPoint privateEP;
    EndPoint publicEP;
    bool isSet;
};

enum class NAT_TYPE {
    NON_SYMMETRIC,
    SYMMETRIC,
};

struct NAT_OPTION {
    NAT_TYPE type;
    bool isSet;
};

enum class ConnectionStatus {
    CONNECTED,
    DISCONNECTED
};

typedef struct
{
    SOCKET clntSock;
    SOCKADDR_IN clntAddr;
    NAT_OPTION NATOption;
    EndPointInfo udpEpInfo;
} CLIENT_OPTION;