using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turntable : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 1.0f;
    [SerializeField] Space space = Space.World;

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, space);
    }
}
