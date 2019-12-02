using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdMovement_Old : MonoBehaviour
{

    public Vector3 targetDirection = Vector3.left;
    

    public float speed = 0.5f;
    public float slerpAmount = 10f;
    // Start is called before the first frame update
    public float flockRange = .5f;
    void Start()
    {
        this.transform.localRotation = Quaternion.LookRotation(targetDirection, this.transform.up);
        BirdLogic_Old.RegisterBird(this);
    }

    // Update is called once per frame
    void Update()
    {
        targetDirection = BirdLogic_Old.GetNewMovement(this);
        Debug.DrawRay(this.transform.position, targetDirection);
        

        this.transform.localRotation = Quaternion.Slerp(this.transform.localRotation, Quaternion.LookRotation(targetDirection, this.transform.up), slerpAmount * Time.deltaTime);
        // Technically make them slow down if they are going to hit something....
        float dist = 1;
        while (true)
        {
            if (Physics.Raycast(this.transform.position, this.transform.forward, this.transform.forward.magnitude * speed * Time.smoothDeltaTime * dist*1.25f))
            {
                this.transform.localRotation = Quaternion.Slerp(this.transform.localRotation, Quaternion.LookRotation(targetDirection, this.transform.up), slerpAmount * Time.deltaTime);
                dist *= .85f;
            }
            else
            {
                break;
            }
        }
        this.transform.localPosition += this.transform.forward * speed * Time.smoothDeltaTime*dist;
        // Slowly lerp the rotation to the target direction
        
    }
}
