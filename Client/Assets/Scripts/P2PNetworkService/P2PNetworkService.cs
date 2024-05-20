using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using System.Data.SqlTypes;

public struct RecvInfo
{
    public IPEndPoint sender;
    public byte[] packet;

    public RecvInfo(IPEndPoint sender, byte[] packet)
    {
        this.sender = sender;
        this.packet = packet;
    }
}

public class P2PNetworkService : Singleton<P2PNetworkService>
{
    private GameManager gameManager;

    private Queue<RecvInfo> recvQueue;
    public Queue<RecvInfo> RecvQueue { get { return recvQueue; } }

    public P2PNetworkService()
    {
        recvQueue = new Queue<RecvInfo>();
    }

    public void SendPacket(byte[] message, IPEndPoint dstNetworkAddress)
    {
        try
        {
            ClientManager.Instance.MyClient.udpClient.SendAsync(message, message.Length, dstNetworkAddress);
        }
        catch (SocketException e)
        {
            Debug.Log("Can't find receiver");
            Debug.Log(e.Message);
        }
        return;
    }

    public void SendPacket(byte[] message, IPEndPointPair dstNetworkAddressPair)
    {
        try
        {
            ClientManager.Instance.MyClient.udpClient.SendAsync(message, message.Length, dstNetworkAddressPair.privateEP);
        }
        catch
        {
            try
            {
                Debug.Log("try to send with public ip");
                ClientManager.Instance.MyClient.udpClient.SendAsync(message, message.Length, dstNetworkAddressPair.publicEP);
            }
            catch (SocketException e)
            {
                Debug.Log("Can't find receiver");
                Debug.Log("dstNetworkAddressPair.privateEP: " + dstNetworkAddressPair.privateEP);
                Debug.Log("dstNetworkAddressPair.publicEP: " + dstNetworkAddressPair.publicEP);
                Debug.Log(e.Message);
            }
        }
        return;
    }

