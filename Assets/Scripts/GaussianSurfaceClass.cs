using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Mathematics;

public class GaussianSurfaceClass
{
    public float[] WList { get; set; }

    public GaussianSurfaceClass(int wNumber)
    {
        WList = new float[wNumber];

        for (int i = 0; i < WList.Length; i++) 
        {
            WList[i] = UnityEngine.Random.Range(0.0f, 1.0f) - 0.5f;
        }
    }
    public float evaluateGaussian(float x, float y, float sigma, float[] center)
    {
        double gaussX = Math.Pow(x - center[0], 2) / (2 * Math.Pow(sigma, 2));
        double gaussY = Math.Pow(y - center[1], 2) / (2 * Math.Pow(sigma, 2));

        return (float)Math.Exp(-(gaussX + gaussY));
    }
    public float calculateH(float x, float y, float sigma, List<float[]> centers)
    {
        float h = 0;
        for (int i = 0; i < WList.Length; i++)
        {
            h += WList[i] * evaluateGaussian(x, y, sigma, centers[i]);
        }
        return h;
    }

    public void trainGaussSurface(float x, float y, float alpha, float sigma, List<float[]> centers, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            float h = calculateH(x, y, sigma, centers);
            for (int j = 0; j < WList.Length; j++)
            {
                WList[j] = WList[j] + alpha * ((0 - h) * evaluateGaussian(x, y, sigma, centers[j]));
            }
        }
    }
}
