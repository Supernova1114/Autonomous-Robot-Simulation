using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

public class RobotMovement : MonoBehaviour
{
    [SerializeField] private LidarModule2D module;
    [SerializeField] private Transform targetWaypoint;
    [SerializeField] private float safeDistanceThresh;
    [SerializeField] private int safeLaserCountThresh;
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float rotationFixFactor;

    float[,] rangeList;

    void Start()
    {
    }
       

    void Update()
    {
        rangeList = module.GetRanges();

        Vector3 avoidanceVector = Vector3.zero;

        for (int i = 0; i < rangeList.GetLength(1); i++)
        {
            float range = rangeList[1, i];
            float angle = (rangeList[0, i] + 270) % 360;

            if ((angle >= 270 && angle < 360) || (angle >= 180 && angle < 270))
            {
                if (range != Mathf.Infinity && range < safeDistanceThresh)
                {
                    float radianAngle = angle * Mathf.Deg2Rad;
                    Vector3 direction = Quaternion.LookRotation(transform.forward, transform.up) * new Vector3(-Mathf.Cos(radianAngle), 0, -Mathf.Sin(radianAngle));
                    Vector3 laserDirection =Vector3.Reflect(direction, transform.right);
                    
                    //Debug.DrawRay(transform.position + Vector3.up, laserDirection.normalized * range, Color.green, 0.1f);

                    avoidanceVector += laserDirection; // need div by 0 check?
                }
            }


            /* // Get total dist and valid range count for front facing half.
             if (range != Mathf.Infinity && range < safeDistanceThresh)
             {
                 if ((angle >= 270 && angle < 360) || (angle >= 180 && angle < 270))
                 {
                     float radianAngle = angle * Mathf.Deg2Rad;
                     Vector3 direction = new Vector3(Mathf.Cos(radianAngle), 0, Mathf.Sin(radianAngle));
                     avoidanceVector += direction; // need div by 0 check?
                 }
             }*/
        } // for

        avoidanceVector *= -1;

        //Vector3 reflectedVec = Vector3.Reflect(avoidanceVector, transform.forward);

        Debug.DrawRay(transform.position, avoidanceVector.normalized, Color.blue, 0.1f);



        Vector3 towardsWaypoint = (targetWaypoint.position - transform.position);
        Vector3 towardsWaypointXZ = new Vector3(towardsWaypoint.x, 0, towardsWaypoint.z);

        Quaternion targetRotation;

        if (avoidanceVector == Vector3.zero)
        {
            targetRotation = Quaternion.LookRotation(towardsWaypointXZ, transform.up);
        }
        else
        {
            targetRotation = Quaternion.LookRotation(avoidanceVector, transform.up);
        }


        Vector3 targetVelocity = transform.forward * moveSpeed;

        if (towardsWaypointXZ.magnitude > 1)
        {
            transform.position += targetVelocity * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        //float targetRotationDelta = Mathf.Abs(targetRotation.eulerAngles.y - transform.eulerAngles.y);
        //float distToSubpoint = (subpointPos - robotPosXZ).magnitude;

        /*if (distToSubpoint < 0.1f) // Made it to subpoint
        {
            recalculateSubpoint = true;
        }
        else if (angleDelta < 5)
        {
            robotTransform.position += targetVelocity * robotSpeed * Time.deltaTime;
        }*/


    }
}
