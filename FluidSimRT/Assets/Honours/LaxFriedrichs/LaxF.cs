using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


using System.IO;

public class LaxF : MonoBehaviour
{
    int xGridSize;
    int yGridSize;

    //Equation variables
    float g = 9.8f;
    float dt = 0.01f;
    float dx = 0.2f;
    float dy = 0.2f;

    //Simplifiers
    float alpha;
    float beta;

    //Data arrays
    float[,] height;
    float[,] fV;
    float[,] fU;
    float[,] v;
    float[,] u;



    // Start is called before the first frame update
    void Start()
    {
        //Get mesh data from scene 
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //Set intial calculation values
        alpha = dt / (2 * dx);
        beta = dt / (2 * dy);

        //Set initial data values
        for (int x = 0; x < xGridSize; x++)
        {
            for (int y = 0; y < yGridSize; y++)
            {
                height[x, y] = 1;
                fU[x, y] = 0;
                fV[x, y] = 0;
                u[x, y] = 0;
                v[x, y] = 0;

            }
        }

        //Set the mesh from the height data
        for (int x = 0; x < xGridSize; x++)
        {
            for (int y = 0; y < yGridSize; y++)
            {

                vertices[(x * xGridSize) + y].y = height[x, y];

            }

        }

        mesh.vertices = vertices;
    }

    //Initialisation function called from grid script
    public void Init(int gridXY)
    {
        xGridSize = gridXY + 1;
        yGridSize = gridXY + 1;


        height = new float[gridXY + 1, gridXY + 1];
        fV = new float[gridXY + 1, gridXY + 1];
        fU = new float[gridXY + 1, gridXY + 1];
        v = new float[gridXY + 1, gridXY + 1];
        u = new float[gridXY + 1, gridXY + 1];
    }

    //Boundary conditions for wave reflection
    void Boundaries()
    {
        //Boundary Conditions
        for (int x = 0; x < xGridSize - 1; x++)
        {
            height[x, 1] = height[x, 2];
            u[x, 1] = u[x, 2];
            v[x, 1] = -v[x, 2];

            height[x, yGridSize - 2] = height[x, yGridSize - 3];
            u[x, yGridSize - 2] = u[x, yGridSize - 3];
            v[x, yGridSize - 2] = -v[x, yGridSize - 3];
        }

        for (int y = 0; y < yGridSize - 1; y++)
        {
            height[1, y] = height[2, y];
            u[1, y] = -u[2, y];
            v[1, y] = v[2, y];

            height[xGridSize - 2, y] = height[xGridSize - 3, y];
            u[xGridSize - 2, y] = -u[xGridSize - 3, y];
            v[xGridSize - 2, y] = v[xGridSize - 3, y];
        }



    }

    //Lax Friedrichs algorithm function
    void LaxFAlgorithm()
    {
        Profiler.BeginSample("LaxF");
        //Solving H
        for (int x = 1; x < xGridSize - 1; x++)
        {
            for (int y = 1; y < yGridSize - 1; y++)
            {
                float[,] oldHeight = height;

                height[x, y] = 0.25f * (oldHeight[x + 1, y] + oldHeight[x - 1, y] + oldHeight[x, y + 1] + oldHeight[x, y - 1]) - alpha * (fU[x + 1, y] - fU[x - 1, y]) - beta * (fV[x, y + 1] - fV[x, y - 1]);
            }
        }

        //Solving HU
        for (int x = 1; x < xGridSize - 1; x++)
        {
            for (int y = 1; y < yGridSize - 1; y++)
            {
                float[,] oldFU = fU;

                fU[x, y] = 0.25f * (oldFU[x + 1, y] + oldFU[x - 1, y] + oldFU[x, y + 1] + oldFU[x, y - 1]);
                fU[x, y] = fU[x, y] - alpha * ((height[x + 1, y] * (u[x + 1, y]* u[x + 1, y]) + (0.5f * g * (height[x + 1, y]* height[x + 1, y]))) - (height[x - 1, y] * ((u[x - 1, y]* u[x - 1, y])) + (0.5f * g * ((height[x - 1, y]* height[x - 1, y])))));
                fU[x, y] = fU[x, y] - beta * (height[x, y + 1] * v[x, y + 1] * u[x, y + 1] - height[x, y - 1] * v[x, y - 1] * u[x, y - 1]);
            }
        }
         

        //Solving HV
        for (int x = 1; x < xGridSize - 1; x++)
        {
            for (int y = 1; y < yGridSize - 1; y++)
            {
                float[,] oldFV = fV;

                fV[x, y] = 0.25f * (oldFV[x + 1, y] + oldFV[x - 1, y] + oldFV[x, y + 1] + oldFV[x, y - 1]);
                fV[x, y] = fV[x, y] - alpha * (height[x + 1, y] * v[x + 1, y] * u[x + 1, y] - height[x - 1, y] * v[x - 1, y] * u[x - 1, y]);
                fV[x, y] = fV[x, y] - alpha * ((height[x, y + 1] * (v[x, y + 1]* v[x, y + 1]) + (0.5f * g * (height[x, y + 1]*height[x, y + 1]))) - (height[x, y - 1] * (v[x, y - 1]* v[x, y - 1]) + (0.5f * g * (height[x, y - 1]* height[x, y - 1]))));

            }
        }

        //Solving U
        for (int x = 1; x < xGridSize - 1; x++)
        {
            for (int y = 1; y < yGridSize - 1; y++)
            {
                u[x, y] = fU[x, y] / height[x, y];
            }
        }

        //Solving V
        for (int x = 1; x < xGridSize - 1; x++)
        {
            for (int y = 1; y < yGridSize - 1; y++)
            {
                v[x, y] = fV[x, y] / height[x, y];
            }
        }

        Profiler.EndSample();
    }

    void UpdateMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        //Translate 2D array to 1D vector for height 
        for (int x = 0; x < xGridSize; x++)
        {
            for (int y = 0; y < yGridSize; y++)
            {
                vertices[x + (xGridSize * y)].y = 2 * height[x, y];

            }
        }
        mesh.vertices = vertices;

    }

    // Update is called once per frame
    void Update()
    {
        //Simulate water drop
        if (Input.GetKeyDown(KeyCode.Space))
        {

            int xStart = Random.Range(1, 40);
            int yStart = Random.Range(1, 40);
            for (int x = 0; x < 21; x++)
            {
                for (int y = 0; y < 21; y++)
                {
                    height[xStart + x, yStart + y] += 5f * Mathf.Exp(-5 * (Mathf.Pow(-1f + (0.1f * x), 2) + Mathf.Pow(-1f + (0.1f * y), 2)));
                }
            }
        }

        //Checks boundary conditions
        Boundaries();
        //Calls main algorithm
        LaxFAlgorithm();
        //Updates the scene mesh
   

        UpdateMesh();


        /*
        //Gets the average time of one run of the algorithm
        iterationNumber++;
        if (iterationNumber == 100)
        {
            totalTime = totalTime / 100;

            string path = "Assets/Honours/Resources/LF.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */

    }
}
