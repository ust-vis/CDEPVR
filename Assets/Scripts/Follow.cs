using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    public Transform objectToFollow;

    // Update is called once per frame
    void Update()
    {
        transform.position = objectToFollow.position;
    }
}