    public void SendPacketToSuperPeer(byte[] message)
    {
        try
        {
            if (IPAddress.Equals(ClientManager.Instance.MyClient.ClientUDPEndPointPair.publicEP.Address, SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.publicEP.Address))
            {
                ClientManager.Instance.MyClient.udpClient.SendAsync(message, message.Length, SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.privateEP);
            }
            else
            {
                ClientManager.Instance.MyClient.udpClient.SendAsync(message, message.Length, SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.publicEP);
            }
            return;
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }
    public void MulticastPacket(byte[] packet)
    {
        try
        {
            for (int i = 0; i < SuperPeerManager.Instance.GetClients().Count; i++)
            {
                IPEndPointPair dstIPEndPointPair = SuperPeerManager.Instance.GetClients()[i].ClientUDPEndPointPair;
                SendPacket(packet, dstIPEndPointPair);
            }
            return;
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }

    public void SelectiveMulticast(byte[] packet, IPEndPointPair excludedAddressPair)
    {
        try
        {
            for (int i = 0; i < SuperPeerManager.Instance.GetClients().Count; i++)
            {
                IPEndPointPair dstIPEndPointPair = SuperPeerManager.Instance.GetClients()[i].ClientUDPEndPointPair;
                if (IPEndPoint.Equals(excludedAddressPair.privateEP, dstIPEndPointPair.privateEP) ||
                    IPEndPoint.Equals(excludedAddressPair.publicEP, dstIPEndPointPair.publicEP))
                    continue;

                SendPacket(packet, dstIPEndPointPair);
            }
            return;
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }

    public void SelectiveMulticast(byte[] packet, IPEndPoint excludedAddress)
    {
        try
        {
            for (int i = 0; i < SuperPeerManager.Instance.GetClients().Count; i++)
            {
                IPEndPointPair dstIPEndPointPair = SuperPeerManager.Instance.GetClients()[i].ClientUDPEndPointPair;
                if (IPEndPoint.Equals(excludedAddress, dstIPEndPointPair.privateEP) ||
                    IPEndPoint.Equals(excludedAddress, dstIPEndPointPair.publicEP))
                    continue;


                SendPacket(packet, dstIPEndPointPair);
            }
            return;
        }
        catch (SocketException e)
        {
            Debug.Log(e.Message);
        }
    }
    public void ReceivePacket()
    {
        try
        {
            ClientManager.Instance.MyClient.udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), null);
            return;
        }
        catch(SocketException e)
        {
            Debug.Log(e.Message);
        }
    }
    private void ReceiveCallback(IAsyncResult result)
    {
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            byte[] receivedData = ClientManager.Instance.MyClient.udpClient.EndReceive(result, ref sender);
            RecvInfo recvInfo = new RecvInfo(sender, receivedData);

            recvQueue.Enqueue(recvInfo);
        }
        catch (SocketException e)
        {
            Debug.Log("Can't find sender");
            // 자신이 superPeer가 아닐 경우 => superPeer와의 통신 불가
            if(ClientManager.Instance.MyClient.ClientID != SuperPeerManager.Instance.SuperPeer.ClientID && SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair.publicEP != null)
            {
                Debug.Log("SuperPeer connection Lost");
                SuperPeerManager.Instance.RemoveClient(SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair);
                SuperPeerManager.Instance.SuperPeer = null;
                NetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_INFORM_LOST_SP));
            }
            ReceivePacket();
            Debug.Log(e.Message);
        }
    }
    public void HandleReceivedPacket(RecvInfo RecvInfo)
    {
        if (SceneManager.GetActiveScene().name == "Main")
        {
            if (gameManager == null)
                gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        }

        try
        {
            switch ((PacketType)RecvInfo.packet[1])
            {
                case PacketType.SP_INFORM_CAR_START_POSITION:
                    DetailCarInfo startCarInfo = PacketManager.Instance.DecodeDetailCarState(RecvInfo.packet);
                    gameManager.CreateMyCar(startCarInfo);
                    int numOfCarsCreated = (int)ClientManager.Instance.MyClient.InRoomSetting.maximumCapacity;
                    if(gameManager.NumOfCarsCreated == numOfCarsCreated)
                        SendPacket(PacketManager.Instance.CreatePacket(PacketType.P_ANS_CAR_SETTING_COMPLETED), SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair);
                    break;
                case PacketType.SP_INFORM_OTHER_CAR_START_POSITION:
                    DetailCarInfo otherCarInfo = PacketManager.Instance.DecodeDetailCarState(RecvInfo.packet);
                    gameManager.CreateOtherCar(otherCarInfo);
                    numOfCarsCreated = (int)ClientManager.Instance.MyClient.InRoomSetting.maximumCapacity;
                    if (gameManager.NumOfCarsCreated == numOfCarsCreated)
                        SendPacket(PacketManager.Instance.CreatePacket(PacketType.P_ANS_CAR_SETTING_COMPLETED), SuperPeerManager.Instance.SuperPeer.ClientUDPEndPointPair);
                    break;
                case PacketType.P_ANS_CAR_SETTING_COMPLETED:
                    ++gameManager.NumOfPlayersReadyToPlay;
                    if (gameManager.NumOfPlayersReadyToPlay == SuperPeerManager.Instance.GetClients().Count)
                    {
                        MulticastPacket(PacketManager.Instance.CreatePacket(PacketType.SP_REQ_START_PLAY));
                        SuperPeerManager.Instance.SendHeartbeat();
                    }
                    break;
                case PacketType.SP_REQ_START_PLAY:
                    gameManager.ReadyToUpdate(ClientManager.Instance.MyClient.ClientID);
                    Debug.Log("Start Play");
                    break;
                case PacketType.C_REQ_WIN:
                    MulticastPacket(PacketManager.Instance.CreatePacket(PacketType.SP_ALERT_GAME_OVER));
                    NetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.SP_REQ_GAME_OVER));
                    break;
                case PacketType.SP_ALERT_GAME_OVER:
                    gameManager.RemoveEndLine();
                    break;
                case PacketType.C_REQ_TIME:
                    SendPacket(PacketManager.Instance.CreatePacket(PacketType.SP_ANS_TIME), RecvInfo.sender);
                    break;
                case PacketType.SP_ANS_TIME:
                    TimeManager.Instance.CheckDiffTick();
                    break;
                case PacketType.P_UPDATE_CAR_STATE:
                    DetailCarInfo updatedCarInfo = PacketManager.Instance.DecodeDetailCarState(RecvInfo.packet);

                    ulong packetNum = BitConverter.ToUInt64(RecvInfo.packet, 2);
                    Client sender = SuperPeerManager.Instance.FindClient(updatedCarInfo.clientID);
                    if (sender.PreviousPacketNum > packetNum)
                    {
                        Debug.Log("P2PNetworkService: current packet num is " + sender.PreviousPacketNum + ", Received packet num is " + packetNum + " ignored the packet");
                        break;
                    }
                    sender.PreviousPacketNum = packetNum;
                    gameManager.UpdateCar(updatedCarInfo);
                    break;
                case PacketType.SP_REQ_HB:
                    SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_ANS_HB), RecvInfo.sender);
                    Debug.Log("Received Hearbeat");
                    break;
                case PacketType.C_ANS_HB:
                    SuperPeerManager.Instance.FindClient(RecvInfo.sender).AnsweredHeartbeat = true;
                    ++SuperPeerManager.Instance.numOfClientsAnsweredHeratbeat;
                    break;
                case PacketType.SP_INFORM_LOST_PEER:
                    IPEndPoint epOfLostPeer = PacketManager.Instance.DecodeIPEndPoint(RecvInfo.packet);
                    SuperPeerManager.Instance.RemoveClient(epOfLostPeer);
                    Debug.Log("Removed client,current client size: " + SuperPeerManager.Instance.GetClients().Count);
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
}
