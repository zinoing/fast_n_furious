using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Text;
using System.Linq;

public class LobbyManager : MonoBehaviour
{
    List<RoomSetting> roomSettings = new List<RoomSetting>();
    List<GameObject> rooms = new List<GameObject>();

    static public GameObject selectedRoom;

    public RoomGenerator roomGenerator;

    [Header("MainPage")]
    public GameObject mainPage;
    public Button createRoomBttn;
    public Button enterRoomBttn;

    [Header("CreateRoom")]
    [SerializeField]private bool isPublic;

    public GameObject createRoomPage;
    public InputField createRoomName;
    public InputField maximumCapacity;
    public Button publicBttn;
    public Button privateBttn;
    public GameObject password;
    public InputField createRoomPassword;

    [Header("EnterRoom")]
    public GameObject enterRoomPage;
    public InputField enterRoomPassword;

    public void CreateRoomPageOn()
    {
        createRoomPage.SetActive(true);
        mainPage.SetActive(false);
        return;
    }

    public void EnterRoomPageOn()
    {
        mainPage.SetActive(false);

        if (Convert.ToInt32(selectedRoom.GetComponent<RoomController>().GetRoomSetting().isPublic) == 1)
        {
            Debug.Log("isPublic:" + Convert.ToInt32(selectedRoom.GetComponent<RoomController>().GetRoomSetting().isPublic));
            EnterRoomBttnClick();
            return;
        }
        enterRoomPage.SetActive(true);
    }

    public void PublicBttnClick()
    {
        if (password.activeSelf)
        {
            password.SetActive(false);
        }

        if (isPublic)
            return;
        isPublic = true;

        publicBttn.GetComponent<Image>().color = Color.gray;
        privateBttn.GetComponent<Image>().color = Color.white;
    }

    public void PrivateBttnClick()
    {
        if (!password.activeSelf)
        {
            password.SetActive(true);
        }

        if (!isPublic)
        {
            privateBttn.GetComponent<Image>().color = Color.gray;
            return;
        }
        isPublic = false;

        privateBttn.GetComponent<Image>().color = Color.gray;
        publicBttn.GetComponent<Image>().color = Color.white;
    }

    public RoomSetting GetRoomSetting()
    {
        RoomSetting roomSetting = new RoomSetting();
        roomSetting = PacketManager.Instance.CreateRoomSetting(createRoomName.text, int.Parse(maximumCapacity.text), isPublic, createRoomPassword.text);
        return roomSetting;
    }

    public void FinishRoomsetting()
    {
        byte[] roomSetting = PacketManager.Instance.EncodeRoomSetting(GetRoomSetting());
        NetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_REQ_CREATE, roomSetting.ToArray()));
        SceneManager.LoadScene("Loading");
        return;
    }

    public void CreateRoom(RoomSetting roomSetting)
    {
        roomSettings.Add(roomSetting);
        ClientManager.Instance.MyClient.InRoomSetting = roomSetting;

        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            for(int i=0; i<roomSettings.Count; i++)
            {
                if(rooms.Count == 0)
                {
                    rooms.Add(roomGenerator.CreateRoom(roomSettings[i]));
                    roomSettings.RemoveAt(i);
                    return;
                }

                for(int j=0; j<rooms.Count; j++)
                {
                    if (roomSettings[i].roomID == rooms[j].GetComponent<RoomController>().getRoomID())
                        continue;
                    rooms.Add(roomGenerator.CreateRoom(roomSettings[i]));
                    roomSettings.RemoveAt(i);
                }
            }
        }
    }

    public void DestroyRoom(RoomSetting roomSetting)
    {
        for(int i=0; i<rooms.Count; i++) {
            if (rooms[i].GetComponent<RoomController>().getRoomID().SequenceEqual(roomSetting.roomID))
            {
                Destroy(rooms[i]);
                rooms.RemoveAt(i);
            }
        }
    }
    public void DestroyAllRooms()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Destroy(rooms[i]);
        }
    }

    public void EnterRoomBttnClick()
    {
        ClientManager.Instance.MyClient.InRoomSetting = selectedRoom.GetComponent<RoomController>().GetRoomSetting();
        List<byte> packetDetail = new List<byte>();
        packetDetail.AddRange(ClientManager.Instance.MyClient.InRoomSetting.roomID);
        if(Convert.ToInt32(ClientManager.Instance.MyClient.InRoomSetting.isPublic) == 0)
        {
            packetDetail.AddRange(Encoding.UTF8.GetBytes(enterRoomPassword.text));
        }
        NetworkService.Instance.SendPacket(PacketManager.Instance.CreatePacket(PacketType.C_REQ_ENTER, packetDetail.ToArray()));
    }

    public void PutWrongPassword()
    {
        enterRoomPassword.text = "";
        enterRoomPage.SetActive(false);
        mainPage.SetActive(true);
        SceneManager.LoadScene(1);
    }

    public void EnterRoom()
    {
        SceneManager.LoadScene(2);
    }

    private void Awake()
    {
        roomSettings = new List<RoomSetting>();
        rooms = new List<GameObject>();
    }
}