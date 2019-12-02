using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdMovement : MonoBehaviour
{

    public Vector3 targetDirection = Vector3.left;
    

    
    void Start()
    {
        this.transform.localRotation = Quaternion.LookRotation(targetDirection, this.transform.up);
        // Register itself...
        BirdLogic.RegisterBird(this);
    }
}
