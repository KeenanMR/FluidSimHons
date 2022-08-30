using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;



using System.IO;
//Lax F Data Struct
public struct dataLaxF
{
    //Input Data
    public Vector4 heightData;
    public Vector4 fUData;
    public Vector4 fVData;
    public Vector4 uData;
    public Vector4 vData;

    //Output Data
    public float outputHeight;
    public float outputFU;
    public float outputFV;
    public float outputU;
    public float outputV;
};



public class LaxFGPU : MonoBehaviour
{
    //Grid variables
    int xGridSize;
    int yGridSize;

    //Data arrays
    float[,] height = new float[66, 66];
    float[,] fV = new float[66, 66];
    float[,] fU = new float[66, 66];
    float[,] v = new float[66, 66];
    float[,] u = new float[66, 66];


    //GPU/Compute shader variables
    private dataLaxF[] data;
    ComputeShader computeShader;

    // Start is called before the first frame update
    void Start()
    {
        //Setup Compute shader
        data = new dataLaxF[xGridSize * yGridSize];
        computeShader = (ComputeShader)Resources.Load("LaxFComputeShader");

        //Get mesh data from scene
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

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

    //Runs the algorithm on the GPU
    public void SimulateGPU()
    {
        //Set data to structs for GPU
        for (int x = 1; x < xGridSize-1; x++)
            for (int y = 1; y < yGridSize-1; y++)
            {
              
                dataLaxF inData = new dataLaxF();
                inData.heightData = new Vector4(height[x + 1, y], height[x - 1, y], height[x, y + 1], height[x, y - 1]);
                inData.fUData = new Vector4(fU[x + 1, y], fU[x - 1, y], fU[x, y + 1], fU[x, y - 1]);
                inData.fVData = new Vector4(fV[x + 1, y], fV[x - 1, y], fV[x, y + 1], fV[x, y - 1]);
                inData.uData = new Vector4(u[x + 1, y], u[x - 1, y], u[x, y + 1], u[x, y - 1]);
                inData.vData = new Vector4(v[x + 1, y], v[x - 1, y], v[x, y + 1], v[x, y - 1]);

                inData.outputHeight = height[x, y];
                inData.outputFU = fU[x, y];
                inData.outputFV = fV[x, y];
                inData.outputU = u[x, y];
                inData.outputV = v[x, y];


                data[(x * xGridSize) + y] = inData;
            }

        //Send data to GPU
        int totalSize = sizeof(float) * 25;
        ComputeBuffer dataBuffer = new ComputeBuffer(data.Length, totalSize);

       
        dataBuffer.SetData(data);
        computeShader.SetBuffer(0, "vDatas", dataBuffer);
        computeShader.Dispatch(0, data.Length / 128, 1, 1);
        dataBuffer.GetData(data);
       

        //Recieve and read data
        for (int x = 1; x < xGridSize-1; x++)
            for (int y = 1; y < xGridSize-1; y++)
            {
                dataLaxF outData = data[x * xGridSize + y];
                height[x, y] = outData.outputHeight;
                fU[x, y] = outData.outputFU;
                fV[x, y] = outData.outputFV;
                u[x, y] = outData.outputU;
                v[x, y] = outData.outputV;
            }

        dataBuffer.Dispose();
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



        //Calls the algorithm
        
        SimulateGPU();

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

        //Updates the scene mesh
        UpdateMesh();

        /*
        //Gets the average time of one run of the algorithm
        iterationNumber++;
        if (iterationNumber == 10)
        {
            totalTime = totalTime / 10;

            string path = "Assets/Honours/Resources/LFGPU.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */
    }
}
