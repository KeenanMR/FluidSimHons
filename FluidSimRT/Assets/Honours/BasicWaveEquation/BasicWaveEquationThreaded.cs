using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;
using UnityEngine;


public class BasicWaveEquationThreaded : MonoBehaviour
{
    int iterationNumber = 1;
    int maxIterations = 5000;
    int xGridSize = 100;
    int yGridSize = 100;
    public float pSquared = 0.25f;


    float[,] height = new float[102, 102];
    float[,] lastNHeight = new float[102, 102];

    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;



        for (int x = 0; x < xGridSize + 1; x++)
        {
            for (int y = 0; y < yGridSize + 1; y++)
            {
                height[x, y] = 0;
                lastNHeight[x, y] = 0;
            }
        }
    }
    // Update is called once per frame

    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int aX = 0;
        int aY = 0;

        
        height[50, 50] = 0.001f * Mathf.Sin(iterationNumber * 8 * Mathf.PI / (maxIterations - 1));
        height[50, 50] = height[50, 50] * 0.75f;

   
        

        for (int x = 1; x < xGridSize + 1; x++)
        {
            for (int y = 1; y < yGridSize + 1; y++)
            {
                ThreadPool.QueueUserWorkItem(UpdateHeight, new object[] { x, y });
            }
        }

        

        for (int x = 0; x < xGridSize - 1; x++)
        {
            for (int y = 0; y < yGridSize - 1; y++)
            {
                if (x == 50)
                    aX = 1;
                else
                    aX = Mathf.Abs(50 - x);

                if (y == 50)
                    aY = 1;
                else
                    aY = Mathf.Abs(50 - y);

                float scale = Mathf.Pow((aX + aY), 3);

                vertices[(x * xGridSize) + y].y = (5000 + scale) * height[x, y];

            }
        }

        mesh.vertices = vertices;
        iterationNumber++;
    }

    
    void UpdateHeight(object state)
    {
        object[] values = state as object[];

        int x = (int)values[0];
        int y = (int)values[1];

        height[x, y] = height[x, y] + (pSquared * (height[x + 1, y] + height[x - 1, y] + height[x, y + 1] + height[x, y - 1])) - lastNHeight[x, y];
        lastNHeight[x, y] = height[x, y];
    }
    
}
