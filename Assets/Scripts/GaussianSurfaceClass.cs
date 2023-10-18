using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class GaussianSurfaceClass
{
    public float[] WList { get; set; }

    public GaussianSurfaceClass(int wNumber)
    {
        WList = new float[wNumber];
    }

    public float evaluateGaussian(float x, float y, float sigma, float[] center)
    {
        double gaussX = Math.Pow(x - center[0], 2) / (2 * Math.Pow(sigma, 2));
        double gaussY = Math.Pow(y - center[1], 2) / (2 * Math.Pow(sigma, 2));

        return (float)Math.Exp(-(gaussX + gaussY));
    }

    public float calculateH(float x, float y, float sigma, float[] center)
    {
        float h = 0;
        foreach(float w in WList)
        {
            h += w * evaluateGaussian(x, y, sigma, center);
        }
        return h;
    }
}
