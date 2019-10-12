using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdMovement : MonoBehaviour
{

    public Vector3 targetDirection = Vector3.left;
    

    public float speed = 0.5f;
    public float slerpAmount = 10f;
    // Start is called before the first frame update
    void Start()
    {
        this.transform.localRotation = Quaternion.LookRotation(targetDirection, this.transform.up);
    }

    // Update is called once per frame
    void Update()
    {
        targetDirection = BirdLogic.GetNewMovement(this);
        Debug.DrawRay(this.transform.position, targetDirection);
        this.transform.localPosition += targetDirection * speed * Time.smoothDeltaTime;
        // Slowly lerp the rotation to the target direction
        this.transform.localRotation = Quaternion.Slerp(this.transform.localRotation, Quaternion.LookRotation(targetDirection, this.transform.up), slerpAmount * Time.deltaTime);
    }
}
