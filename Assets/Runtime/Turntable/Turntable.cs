using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turntable : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 1.0f;
    [SerializeField] Space space = Space.World;

    [SerializeField] TurntableAxis axis = TurntableAxis.Y;

    [Range(1, -1)][SerializeField] int direction = 1;

    enum TurntableAxis
    {
        X,
        Y,
        Z
    }

    // Update is called once per frame
    void Update()
    {
        //this.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, space);
        switch (axis)
        {
            case TurntableAxis.X:
                this.transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime * direction, space);
                break;
            case TurntableAxis.Y:
                this.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime * direction, space);
                break;
            case TurntableAxis.Z:
                this.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime * direction, space);
                break;
        }
    }
}
