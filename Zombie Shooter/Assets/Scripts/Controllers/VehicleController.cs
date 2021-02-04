using OpenCvSharp.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public float speed;
    public Transform vehicleCamera;
    public List<Transform> playerPads;
    private List<Transform> waypoints;

    private int currentWaypoint;
    private Rigidbody rigidBody;
    private bool stopped;

    public void Awake()
    {
        //rigidBody = GetComponent<Rigidbody>();
        stopped = true;
    }

    public void SetWaypoints(List<Transform> path)
    {
        stopped = false;
        waypoints = path;
        transform.position = path[0].position;
        currentWaypoint = 0;
    }

    public Transform GetVehicleCameraPosition()
    {
        return vehicleCamera;
    }

    public List<Transform> GetPlayerPads()
    {
        return playerPads;
    }

    public bool IsStopped()
    {
        return stopped;
    }

    public void Update()
    {
        const float turnSpeed = 1.0f;
        const float turnDist = 10.0f;

        if (!stopped)
        {
            var wp = waypoints[currentWaypoint];

            transform.position = Vector3.MoveTowards(transform.position, wp.position, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, wp.position) > turnDist)
                transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, wp.position - transform.position, turnSpeed/2 * Time.deltaTime, 0.0f));
            else
                transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, wp.forward, turnSpeed * Time.deltaTime, 0.0f));

            if (transform.position == wp.position)
            {
                currentWaypoint++;
                if (currentWaypoint == waypoints.Count) {
                    stopped = true;
                    Debug.Log(waypoints.Count);
                }
            }
        }
    }
}
