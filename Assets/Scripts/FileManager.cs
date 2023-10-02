using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.VisualScripting;

public class FileManager
{
    public string Path { set; get; }
    public string Filename { set; get; }
    public string Extension { set; get; }

    public FileManager(string fileName, string extension)
    {
        Filename = fileName;
        Extension = extension;
        Path = Application.dataPath + fileName + extension;
    }

    public void writeQValuesCSV(float[,] qValues) 
    {
        using (StreamWriter writer = new StreamWriter(Path))
        {
            for (int i = 0; i < qValues.GetLength(0); i++)
            {
                for (int j = 0; j < qValues.GetLength(1); j++)
                {
                    writer.Write(qValues[i, j]);

                    if (j < qValues.GetLength(1) - 1)
                    {
                        writer.Write(",");
                    }
                }
                writer.WriteLine();
            }
        }
    }

    public float[,] readQValuesCSV()
    {
        float[,] qValues = null;

        if (File.Exists(Path)) 
        {
            using (StreamReader reader = new StreamReader(Path))
            {
                string[] lines = File.ReadAllLines(Path);

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split(',');
                    
                    if (qValues == null)
                        qValues = new float[lines.Length, values.Length];

                    for (int j = 0; j < values.Length; j++)
                    {
                        if (float.TryParse(values[j], out float floatValue))
                        {
                            qValues[i, j] = floatValue;
                        }
                        else
                        {
                            Debug.LogWarning($"No se pudo convertir el valor a flotante: {values[j]}");
                        }
                    }
                }
            }
        }
        
        return qValues;
    }
 }
