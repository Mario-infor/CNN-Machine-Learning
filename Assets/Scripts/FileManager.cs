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

    public void writeQValuesCSV(TileState[,] gridPosMatrix) 
    {
        using (StreamWriter writer = new StreamWriter(Path))
        {
            for (int i = 0; i < gridPosMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gridPosMatrix.GetLength(1); j++)
                {
                    for (int k = 0; k < gridPosMatrix[i, j].qValues.Length; k++)
                    {
                        writer.Write(gridPosMatrix[i, j].qValues[k]);
                        
                        if (k < gridPosMatrix[i, j].qValues.GetLength(1) - 1)
                        {
                            writer.Write(",");
                        }
                    }
                    writer.WriteLine();
                }
            }
        }
    }

    public List<float[]> readQValuesCSV()
    {
        List<float[]> qValues = new List<float[]>();

        if (File.Exists(Path)) 
        {
            using (StreamReader reader = new StreamReader(Path))
            {
                string[] lines = File.ReadAllLines(Path);

                for (int i = 0; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split(',');

                    float[] value = new float[values.Length];

                    for (int j = 0; j < values.Length; j++)
                    {
                        if (float.TryParse(values[j], out float floatValue))
                        {
                            value[j] = floatValue;
                        }
                        else
                        {
                            Debug.LogWarning($"No se pudo convertir el valor a flotante: {values[j]}");
                        }
                    }
                    qValues.Add(value);
                }
            }
        }
        
        return qValues;
    }
 }
