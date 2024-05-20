using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using UnityEngine.UIElements;

public struct CarState
{
    public CarState(Vector3 position, Quaternion rotation, float currentSpeed, Vector3 currentAngularVelocity, bool isReversing = false)
    {
        this.position = new Vector3(position.x, position.y, position.z);
        this.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        this.currentSpeed = currentSpeed;
        this.currentAngularVelocity = currentAngularVelocity;
        this.isReversing = isReversing;
    }
    public CarState(CarState carState)
    {
        this.position = carState.position;
        this.rotation = carState.rotation;
        this.currentSpeed = carState.currentSpeed;
        this.currentAngularVelocity = carState.currentAngularVelocity;
        this.isReversing = carState.isReversing;
    }
    public Vector3 position;
    public Quaternion rotation;
    public float currentSpeed;
    public Vector3 currentAngularVelocity;
    public bool isReversing;
}

public struct DetailCarInfo
{
    public DetailCarInfo(int clientID, CarState currentCarState, long gameTick = 0)
    {
        this.clientID = clientID;
        this.carState = new CarState(currentCarState);
        car = null;
        this.gameTick = gameTick;
        targetPos = Vector3.zero;
        targetRotation = Quaternion.identity;
    }
    public int clientID;
    public CarState carState;
    public GameObject car;
    public long gameTick;
    public Vector3 targetPos;
    public Quaternion targetRotation;
}

public class CarController : MonoBehaviour
{
    internal enum driveType
    {
        frontWheelDrive,
        rearWheelDrive,
        allWheelDrive
    }
    [SerializeField] private driveType drive;
    [SerializeField] private CarUIManager carUIManager;
    [SerializeField] private GameObject endLine;

    [Header("Variables")]
    public float totalPower = 0.0f;
    public float KPH = 0.0f;
    public float wheelsRPM = 0.0f;
    public float smoothTime = 0.01f;
    public float steeringMax = 20f;
    public float engineRPM = 0.0F;
    public float[] gears;
    public int gearNum = 0;
    public AnimationCurve enginePower;

    private InputManager IM;
    [SerializeField]private WheelCollider[] wheelColliders = new WheelCollider[4];
    [SerializeField] private GameObject[] wheelMesh = new GameObject[4];
    private GameObject centerOfMass;
    private Rigidbody rigidbody;

    private float radius = 6;
    private float downForceValue = 50f;
    private float brakePower = 300000f;
    private float thrust = 10000f;

    [Header("Debug")]
    public float[] forwardSlips = new float[4];
    public float[] sidewaysSlips = new float[4];
    private bool isReversing;

    [Header("SkidMark")]
    public GameObject skidMarkPrefab;

    [Header("SmokeParticle")]
    public ParticleSystem tireBurnEffect;

    [Header("EngineSound")]
    public AudioClip engineSound;
    private AudioSource audio;
    private bool engineSoundOn;

