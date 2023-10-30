using System.Collections;
using System.Collections.Generic;
using System;

/*
 * Class that represents a gaussian surface with its behaviors and the W values.
 */
public class GaussianSurfaceClass
{
    public float[] WList { get; set; }
    public int iterations { get; set; }
    public GaussianSurfaceClass(int wNumber, int iterations)
    {
        this.iterations = iterations;
        WList = new float[wNumber];

        for (int i = 0; i < WList.Length; i++) 
        {
            WList[i] = UnityEngine.Random.Range(-0.5f, 0.5f);
        }
    }

    // Evaluate a (x, y) point on the gaussian and returns the z value (equivalent to qValue).
    public float evaluateGaussian(float x, float y, float sigma, float[] center)
    {
        double gaussX = Math.Pow(x - center[0], 2) / (2 * Math.Pow(sigma, 2));
        double gaussY = Math.Pow(y - center[1], 2) / (2 * Math.Pow(sigma, 2));

        return (float)Math.Exp(-(gaussX + gaussY));
    }

    // Evaluates a point (x, y) in every gasussian on the surface and multiplies it by the corresponding W.
    // This is used on the surface traninning process.
    public float calculateH(float x, float y, float sigma, List<float[]> centers)
    {
        float h = 0;
        for (int i = 0; i < WList.Length; i++)
        {
            h += WList[i] * evaluateGaussian(x, y, sigma, centers[i]);
        }
        return h;
    }

    // Use a point (x, y) to train the gaussian surface and update the W values.
    public void trainGaussSurface(float x, float y, float alpha, float sigma, List<float[]> centers, float newQValue)
    {
        for (int i = 0; i < iterations; i++)
        {
            float h = calculateH(x, y, sigma, centers);
            for (int j = 0; j < WList.Length; j++)
            {
                WList[j] = WList[j] + alpha * ((newQValue - h) * evaluateGaussian(x, y, sigma, centers[j]));
            }
        }
    }
}
