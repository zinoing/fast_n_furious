#include "Stdfax.h"
#include "Server.h"
#include "Client.h"

int main()
{   
    // To detect memory leaks
    _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);

    Server::Instance().run();

    int idx = 0;
    while (1) {
        Client* client = Server::Instance().connectToClient();

        if (client == nullptr)
            continue;

        HANDLE iocpHandle = client->bindIOCompletionPort(Server::Instance().getCompletionPort());
        client->receivePacket();
    }

    Server::Instance().close();

    return 0;
}