    public bool readyToUpdate = false;
    private CarState previousCarState;
    public int targetFrame;
    private int currentFrame;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        audio = GetComponent<AudioSource>();
        audio.clip = engineSound;

    }

    void Start()
    {
        carUIManager = GameObject.Find("CarUIManager").GetComponent<CarUIManager>();
        endLine = GameObject.Find("EndLine");
        IM = GetComponent<InputManager>();
        rigidbody = GetComponent<Rigidbody>();
        centerOfMass = GameObject.Find("Mass");

        rigidbody.centerOfMass = centerOfMass.transform.localPosition;
        previousCarState = new CarState(gameObject.transform.position, gameObject.transform.rotation, rigidbody.velocity.magnitude, rigidbody.angularVelocity, false);

        targetFrame = 12;
        currentFrame = 0;

        engineSoundOn = false;

        isReversing = false;
        tireBurnEffect.Stop();
    }

    private void FixedUpdate()
    {
        ++currentFrame;
        CalculateEnginePower();
        AddDownForce();
        AnimateWheels();
        MoveVehicle();
        SteerVehicle();
        GetFriction();
        Shifter();

        carUIManager.vehicleSpeed = KPH;
        carUIManager.UpdateSpeedoNeedle();
        carUIManager.UpdateRPMNeedle(engineRPM);

        if(readyToUpdate)
        {
            // targetFrame마다 아래 동작들이 이루어집니다.
            if (currentFrame % targetFrame == 0)
            {
                currentFrame = 0;
                isReversing = wheelsRPM >= 0 ? false : true;
                CarState currentCarState = new CarState(gameObject.transform.position,
                                                        Quaternion.Euler(gameObject.transform.rotation.eulerAngles.x, gameObject.transform.rotation.eulerAngles.y, gameObject.transform.rotation.eulerAngles.z), 
                                                        rigidbody.velocity.magnitude,
                                                        rigidbody.angularVelocity,
                                                        isReversing);

                // 불필요한 전송을 방지하기 위해 자신의 차량이 이전 위치와 다를 경우에만 전송이 이루어지도록 하였습니다.
                if (previousCarState.position != currentCarState.position)
                {
                    DetailCarInfo currentCarInfo = new DetailCarInfo(ClientManager.Instance.MyClient.ClientID, currentCarState);

                    P2PNetworkService.Instance.SelectiveMulticast(PacketManager.Instance.CreatePacketWithSequenceNum(PacketType.P_UPDATE_CAR_STATE,
                                                    PacketManager.Instance.EncodeDetailCarState(currentCarInfo)),
                                                    ClientManager.Instance.MyClient.ClientUDPEndPointPair);

                    previousCarState = currentCarState;
                }
            }
        }
    }

    private void AddDownForce()
    {
        rigidbody.AddForce(-transform.up * downForceValue * rigidbody.velocity.magnitude);
    }

    private void MoveVehicle()
    {
        switch(drive)
        {
            case driveType.allWheelDrive:
                for (int i = 0; i < wheelColliders.Length; i++)
                {
                    wheelColliders[i].motorTorque = Math.Abs(IM.vertical) * (totalPower / 4);
                }
                break;
            case driveType.frontWheelDrive:
                for (int i = 0; i < wheelColliders.Length - 2; i++)
                {
                    wheelColliders[i].motorTorque = Math.Abs(IM.vertical) * (totalPower / 2);
                }
                break;
            case driveType.rearWheelDrive:
                for (int i = 2; i < wheelColliders.Length; i++)
                {
                    wheelColliders[i].motorTorque = Math.Abs(IM.vertical) * (totalPower / 2);
                }
                break;
        }

        KPH = rigidbody.velocity.magnitude * 3.6f;

        if (IM.handBrake)
        {
            // 앞 바퀴에 제동이 걸립니다.
            wheelColliders[0].brakeTorque = brakePower * 0.7f;
            wheelColliders[1].brakeTorque = brakePower * 0.7f;
            wheelColliders[2].brakeTorque = brakePower * 0.3f;
            wheelColliders[3].brakeTorque = brakePower * 0.3f;

            WheelFrictionCurve curve = new WheelFrictionCurve();
            curve = wheelColliders[2].sidewaysFriction;
            curve.stiffness = 0.5f;

            wheelColliders[2].sidewaysFriction = curve;
            wheelColliders[3].sidewaysFriction = curve;
        }
        else
        {
            foreach(WheelCollider wheelCollider in wheelColliders)
            {
                wheelCollider.brakeTorque = 0;
            }
            WheelFrictionCurve curve = new WheelFrictionCurve();
            curve = wheelColliders[2].sidewaysFriction;
            curve.stiffness = 1.0f;

            wheelColliders[2].sidewaysFriction = curve;
            wheelColliders[3].sidewaysFriction = curve;
        }

        if (IM.boosting)
        {
            rigidbody.AddForce(gameObject.transform.forward * thrust);
            if (!engineSoundOn)
            {
                engineSoundOn = true;
                audio.Play();
            }
        }
        else
        {
            if (engineSoundOn)
            {
                engineSoundOn = false;
                audio.Stop();
            }
        }

        if (IM.handBrake && IM.boosting && KPH > 50)
        {
            SetSkidMark();
            tireBurnEffect.Play();
            targetFrame = 3;
        }
        else
        {
            targetFrame = 10;
        }
    }
    
    private void SteerVehicle()
    {
        if (IM.horizontal > 0)
        {
            wheelColliders[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * IM.horizontal;
            wheelColliders[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * IM.horizontal;
        }
        else if (IM.horizontal < 0)
        {
            wheelColliders[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * IM.horizontal;
            wheelColliders[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * IM.horizontal;
        }

        for (int i = 0; i < wheelColliders.Length - 2; i++)
        {
            wheelColliders[i].steerAngle = IM.horizontal * steeringMax;
        }
    }
    
    private void AnimateWheels()
    {
        Vector3 wheelPosition;
        Quaternion wheelRotation;

        for (int i = 0; i < wheelColliders.Length - 2; i++)
        {
            wheelColliders[i].GetWorldPose(out wheelPosition, out wheelRotation);
            wheelMesh[i].transform.position = wheelPosition;
            wheelMesh[i].transform.rotation = wheelRotation;
        }
    }

    private void GetFriction()
    {
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelHit wheelHit;
            wheelColliders[i].GetGroundHit(out wheelHit);

            forwardSlips[i] = wheelHit.forwardSlip;
            sidewaysSlips[i] = wheelHit.sidewaysSlip;
        }
    }
    
    private void CalculateEnginePower()
    {
        // wheel RPM 을 engine RPM으로 변환
        WheelRPM();
        totalPower = enginePower.Evaluate(engineRPM) * (gears[gearNum]) * IM.vertical;
        float velocity = 0.0f;
        engineRPM = Mathf.SmoothDamp(engineRPM, 1000 + (Mathf.Abs(wheelsRPM) * 3.6f * (gears[gearNum])), ref velocity, smoothTime);

    }
    
    private void WheelRPM()
    {
        float sum = 0;
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            sum += wheelColliders[i].rpm;
        }
        // 각각의 wheelCollider RPM의 평균치
        wheelsRPM = (wheelColliders.Length != 0) ? sum / wheelColliders.Length : 0;
    }

    private void Shifter()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            if (gearNum >= 4)
                return;
            ++gearNum;
            carUIManager.ChangeGear(gearNum.ToString());
        }
        else if(Input.GetKeyDown(KeyCode.Q))
        {
            if (gearNum == 0)
                return;
            --gearNum;
            carUIManager.ChangeGear(gearNum.ToString());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.name == "Line")
        {
            Debug.Log("Entered finish line");
            Destroy(endLine);
            P2PNetworkService.Instance.SendPacketToSuperPeer(PacketManager.Instance.CreatePacket(PacketType.C_REQ_WIN));
        }
    }
    private Vector3 leftWheelPreviousPos = Vector3.zero;
    private Vector3 rightWheelPreviousPos = Vector3.zero;
    private void SetSkidMark()
    {
        WheelHit hit1;
        WheelHit hit2;

        wheelColliders[2].GetGroundHit(out hit1);
        wheelColliders[3].GetGroundHit(out hit2);

        if(leftWheelPreviousPos == Vector3.zero && rightWheelPreviousPos == Vector3.zero)
        {
            leftWheelPreviousPos = hit1.point;
            rightWheelPreviousPos = hit2.point;
            return;
        }
        Vector3 leftWheelRelativePos = leftWheelPreviousPos - hit1.point;
        Vector3 rightWheelRelativePos = rightWheelPreviousPos - hit2.point;

        Quaternion rot = Quaternion.LookRotation((leftWheelRelativePos + rightWheelRelativePos) /2);
        Instantiate(skidMarkPrefab, (hit1.point + hit2.point)/2, rot);
    }
}
