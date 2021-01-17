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
        if (!stopped)
        {
            transform.position = Vector3.MoveTowards(transform.position, waypoints[currentWaypoint].position, speed * Time.deltaTime);

            if (transform.position == waypoints[currentWaypoint].position)
            {
                currentWaypoint++;
                if (currentWaypoint == waypoints.Count)
                    stopped = true;
            }
        }
    }
}
