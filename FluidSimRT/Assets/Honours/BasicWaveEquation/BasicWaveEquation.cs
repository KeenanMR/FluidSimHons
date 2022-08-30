using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


using System.IO;

public class BasicWaveEquation : MonoBehaviour
{   //Iteration data
    int iterationNumber = 1;
    int maxIterations = 5000;

    //Grid Size data
    int xGridSize;
     int yGridSize;

    //Math Constants
    public float pSquared = 0.25f;

    //Data arrays
    float[,] height;
    float[,] lastNHeight;


    //Mid point coord variables
    int aX;
    int aY;

    //Scaling variables
    float xScale = 0;
    float yScale = 0;



    public bool done = false;

    // Start is called before the first frame update
    void Start()
    {
        //Get mesh data from scene 
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //Set initial data values
        for (int x = 0; x < xGridSize + 1; x++)
        {
            for (int y = 0; y < yGridSize + 1; y++)
            {
                height[x, y] = 0;
                lastNHeight[x, y] = 0;
            }
        }
    }

    //Initialisation function called from grid script
    public void Init(int gridXY)
    {
        xGridSize = gridXY + 1;
        yGridSize = gridXY + 1;

        height = new float[gridXY + 3, gridXY + 3];
        lastNHeight = new float[gridXY + 3, gridXY + 3];

        aX = xGridSize / 2;
        aY = yGridSize / 2;
    }



    //Wave equation algorithm function
    void RunAlgorithm()
    {


        
        //Calculates the height of the new point
        for (int x = 1; x < xGridSize + 1; x++)
        {
            for (int y = 1; y < yGridSize + 1; y++)
            {
                height[x, y] = height[x, y] + (pSquared * (height[x + 1, y] + height[x - 1, y] + height[x, y + 1] + height[x, y - 1])) - lastNHeight[x, y];
                lastNHeight[x, y] = height[x, y];

            }
        }

        
    }

    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //Update centre point height
        height[aX, aY] = 0.001f * Mathf.Sin(iterationNumber * 8 * Mathf.PI / (maxIterations - 1));


        //Calls main algorithm
        RunAlgorithm();
        iterationNumber++;


        //Updates the scene mesh
        for (int x = 0; x < xGridSize - 1; x++)
        {
            for (int y = 0; y < yGridSize - 1; y++)
            {

                if (x == aX)
                    xScale = 1;
                else
                    xScale = Mathf.Abs(aX - x);

                if (y == aY)
                    yScale = 1;
                else
                    yScale = Mathf.Abs(aY - y);

                float scale = Mathf.Pow((xScale + yScale),2.75f);

                vertices[(x * xGridSize) + y].y = (5000 + scale) * height[x, y];

            }
        }

        mesh.vertices = vertices;
     
        /*
        //Gets the average time of one run of the algorithm
        iterationNumber++;
        if (iterationNumber == 100)
        {
            totalTime = totalTime / 100;

            string path = "Assets/Honours/Resources/WE.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */
    }
    

}
