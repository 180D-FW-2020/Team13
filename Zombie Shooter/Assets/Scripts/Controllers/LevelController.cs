using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    public int levelNumber;
    public Transform playerPads;
    public Transform waypoints;


    public List<Transform> GetWaypoints()
    {
        return waypoints.Cast<Transform>().ToList();
    }

    public List<Transform> GetPlayerPads()
    {
        return playerPads.Cast<Transform>().ToList();
    }
}
