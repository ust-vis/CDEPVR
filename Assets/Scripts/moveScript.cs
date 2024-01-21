using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moveScript : MonoBehaviour
{
    public Transform sphere;
    // Update is called once per frame
    void Update()
    {
        sphere.position = transform.position;
    }
}
