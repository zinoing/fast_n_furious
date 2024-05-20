using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System;
using System.Text;
using System.Net;
using UnityEngine.UIElements;

// 패킷 구조: 사이즈/ id / 타입 / 데이터
// 비밀번호 확인 시: roomID / inputPassword

public enum PacketType
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

public class PacketManager : Singleton<PacketManager>
{
    public int roomSettingSize = 30;
    private static ulong udpPacketNum = 1;

    public byte[] CreatePacket(PacketType type, byte[] detail = null)
    {
        int packetSize = 2 + ((detail == null) ? 0 :detail.Length);

        List<byte> pakcetByteList = new List<byte>();

        // packet size
        pakcetByteList.Add(Convert.ToByte(packetSize));

        // type
        pakcetByteList.Add(Convert.ToByte((int)type));

        // data
        if(detail != null)
            pakcetByteList.AddRange(detail);

        return pakcetByteList.ToArray();
    }

    public byte[] CreatePacketWithSequenceNum(PacketType type, byte[] detail = null)
    {
        int packetSize = 6 + ((detail == null) ? 0 : detail.Length);

        List<byte> pakcetByteList = new List<byte>();

        // packet size
        pakcetByteList.Add(Convert.ToByte(packetSize));

        // type
        pakcetByteList.Add(Convert.ToByte((int)type));

        // packet number
        byte[] packetNum = BitConverter.GetBytes(udpPacketNum);
        pakcetByteList.AddRange(packetNum);

        // data
        if (detail != null)
            pakcetByteList.AddRange(detail);

        ++udpPacketNum;
        return pakcetByteList.ToArray();
    }

    public RoomSetting CreateRoomSetting(string roomName, int maximumCapacity, bool isPublic, string roomPassword = null)
    {
        RoomSetting roomSetting = new RoomSetting();

        roomSetting.roomID = new byte[4];
        Array.Clear(roomSetting.roomID, 0, 4);

        roomSetting.roomName = new byte[20];
        Array.Clear(roomSetting.roomName, 0, 20);

        roomSetting.roomPassword = new byte[4];
        Array.Clear(roomSetting.roomPassword, 0, 4);

        for (int i = 0; i < roomName.Length; i++)
        {
            roomSetting.roomName[i] = Convert.ToByte(roomName[i]);
        }

        roomSetting.maximumCapacity = Convert.ToByte(maximumCapacity);

        roomSetting.isPublic = Convert.ToByte(isPublic);

        if (isPublic == false)
        {
            if (roomPassword != null && roomPassword.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    roomSetting.roomPassword[i] = Convert.ToByte(roomPassword[i]);
                }
            }
        }

