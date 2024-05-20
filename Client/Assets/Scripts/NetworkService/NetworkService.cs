using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

public struct IPEndPointPair
{
    public IPEndPoint privateEP;
    public IPEndPoint publicEP;

    public IPEndPointPair(IPEndPoint privateEP, IPEndPoint publicEP)
    {
        this.privateEP = privateEP;
        this.publicEP = publicEP;
    }
} 

public class NetworkService : Singleton<NetworkService>
{
    private string serverIPAddress = "192.168.0.6";
    private short serverPort = 7000;

    private Queue<byte[]> sendQueue;
    private Queue<byte[]> recvQueue;
    public Queue<byte[]> RecvQueue { get { return recvQueue; } }

    private SocketAsyncEventArgs sendArgs;
    private SocketAsyncEventArgs recvArgs;

    private object sendQueueLockObj;
    private object recvQueueLockObj;

    private LobbyManager lobbyManager;
    public LobbyManager LobbyManager
    {
        get { return lobbyManager; }
        set { lobbyManager = value; }
    }

    public NetworkService()
    {
        sendQueue = new Queue<byte[]>();
        recvQueue = new Queue<byte[]>();

        sendArgs = new SocketAsyncEventArgs();
        recvArgs = new SocketAsyncEventArgs();

        recvArgs.Completed += IO_Completed;
        sendArgs.Completed += IO_Completed;

        recvQueueLockObj = new object();
        sendQueueLockObj = new object();

        lobbyManager = null;
    }

    public void ConnectToServer()
    {
        var ep = new IPEndPoint(IPAddress.Parse(serverIPAddress), serverPort);
        ClientManager.Instance.MyClient.clntTCPSock.Connect(ep);

        //client.clientTCPEndPoint = new IPEndPoint(((IPEndPoint)client.clntTCPSock.LocalEndPoint).Address, ((IPEndPoint)client.clntTCPSock.LocalEndPoint).Port);
        //client.clientPrivateUDPEndPoint = new IPEndPoint(((IPEndPoint)client.clntTCPSock.LocalEndPoint).Address, ((IPEndPoint)client.udpClient.Client.LocalEndPoint).Port);

        // set private ep;
        var tcpPrivateEP = new IPEndPoint(IPAddress.Parse(GetPrivateIP()), ((IPEndPoint)ClientManager.Instance.MyClient.clntTCPSock.LocalEndPoint).Port);
        var udpPrivateEP = new IPEndPoint(IPAddress.Parse(GetPrivateIP()), ((IPEndPoint)ClientManager.Instance.MyClient.udpClient.Client.LocalEndPoint).Port);
        ClientManager.Instance.MyClient.clientTCPEndPoint = new IPEndPoint(tcpPrivateEP.Address, tcpPrivateEP.Port);
        //ClientManager.Instance.MyClient.ClientUDPEndPointPair = new IPEndPoint(udpPrivateEP.Address, udpPrivateEP.Port);
        ClientManager.Instance.MyClient.CurrentState = ClientState.CONNECTED;
        Debug.Log("Connected to Server");
    }

