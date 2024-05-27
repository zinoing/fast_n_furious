using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public CarGenerator carGenerator;
    public List<DetailCarInfo> carStates;
    public DetailCarInfo myCarInfo;
    public GameObject endLine;

    private bool readyToUpdate;
    int diffSecond;

    private int numOfCarsCreated;
    public int NumOfCarsCreated
    {
        get { return numOfCarsCreated; }
        set { numOfCarsCreated = value; }
    }

    private int numOfPlayersReadyToPlay;
    public int NumOfPlayersReadyToPlay
    {
        get { return numOfPlayersReadyToPlay; }
        set { numOfPlayersReadyToPlay = value; }
    }

    public void CreateMyCar(DetailCarInfo carInfo)
    {
        carInfo.car = carGenerator.CreateCar(carInfo.carState);
        carInfo.targetPos = carInfo.car.transform.position;
        carInfo.targetRotation = carInfo.car.transform.rotation;

        myCarInfo = carInfo;

        carStates.Add(carInfo);
        ++numOfCarsCreated;
    }

    public void CreateOtherCar(DetailCarInfo carInfo)
    {
        carInfo.car = carGenerator.CreateOtherCar(carInfo.carState);
        carInfo.targetPos = carInfo.car.transform.position;
        carInfo.targetRotation = carInfo.car.transform.rotation;

        carStates.Add(carInfo);
        ++numOfCarsCreated;
    }

    private bool CheckErrorRange(Vector3 currentPosition, Vector3 updatePosition, Quaternion currentRotation, Quaternion updateRotation, float positionErrorThreshold, float rotationErrorThreshold)
    {
        float positionDifference = Vector3.Distance(currentPosition, updatePosition);
        if (positionDifference > positionErrorThreshold)
        {
            return false;
        }

        float rotationDifference = Quaternion.Angle(currentRotation, updateRotation);
        if (rotationDifference > rotationErrorThreshold)
        {
            return false;
        }
        return true;
    }

    public void UpdateCar(DetailCarInfo carInfo)
    {
        readyToUpdate = true;

        for(int i=0; i< numOfCarsCreated; i++)
        {
            if(carStates[i].clientID == carInfo.clientID)
            {
                
                if (CheckErrorRange(carStates[i].car.transform.position, carInfo.carState.position, carStates[i].car.transform.rotation, carInfo.carState.rotation, carInfo.carState.currentSpeed, 30.0f) == false)
                {
                    carStates[i].car.transform.position = carInfo.carState.position;
                    carStates[i].car.transform.rotation = carInfo.carState.rotation;
                }
                
                // 송신한 Peer 시점과 현재 시간 차를 계산합니다. 
                long tickDiff = TimeManager.Instance.CheckDiffTickFromGameTime(new DateTime(carInfo.gameTick));
                diffSecond = (new DateTime(tickDiff)).Second;

                // 속도를 이용하여 시간 차 동안 움직인 위치를 계산합니다.
                bool isReversing = carInfo.carState.isReversing;
                float distanceMoved = carInfo.carState.currentSpeed * ((float)diffSecond);
                Vector3 currentVector = isReversing ? -carStates[i].car.transform.forward : carStates[i].car.transform.forward;
                Vector3 movement = currentVector.normalized * distanceMoved;

                DetailCarInfo updatedCarInfo = carStates[i];

                // 1초 뒤의 위치와 회전 값을 목표값으로 설정하였습니다.
                distanceMoved = carInfo.carState.currentSpeed * (1.0f + (float)diffSecond);
                movement = currentVector.normalized * distanceMoved;
                updatedCarInfo.targetPos = carStates[i].car.transform.position + movement;

                // 각속도를 이용하여 시간 차 동안의 회전을 계산합니다.
                float rotationAngle = carInfo.carState.currentAngularVelocity.magnitude * (1.0f + (float)diffSecond);
                Vector3 rotationAxis = carInfo.carState.currentAngularVelocity.normalized;
                Quaternion rotationQuaternion = Quaternion.AngleAxis(rotationAngle, rotationAxis);
                updatedCarInfo.targetRotation = rotationQuaternion * carInfo.carState.rotation;
                carStates[i] = updatedCarInfo;

                return;
            }
        }
    }
    public void ReadyToUpdate(int clientID)
    {
        for (int i = 0; i < carStates.Count; i++)
        {
            // 자신의 차량을 찾았을 경우 update 준비가 되었음을 알립니다.
            if (carStates[i].clientID == clientID)
            {
                carStates[i].car.GetComponent<CarController>().readyToUpdate = true;
            }
        }
    }

    public void RemoveEndLine()
    {
        if (endLine == null)
            return;
        Destroy(endLine);
    }
    private void Awake()
    {
        carStates = new List<DetailCarInfo>();
        numOfCarsCreated = 0;
        numOfPlayersReadyToPlay = 0;
        readyToUpdate = false;
    }

    private void Update()
    {
        if (readyToUpdate)
        {
            for (int i = 0; i < numOfCarsCreated; i++)
            {
                if (carStates[i].clientID == myCarInfo.clientID)
                {
                    continue;
                }

                Vector3 newPosition = Vector3.Lerp(carStates[i].car.transform.position, carStates[i].targetPos, Time.deltaTime);
                Quaternion newRotation = Quaternion.Lerp(carStates[i].car.transform.rotation, carStates[i].targetRotation, Time.deltaTime);

                carStates[i].car.transform.position = newPosition;
                carStates[i].car.transform.rotation = newRotation;
            }
        }
    }
}
