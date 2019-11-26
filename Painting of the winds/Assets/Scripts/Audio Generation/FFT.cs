using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FFT : MonoBehaviour
{
    // Start is called before the first frame update
    static int N = 32;
    float[] twidles = new float[N * 2];
    public float scale = 30f;
    void Start()
    {
        AC = new AnimationCurve();
        PC = new AnimationCurve();
        for(int i = 0; i < N; i++)
        {
            twidles[2 * i] = Mathf.Cos(2 * Mathf.PI*scale*i / ((float)N));
            twidles[2 * i + 1] = -Mathf.Sin(2 * Mathf.PI*scale*i / ((float)N));
            points[i] = Instantiate(point);
            PC.AddKey(((float)i) / ((float)N), 0.5f);
            AC.AddKey(((float)i) / ((float)N), 0.5f);
        }
        
    }
    [Range(1f,10000f)]
    public float genfreq = 50f;
    public GameObject point;
    public float cutoff = -5f;
    float pastfreq = -1f;
    GameObject[] points = new GameObject[N];
    // Update is called once per frame
    public AnimationCurve AC;
    public AnimationCurve PC;
    void Update()
    {
        if (!Mathf.Approximately(pastfreq, genfreq))
        {
            float[] ppoints = new float[N];
            for (int i = 0; i < N; i++)
            {
                ppoints[i] = Mathf.Sin(2 * Mathf.PI * genfreq * ((float)i) / ((float)N)); // Freq = PI
                AC.MoveKey(i, new Keyframe(((float)i) / ((float)N), ppoints[i]));
            }
            float[] fft = DFT(ppoints, N, 0, 1, twidles);


            float[] pow = new float[N];
            float[] nsp = new float[N];
            for (int i = 0; i < N; i++)
            {
                pow[i] = Mathf.Sqrt(fft[2 * i] * fft[2 * i] + fft[2 * i + 1] * fft[2 * i + 1])/N;
                points[i].transform.position = new Vector3(-3f + 6f*((float)i)/((float)N), 0.2f*Mathf.Max(-5f,Mathf.Log10(pow[i]+0.0001f)), 0);
                for(int j = 0; j < N; j++)
                {
                    // Fails here :(
                    nsp[j] += pow[i] * Mathf.Cos((-2f * Mathf.PI * scale * i * j / ((float)N * N)) - Mathf.Max(pow[i]*pow[i]*pow[i], 0.001f)*Mathf.Atan2(fft[2*i+1], fft[2*i]));
                }
            }
            pastfreq = genfreq;
            for(int i = 0; i < N; i++)
            {
                PC.MoveKey(i, new Keyframe(((float)i) / ((float)N), nsp[i]));
            }
        }
        
    }



    /*
     * FFT requires us to do
     * x[k] = sum(x[n]e^(-j*2*pi*k*n/N))
     * 
     * W_N = e^(-j*2*pi/N) [CONST]
     * 
     * Compute it in terms of complex numbers, since adding is easy, mult is also easy
     * (a+bi)(c+di) = (ac-db)+ (bc+ad)i
     *
     * 
     * The idea is to take that sum, and break it up
     * x[k] = sum_e(x[n]W_N^(k*n)) + W_N * sum_o(x[n]W_N^(k*n))
     */
    void SpecialPrint()
    {

    }
    float[] DFT(float[] xx, int N,int p, int skip, float[] twiddle)
    {
        Debug.Log("DFT:" + String.Join(",",
             new List<int>(new int[] { N, p, skip })
             .ConvertAll(i => i.ToString())
             .ToArray()));
        if (N == 1)
        {
            return new float[] {xx[p],0}; // It is entirely real right now.
        }
        float[] fh = DFT(xx, N / 2, p, skip * 2, twiddle); // NEED TO REDO XX!!!!!!!
        float[] sh = DFT(xx, N / 2, p + skip, skip * 2, twiddle); // ERROR HERE!!!
        float[] full = new float[2*N];
        Debug.Log("DFT1:" + String.Join(",",
             new List<int>(new int[] { N, p, skip })
             .ConvertAll(i => i.ToString())
             .ToArray()));
        Debug.Log(twiddle.Length + " " + sh.Length + " " + fh.Length + " " + N);
        for (int i = 0; i < (N/2); i++)
        {
            full[2 * i] = fh[2 * i] + (sh[2 * i] * twiddle[2 * i*skip] - sh[2 * i + 1] * twiddle[2 * i*skip + 1]);
            full[2 * i + 1] = fh[2 * i + 1] + (sh[2 * i + 1] * twiddle[2 * i*skip] + sh[2 * i] * twiddle[2 * i*skip + 1]);
            full[2 * (i + (N/2))] = fh[2 * i] - (sh[2 * i] * twiddle[2 * i*skip] - sh[2 * i + 1] * twiddle[2 * i*skip + 1]);
            full[2 * (i + (N/2)) + 1] = fh[2 * i + 1] - (sh[2 * i + 1] * twiddle[2 * i*skip] + sh[2 * i] * twiddle[2 * i*skip + 1]);
        }


        return full;
    }




}


