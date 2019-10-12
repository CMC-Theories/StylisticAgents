using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    public BirdMovement bird;
    public int numBirds = 10;
    // Start is called before the first frame update
    void Start()
    {
        for(int i =0; i < numBirds; i++)
        {
            GameObject GO = Instantiate(bird.gameObject, this.transform);
            GO.transform.localPosition = Random.onUnitSphere;
            BirdMovement birdMove = GO.GetComponent<BirdMovement>();
            birdMove.targetDirection = Random.onUnitSphere;
            birdMove.enabled = true;
            TrailRenderer trail = GO.GetComponentInChildren<TrailRenderer>();
            trail.startColor = Random.ColorHSV(0,1,1f,1,.1f,1f,1f,1f);
            trail.endColor = new Color(0, 0, 0, 0f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
