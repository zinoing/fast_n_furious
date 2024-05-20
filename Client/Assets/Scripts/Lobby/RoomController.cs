using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public struct RoomSetting
{
    public byte[] roomID; // maximum 4
    public byte[] roomName; // maximum 20
    public byte maximumCapacity;
    public byte isPublic;
    public byte[] roomPassword; // maximum 4
};

public class RoomController : MonoBehaviour
{
    private RoomSetting roomSetting;
    public TMP_Text roomName;

    public RoomSetting GetRoomSetting()
    {
        return roomSetting;
    }

    public byte[] getRoomID()
    {
        return roomSetting.roomID;
    }

    public void SetRoomSetting(RoomSetting roomSetting)
    {
        this.roomSetting = roomSetting;
        roomName.text = Encoding.UTF8.GetString(roomSetting.roomName);
    }

    public void SelectBttnClick()
    {
        if (LobbyManager.selectedRoom != null)
        {
            LobbyManager.selectedRoom.transform.Find("RoomBackground").GetComponent<RawImage>().color = Color.white;
        }
        gameObject.transform.Find("RoomBackground").GetComponent<RawImage>().color = Color.gray;
        LobbyManager.selectedRoom = gameObject;
    }

}
