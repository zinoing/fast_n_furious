using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    private GameObject player;
    private GameObject constraint;
    private GameObject focus;
    [SerializeField] private CarController controller; 
    public float speed;

    private void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                constraint = player.transform.Find("camera constraint").gameObject;
                focus = player.transform.Find("camera focus").gameObject;
                controller = player.GetComponent<CarController>();
                return;
            }
        }
    }

    void FixedUpdate()
    {
        if(player != null)
            follow();
    }
    
    private void follow()
    {
        speed = Mathf.Lerp(speed, controller.KPH / 4, Time.deltaTime);

        gameObject.transform.position = Vector3.Lerp(transform.position, constraint.transform.position, Time.deltaTime * speed);
        gameObject.transform.LookAt(focus.transform.position);
    }
}
