using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMouseTracking : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Input.mousePosition.z);
        if (Input.GetMouseButtonDown(0))
            Debug.Log(transform.position);
    }
}
