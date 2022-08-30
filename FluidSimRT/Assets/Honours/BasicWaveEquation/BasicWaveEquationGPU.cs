using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


using System.IO;
public struct VertexData
{
    public float height;
    public float posX;
    public float posY;
    public float negX;
    public float negY;
    public float prevHeight;
}


public class BasicWaveEquationGPU : MonoBehaviour
{
    //Iteration data
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



    //Compute shader variables
    private VertexData[] data;
    public ComputeShader computeShader;

    //Scaling variables
    float xScale = 0;
    float yScale = 0;

    // Start is called before the first frame update
    void Start()
    {
        //Setup Compute shader
        data = new VertexData[xGridSize*yGridSize];
        computeShader = (ComputeShader)Resources.Load("BasicWaveEquationCS");
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

    //Runs the algorithm on the GPU
    public void SimulateGPU()
    {
        //Set data to structs for GPU
        for (int x = 1; x < xGridSize; x++)
            for (int y = 1; y < yGridSize; y++)
            {
                VertexData inData = new VertexData();
                inData.height = height[x, y];
                inData.negX = height[x - 1, y];
                inData.posX = height[x + 1, y];
                inData.negY = height[x, y - 1];
                inData.posY = height[x, y + 1];
                inData.prevHeight = lastNHeight[x, y];

                data[(x * xGridSize) + y] = inData;
            }

        //Send data to GPU
        int totalSize = sizeof(float) *6;
        ComputeBuffer dataBuffer = new ComputeBuffer(data.Length, totalSize);

        
        dataBuffer.SetData(data);
        computeShader.SetBuffer(0, "vDatas", dataBuffer);
        computeShader.Dispatch(0, data.Length / 128, 1, 1);
        dataBuffer.GetData(data);
       


        lastNHeight = height;

        //Recieve and read data
        for (int x = 1; x < xGridSize; x++)
            for (int y = 1; y < xGridSize; y++)
            {
                VertexData outData = data[x * xGridSize + y];
                height[x, y] = outData.height;
            }

        dataBuffer.Dispose();

    }


    void UpdateMesh()
    {

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        


        //Translate 2D array to 1D vector for height also scales based on distance from centre point 
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

                float scale = Mathf.Pow((xScale + yScale), 2.75f);

                vertices[(x * xGridSize) + y].y = (5000 + scale) * height[x, y];

            }
        }

        mesh.vertices = vertices;
    }


    // Update is called once per frame
    void Update()
    {

        //Update centre point height
        height[aX, aY] = 0.001f * Mathf.Sin(iterationNumber * 8 * Mathf.PI / (maxIterations - 1));

        //Calls main algorithm
        SimulateGPU();
        
        //Updates the scene mesh
        UpdateMesh();
        iterationNumber++;
        /*
        //Gets the average time of one run of the algorithm
        iterationNumber++;
        if (iterationNumber == 100)
        {
            totalTime = totalTime / 100;

            string path = "Assets/Honours/Resources/WEGPU.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */
    }
}
