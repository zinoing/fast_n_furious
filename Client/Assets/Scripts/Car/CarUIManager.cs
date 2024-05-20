using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarUIManager : MonoBehaviour
{
    public GameObject speedoNeedle;
    public GameObject tachnoNeedle;
    public Text gearNum;
    private float speedoStartPos, speedoEndPos;
    private float tachnoStartPos, tachnoEndPos;
    private float desiredPos;

    public float vehicleSpeed;
    void Start()
    {
        speedoStartPos = 220f;
        speedoEndPos = -49f;
        tachnoStartPos = 215f;
        tachnoEndPos = -35f;
        vehicleSpeed = 0.0f;
    }

    public void UpdateSpeedoNeedle()
    {
        desiredPos = speedoStartPos - speedoEndPos;
        float temp = vehicleSpeed / 180;
        if(vehicleSpeed > 180f)
        {
            speedoNeedle.transform.eulerAngles = new Vector3(0, 0, speedoEndPos);
        }
        else
        {
            speedoNeedle.transform.eulerAngles = new Vector3(0, 0, speedoStartPos - temp * desiredPos);
        }
    }

    public void UpdateRPMNeedle(float engineRPM)
    {
        desiredPos = tachnoStartPos - tachnoEndPos;
        float temp = engineRPM / 10000;
        if (engineRPM > 10000f)
        {
            tachnoNeedle.transform.eulerAngles = new Vector3(0, 0, tachnoEndPos);
        }
        else
        {
            tachnoNeedle.transform.eulerAngles = new Vector3(0, 0, tachnoStartPos - temp * desiredPos);
        }
    }

    public void ChangeGear(string gearNumArg)
    {
        gearNum.text = gearNumArg;
    }
}