    public string GetPrivateIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }

        throw new Exception("Can't find IP address");
    }

    public void SendPacket(byte[] message)
    {
        lock(sendQueueLockObj)
        {
            sendQueue.Enqueue(message);
        }
        sendArgs.SetBuffer(message, 0, message.Length);
        ClientManager.Instance.MyClient.clntTCPSock.SendAsync(sendArgs);
        return;
    }

    public void ReceivePacket()
    {
        byte[] checkPacket = new byte[1024];
        recvArgs.SetBuffer(checkPacket, 0, checkPacket.Length);
        ClientManager.Instance.MyClient.clntTCPSock.ReceiveAsync(recvArgs);
        return;
    }

    private void IO_Completed(object sender, SocketAsyncEventArgs e)
    {
        switch (e.LastOperation)
        {
            case SocketAsyncOperation.Receive:
                lock(recvQueueLockObj)
                {
                    if (e.BytesTransferred == 0)
                    {
                        ReceivePacket();
                        return;
                    }

                    int offset = 0;
                    if (e.BytesTransferred > e.Buffer[offset])
                    {
                        while (e.BytesTransferred > offset)
                        {
                            byte[] segmentatedData = new byte[e.Buffer[offset]];
                            Buffer.BlockCopy(e.Buffer, offset, segmentatedData, 0, e.Buffer[offset]);
                            recvQueue.Enqueue(segmentatedData);
                            offset += e.Buffer[offset];

                        }
                        ReceivePacket();
                        return;
                    }

                    byte[] receivedData = new byte[e.BytesTransferred];
                    Buffer.BlockCopy(e.Buffer, 0, receivedData, 0, e.BytesTransferred);
                    recvQueue.Enqueue(receivedData);
                    ReceivePacket();
                }
                break;
            case SocketAsyncOperation.Send:
                lock(sendQueueLockObj)
                {
                    sendQueue.Dequeue();
                }
                break;
            default:
                throw new ArgumentException("NetworkService: The last operation completed on the socket was not a receive or send");
        }
    }

    public void HandleReceivedPacket(byte[] packet)
    {
        try
        {
            Debug.Log("Handle tag: " + (PacketType)packet[1]);
            switch ((PacketType)packet[1])
            {
                case PacketType.S_ANS_ID:
                    int id = (int)packet[2];
                    ClientManager.Instance.MyClient.ClientID = id;
                    break;
                case PacketType.S_ANS_CREATE:
                    RoomSetting roomSetting = PacketManager.Instance.DecodeRoomSetting(packet);
                    lobbyManager.CreateRoom(roomSetting);
                    break;
                case PacketType.S_ANS_ENTER:
                    lobbyManager.DestroyAllRooms();
                    SceneManager.LoadScene("Loading");
                    break;
                case PacketType.S_ANS_LEAVE:
                    break;
                case PacketType.S_REQ_PRIVATE_EP:
                    // send id & private ep
                    byte[] idBytes = BitConverter.GetBytes(ClientManager.Instance.MyClient.ClientID);
                    Debug.Log("ClientID: " + ClientManager.Instance.MyClient.ClientID);

                    // set private ep
                    IPEndPoint privateEP = new IPEndPoint(IPAddress.Parse(GetPrivateIP()), ((IPEndPoint)ClientManager.Instance.MyClient.udpClient.Client.LocalEndPoint).Port);
                    IPEndPointPair pair = ClientManager.Instance.MyClient.ClientUDPEndPointPair;
                    pair.privateEP = privateEP;
                    ClientManager.Instance.MyClient.ClientUDPEndPointPair = pair;                    
                    
                    byte[] localEndPointBytes = PacketManager.Instance.EncodeString(privateEP.ToString() + '\0');
                    byte[] bytesToSend = new byte[idBytes.Length + localEndPointBytes.Length];
                    Buffer.BlockCopy(idBytes, 0, bytesToSend, 0, idBytes.Length);
                    Buffer.BlockCopy(localEndPointBytes, 0, bytesToSend, idBytes.Length, localEndPointBytes.Length);

                    P2PNetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_ANS_PRIVATE_EP, bytesToSend),
                                                          new IPEndPoint(IPAddress.Parse(serverIPAddress), serverPort + 1));
                    // send packet to server's second udp socket
                    P2PNetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_ANS_PRIVATE_EP, bytesToSend),
                                                          new IPEndPoint(IPAddress.Parse(serverIPAddress), serverPort + 2));

                    Debug.Log("NetworkService: Send private endpoint: " + ((IPEndPoint)ClientManager.Instance.MyClient.udpClient.Client.LocalEndPoint));
                    break;
                case PacketType.S_INFORM_CLIENTS_INFORMATIONS:
                    Debug.Log("*****************NetworkService:received clients' informations");
                    List<Client> clients = PacketManager.Instance.DecodeClientsUDPAddress(packet);
                    SuperPeerManager.Instance.SetClients(clients);
                    ClientManager.Instance.MyClient.ClientUDPEndPointPair = SuperPeerManager.Instance.FindClient(ClientManager.Instance.MyClient.ClientUDPEndPointPair.privateEP).ClientUDPEndPointPair;

                    SceneManager.LoadScene(3);
                    SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_READY_TO_PLAY));

                    break;
                case PacketType.S_INFORM_SUPER_PEER:
                    IPEndPointPair superPeerEndPointPair = PacketManager.Instance.DecodeSuperPeer(packet);

                    if (IPEndPoint.Equals(superPeerEndPointPair.privateEP, ClientManager.Instance.MyClient.ClientUDPEndPointPair.privateEP) ||
                       IPEndPoint.Equals(superPeerEndPointPair.publicEP, ClientManager.Instance.MyClient.ClientUDPEndPointPair.publicEP))
                    {
                        Debug.Log("*****************NetworkService: I'm super peer");
                        SuperPeerManager.Instance.SuperPeer = ClientManager.Instance.MyClient;
                    }
                    else
                    {
                        SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair = superPeerEndPointPair;
                        TimeManager.Instance.ReqTimeToSuperPeer();
                    }

                    break;
                case PacketType.S_REQ_PLAY:
                    SuperPeerManager.Instance.inGame = true;
                    SuperPeerManager.Instance.SetPositions();

                    for (int i = 0; i < SuperPeerManager.Instance.GetClients().Count; i++)
                    {
                        Client peer = SuperPeerManager.Instance.GetClients()[i];



                        /*
                        P2PNetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacketWithSequenceNum(PacketType.SP_INFORM_CAR_START_POSITION, 
                                                              PacketManager.Instance.EncodeDetailCarState(peer.DetailCarInfo)), peer.clientPublicUDPEndPoint);
                        P2PNetworkService.Instance.SelectiveMulticast(PacketManager.Instance.CreatePacketWithSequenceNum(PacketType.SP_INFORM_OTHER_CAR_START_POSITION,
                                                                      PacketManager.Instance.EncodeDetailCarState(peer.DetailCarInfo)), peer.clientPublicUDPEndPoint);
                        
                        Debug.Log("Send to peer's public end point: " + peer.clientPublicUDPEndPoint.ToString());
                        */

                        P2PNetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacketWithSequenceNum(PacketType.SP_INFORM_CAR_START_POSITION,
                                                              PacketManager.Instance.EncodeDetailCarState(peer.DetailCarInfo)), peer.ClientUDPEndPointPair);
                        P2PNetworkService.Instance.SelectiveMulticast(PacketManager.Instance.CreatePacketWithSequenceNum(PacketType.SP_INFORM_OTHER_CAR_START_POSITION,
                                                                      PacketManager.Instance.EncodeDetailCarState(peer.DetailCarInfo)), peer.ClientUDPEndPointPair);
                    }
                    break;
                case PacketType.S_REQ_DESTROY_ROOM_IN_LOBBY:
                    if (lobbyManager == null)
                        break;
                    RoomSetting roomSettingToDestroy = PacketManager.Instance.DecodeRoomSetting(packet);
                    lobbyManager.DestroyRoom(roomSettingToDestroy);
                    break;
                case PacketType.S_ANS_WRONG_PASSWORD:
                    lobbyManager.PutWrongPassword();
                    break;
                case PacketType.REQ_PING:
                    SendPacket(PacketManager.Instance.CreatePacket(PacketType.ANS_PING));
                    break;
                case PacketType.S_ANS_GAME_OVER:
                    WaitToEndGame();
                    break;
                default:
                    break;
            }
            return;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private async Task WaitToEndGame()
    {
        await Task.Delay(5000);
        SuperPeerManager.Instance.inGame = false;
        Debug.Log("Game Finished");
        SceneManager.LoadScene("Lobby");
    }

    /*
    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "Lobby" && lobbyManager == null)
        {
            lobbyManager = GameObject.Find("LobbyManager").GetComponent<LobbyManager>();
        }

        if (client.CurrentState == ClientState.DISCONNECTED)
            return;

        if (recvQueue.Count > 0)
        {
            HandleReceivedPacket(recvQueue.Peek());
            recvQueue.Dequeue();
        }
    }
    */
}
