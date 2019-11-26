using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicialGen : MonoBehaviour
{

    /*
     * After further digging:
     *      No one can _not_ access information directly from the 
     *      buffers inside of the function that generates audio (since
     *      it is not the main thread.)
     *      
     *      The only workaround is to possibly have an array that is
     *      modified on the main thread, and read in the function.
     * 
     *      This will clearly lead to some desyncs, so as a work around
     *      we can turn to fourier. The idea is that we will only allow
     *      certain frequencies to be represented.
     *      
     *      The compute shader will figure out the volume of the birds
     *      and perform the fourier transform (tenatively).
     *      
     *      This amplitude data will then be fed back into the audio generator.
     *      It will need to do 2 things:
     *          Prevent clipping/clicking, this means we simply need
     *              to have a counter in addition to position, pos dictates
     *              where to write data out, counter is effectively time.
     *              In addition, we will need to phase sync the waves, and
     *              deal with waves decreasing in amplitude, this could be
     *              best done with an quadratic/sloping envelope. The idea is 
     *              that over the range of the data, we have the envelope take
     *              action and dampen/increase the volume of the wave.
     *          Optimize the Sine function. Since we are generating audio data,
     *              we need to perform upwards of (size of data to generate * 
     *              number of channels), this will be terrible in the long run
     *              as it effectively means that it will run almost n^2 time, on
     *              top of having to evaluate sine. By precomputing sine for a 
     *              lot of points, we can then simply interpolate the points inbetween.
     *              Also we don't need pristine sine curves, we can get away with approximated curves.
     *              
     *              
     *          
     */




    public static int res = 100;
    public float[] PreSine = new float[res];

    public int position = 0;
    public int samplerate = 2000; // low but we don't need higher...
    public float frequency = 440;
    public Transform leftMostPlane, rightMostPlane, topMostPlane, bottomMostPlane, farPlane, closePlane;
    public float minFreq = 50;
    public float maxFreq = 250;

    // The idea is to add up additional frequencies
    Vector3 LMP, RMP, TMP, BMP, FP, CP;
    void Start()
    {
        for(int i =0; i < res; i++)
        {
            PreSine[i] = Mathf.Sin(((float)i) * 2 * Mathf.PI / ((float)res));
        }


        LMP = leftMostPlane.position;
        RMP = rightMostPlane.position;
        TMP = topMostPlane.position;
        BMP = bottomMostPlane.position;
        FP = farPlane.position;
        CP = closePlane.position;
        Debug.Log("" + LMP + " " + RMP + " " + TMP + " " + BMP + " " + FP + " " + CP);
        AudioClip myClip = AudioClip.Create("MS", samplerate /4, 2, samplerate, true, OnAudioRead, OnAudioSet);
        AudioSource aud = GetComponent<AudioSource>();
        aud.clip = myClip;
        aud.loop = true;
        aud.Play();
    }
    public void OnAudioRead(float[] data) {
        int count = 0;
        int copys = BirdLogic.otherBirds.Count;
        int op = position;
        for(int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        foreach (BirdMovement bm in BirdLogic.otherBirds)
        {
            float targFreq = (BirdLogic.currentBirdsLoc[bm].x - LMP.x) / (RMP.x - LMP.x)*(maxFreq - minFreq) + minFreq;
            count = 0;
            Debug.Log(targFreq);
            position = op;
            while (count < data.Length)
            {
                data[count] += Mathf.Sin(position *
                    targFreq * Mathf.PI /samplerate)/((float)copys);
                count++;
                position++;
            }
        }
    }
    public void OnAudioSet(int np) { position = np; }
}