        Debug.Log("created roomSetting");
        return roomSetting;
    }

    public RoomSetting DecodeRoomSetting(byte[] packet)
    {
        RoomSetting roomSetting = new RoomSetting();
        int currentIndex = 2;

        roomSetting.roomID = new byte[4];
        Array.Clear(roomSetting.roomID, 0, 4);

        roomSetting.roomName = new byte[20];
        Array.Clear(roomSetting.roomName, 0, 20);

        for (int i = 0; i < roomSetting.roomID.Length; i++)
        {
            roomSetting.roomID[i] = packet[currentIndex];
            ++currentIndex;
        }

        for (int i = 0; i < roomSetting.roomName.Length; i++)
        {
            roomSetting.roomName[i] = packet[currentIndex];
            ++currentIndex;
        }

        roomSetting.maximumCapacity = packet[currentIndex++];

        roomSetting.isPublic = packet[currentIndex++];

        return roomSetting;
    }

    public byte[] EncodeRoomSetting(RoomSetting roomSetting)
    {
        List<byte> roomSettingByteList = new List<byte>();

        roomSettingByteList.AddRange(roomSetting.roomID);
        roomSettingByteList.AddRange(roomSetting.roomName);
        roomSettingByteList.Add(roomSetting.maximumCapacity);
        roomSettingByteList.Add(Convert.ToByte(roomSetting.isPublic));
        roomSettingByteList.AddRange(roomSetting.roomPassword);

        return roomSettingByteList.ToArray();
    }

    public string DecodeIPAddress(byte[] ipAddressBytes)
    {
        StringBuilder stringBuilder = new StringBuilder();

        for (int i = 0; i < ipAddressBytes.Length && ipAddressBytes[i] != 0; i++)
        {
            char decodedChar = (char)ipAddressBytes[i];
            stringBuilder.Append(decodedChar);
        }

        return stringBuilder.ToString();
    }

    public IPEndPointPair DecodeSuperPeer(byte[] packet)
    {
        int currentIndex = 2;

        // decode private end point
        byte[] privateIpAddressBytes = new byte[15];
        Array.Copy(packet, currentIndex, privateIpAddressBytes, 0, 15);
        currentIndex += 15;
        string privateIpAddressString = DecodeIPAddress(privateIpAddressBytes);

        Debug.Log(privateIpAddressString);


        byte[] privatePortBytes = new byte[2];
        Array.Copy(packet, currentIndex, privatePortBytes, 0, 2);
        currentIndex += 2;
        ushort privatePort = BitConverter.ToUInt16(BitConverter.GetBytes(BitConverter.ToInt16(privatePortBytes, 0)), 0);
        IPAddress privateIpAddress = IPAddress.Parse(privateIpAddressString);
        IPEndPoint superPeerPrivateEndPoint = new IPEndPoint(privateIpAddress, privatePort);

        Debug.Log(superPeerPrivateEndPoint.ToString());

        // decode public end point
        byte[] publicIpAddressBytes = new byte[15];
        Array.Copy(packet, currentIndex, publicIpAddressBytes, 0, 15);
        currentIndex += 15;
        string publicIpAddressString = DecodeIPAddress(publicIpAddressBytes);

        Debug.Log(publicIpAddressString);


        byte[] publicPortBytes = new byte[2];
        Array.Copy(packet, currentIndex, publicPortBytes, 0, 2);
        currentIndex += 2;
        ushort publicPort = BitConverter.ToUInt16(BitConverter.GetBytes(BitConverter.ToInt16(publicPortBytes, 0)), 0);
        IPAddress publicIpAddress = IPAddress.Parse(publicIpAddressString);
        IPEndPoint superPeerPublicEndPoint = new IPEndPoint(publicIpAddress, publicPort);

        Debug.Log(superPeerPublicEndPoint.ToString());

        IPEndPointPair endPointPair = new IPEndPointPair(superPeerPrivateEndPoint, superPeerPublicEndPoint);

        return endPointPair;
    }

    public List<Client> DecodeClientsUDPAddress(byte[] packet)
    {
        List<Client> clients = new List<Client>();
        int currentIndex = 2;
        int addressSize = 38;  // ID 4, 최대 IP주소 15, udpPort 2, end point가 private public 2개이므로 4 + 17*2 = 38
        int clientSize = (packet.Length - currentIndex) / addressSize;

        for (int i = 0; i < clientSize; i++)
        {
            byte[] id = new byte[4];
            Array.Copy(packet, currentIndex, id, 0, 4);
            int clientID = BitConverter.ToInt32(id, 0);
            currentIndex += 4;

            byte[] privateIpAddressBytes = new byte[15];
            Array.Copy(packet, currentIndex, privateIpAddressBytes, 0, 15);
            string privateIpAddressString = DecodeIPAddress(privateIpAddressBytes);
            currentIndex += 15;

            byte[] privateUdpPortBytes = new byte[2];
            Array.Copy(packet, currentIndex, privateUdpPortBytes, 0, 2);
            ushort privateUdpPort = BitConverter.ToUInt16(BitConverter.GetBytes(BitConverter.ToInt16(privateUdpPortBytes, 0)), 0);
            currentIndex += 2;

            byte[] publicIpAddressBytes = new byte[15];
            Array.Copy(packet, currentIndex, publicIpAddressBytes, 0, 15);
            string publicIpAddressString = DecodeIPAddress(publicIpAddressBytes);
            currentIndex += 15;

            byte[] publicUdpPortBytes = new byte[2];
            Array.Copy(packet, currentIndex, publicUdpPortBytes, 0, 2);
            ushort publicUdpPort = BitConverter.ToUInt16(BitConverter.GetBytes(BitConverter.ToInt16(publicUdpPortBytes, 0)), 0);
            currentIndex += 2;

            IPEndPoint privateEP = new IPEndPoint(IPAddress.Parse(privateIpAddressString), privateUdpPort);
            IPEndPoint publicEP = new IPEndPoint(IPAddress.Parse(publicIpAddressString), publicUdpPort);
            Client client = new Client(privateEP, publicEP);
            client.ClientID = clientID;

            Debug.Log("privateIpAddressString: " + privateIpAddressString);
            Debug.Log("privateUdpPort: " + privateUdpPort);
            Debug.Log("publicIpAddressString: " + publicIpAddressString);
            Debug.Log("publicUdpPort: " + publicUdpPort);

            clients.Add(client);
        }

        return clients;
    }

    public DetailCarInfo DecodeDetailCarState(byte[] packet)
    {
        DetailCarInfo currentCarInfo = new DetailCarInfo();
        int currentIndex = 10;

        byte[] gameTime = new byte[8];
        Array.Copy(packet, currentIndex, gameTime, 0, 8);
        currentCarInfo.gameTick = BitConverter.ToInt64(gameTime, 0);
        currentIndex += sizeof(long);

        byte[] id = new byte[4];
        Array.Copy(packet, currentIndex, id, 0, 4);
        currentCarInfo.clientID = BitConverter.ToInt32(id, 0);
        currentIndex += sizeof(int);

        // Serialize position
        float posX = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float posY = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float posZ = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);

        currentCarInfo.carState.position.x = posX;
        currentCarInfo.carState.position.y = posY;
        currentCarInfo.carState.position.z = posZ;

        // Serialize rotation
        float quaternionX = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float quaternionY = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float quaternionZ = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float quaternionW = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);

        currentCarInfo.carState.rotation.x = quaternionX;
        currentCarInfo.carState.rotation.y = quaternionY;
        currentCarInfo.carState.rotation.z = quaternionZ;
        currentCarInfo.carState.rotation.w = quaternionW;

        // Serialize currentSpped
        float speed = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        currentCarInfo.carState.currentSpeed = speed;

        float angularVelocityX = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float angularVelocityY = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);
        float angularVelocityZ = BitConverter.ToSingle(packet, currentIndex);
        currentIndex += sizeof(float);

        currentCarInfo.carState.currentAngularVelocity.x = angularVelocityX;
        currentCarInfo.carState.currentAngularVelocity.y = angularVelocityY;
        currentCarInfo.carState.currentAngularVelocity.z = angularVelocityZ;

        bool isReversing = BitConverter.ToBoolean(packet, currentIndex);
        currentIndex += sizeof(bool);
        currentCarInfo.carState.isReversing = isReversing;

        return currentCarInfo;
    }

    public byte[] EncodeDetailCarState(DetailCarInfo currentCarInfo)
    {
        List<byte> carStateList = new List<byte>();

        byte[] gameTickBytes = BitConverter.GetBytes(TimeManager.Instance.GameTime.Ticks);
        carStateList.AddRange(gameTickBytes);

        carStateList.AddRange(BitConverter.GetBytes(currentCarInfo.clientID));

        float positionX = currentCarInfo.carState.position.x;
        float positionY = currentCarInfo.carState.position.y;
        float positionZ = currentCarInfo.carState.position.z;
        carStateList.AddRange(BitConverter.GetBytes(positionX));
        carStateList.AddRange(BitConverter.GetBytes(positionY));
        carStateList.AddRange(BitConverter.GetBytes(positionZ));

        float rotationX = currentCarInfo.carState.rotation.x;
        float rotationY = currentCarInfo.carState.rotation.y;
        float rotationZ = currentCarInfo.carState.rotation.z;
        float rotationW = currentCarInfo.carState.rotation.w;
        carStateList.AddRange(BitConverter.GetBytes(rotationX));
        carStateList.AddRange(BitConverter.GetBytes(rotationY));
        carStateList.AddRange(BitConverter.GetBytes(rotationZ));
        carStateList.AddRange(BitConverter.GetBytes(rotationW));

        float speed = currentCarInfo.carState.currentSpeed;
        carStateList.AddRange(BitConverter.GetBytes(speed));

        float angularVelocityX = currentCarInfo.carState.currentAngularVelocity.x;
        float angularVelocityY = currentCarInfo.carState.currentAngularVelocity.y;
        float angularVelocityZ = currentCarInfo.carState.currentAngularVelocity.z;
        carStateList.AddRange(BitConverter.GetBytes(angularVelocityX));
        carStateList.AddRange(BitConverter.GetBytes(angularVelocityY));
        carStateList.AddRange(BitConverter.GetBytes(angularVelocityZ));

        bool isReversing = currentCarInfo.carState.isReversing;
        carStateList.AddRange(BitConverter.GetBytes(isReversing));

        return carStateList.ToArray();
    }

    public IPEndPoint DecodeIPEndPoint(byte[] packet)
    {
        byte[] addressBytes = new byte[4];
        Buffer.BlockCopy(packet, 0, addressBytes, 0, addressBytes.Length);

        byte[] portBytes = new byte[2];
        Buffer.BlockCopy(packet, addressBytes.Length, portBytes, 0, portBytes.Length);

        ushort port = (ushort)BitConverter.ToInt16(portBytes, 0);

        IPAddress ipAddress = new IPAddress(addressBytes);
        IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

        return endPoint;
    }

    public byte[] EncodeIPEndPoint(IPEndPoint epToEncode)
    {
        List<byte> ep = new List<byte>();

        byte[] addressBytes = epToEncode.Address.GetAddressBytes();
        ushort port = (ushort)epToEncode.Port;
        byte[] portBytes = BitConverter.GetBytes(port);

        ep.AddRange(addressBytes);
        ep.AddRange(portBytes);

        return ep.ToArray();
    }

    public byte[] EncodeUDPPort(IPEndPoint ep)
    {
        List<byte> UDPAddress = new List<byte>();

        ushort port = (ushort)ep.Port;
        UDPAddress.AddRange(BitConverter.GetBytes(port));

        return UDPAddress.ToArray();
    }

    public String DecodeString(byte[] packet)
    {
        return Encoding.UTF8.GetString(packet);
    }

    public byte[] EncodeString(String str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
}
