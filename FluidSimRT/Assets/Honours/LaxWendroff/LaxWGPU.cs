using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


using System.IO;
//Lax W Data Struct X
public struct dataLaxW_X
{
    public float HXValue;
    public float UXValue;
    public float VXValue;

    public Vector2 HValues;
    public Vector2 UValues;
    public Vector2 VValues;

};
//Lax W Data Struct Y
public struct dataLaxW_Y
{
    public float HYValue;
    public float UYValue;
    public float VYValue;

    public Vector2 HValues;
    public Vector2 UValues;
    public Vector2 VValues;

};
//Lax W Data Struct Final
public struct dataLaxW_F
{
    public float HValue;
    public float UValue;
    public float VValue;

    public Vector2 UXValues;
    public Vector2 HXValues;
    public Vector2 VYValues;
    public Vector2 UYValues;
    public Vector2 HYValues;
    public Vector2 VXValues;
};

public class LaxWGPU : MonoBehaviour
{

    //Grid variables
    int xGridSize;
    int yGridSize;

    //bool to run
    bool run = true;

    //Heigth arrays
    float[,] H;
    float[,] Hx;
    float[,] Hy;

    //X momentum arrays
    float[,] Ux;
    float[,] Vx;
    float[,] U;

    //Y momentum arrays
    float[,] Uy;
    float[,] Vy;
    float[,] V;

    //GPU data arrays 
    private dataLaxW_X[] xData;
    private dataLaxW_Y[] yData;
    private dataLaxW_F[] fData;

    //Compute shader variable
    ComputeShader computeShader;



    // Start is called before the first frame update
    void Start()
    {
        //Init GPU data arrays and set compute shader
        xData = new dataLaxW_X[xGridSize * yGridSize];
        yData = new dataLaxW_Y[xGridSize * yGridSize];
        fData = new dataLaxW_F[xGridSize * yGridSize];
        computeShader = (ComputeShader)Resources.Load("LaxWCs");

        //Get mesh data from scene
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;


        //Set initial data values
        for (int x = 0; x < xGridSize; x++)
        {
            for (int y = 0; y < yGridSize; y++)
            {
                H[x, y] += 1;
                Hx[x, y] = 0;
                Hy[x, y] = 0;
                Ux[x, y] = 0;
                Uy[x, y] = 0;
                Vx[x, y] = 0;
                Vy[x, y] = 0;
                U[x, y] = 0;
                V[x, y] = 0;
            }
        }

        //Set the mesh from the height data
        for (int x = 0; x < xGridSize; x++)
        {
            for (int y = 0; y < yGridSize; y++)
            {

                vertices[(x * xGridSize) + y].y = H[x, y];

            }

        }

        mesh.vertices = vertices;
    }

    //Initialisation function called from grid script
    public void Init(int gridXY)
    {
        xGridSize = gridXY + 1;
        yGridSize = gridXY + 1;

        //Heigth arrays
        H = new float[gridXY + 1, gridXY + 1];
        Hx = new float[gridXY + 1, gridXY + 1];
        Hy = new float[gridXY + 1, gridXY + 1];

        //X momentum arrays
        Ux = new float[gridXY + 1, gridXY + 1];
        Vx = new float[gridXY + 1, gridXY + 1];
        U = new float[gridXY + 1, gridXY + 1];

        //Y momentum arrays
        Uy = new float[gridXY + 1, gridXY + 1];
        Vy = new float[gridXY + 1, gridXY + 1];
        V = new float[gridXY + 1, gridXY + 1];

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
                vertices[x + (xGridSize * y)].y = H[x, y];
            }
        }
        mesh.vertices = vertices;
    }

    //Runs the algorithm on the GPU for the x-direction
    void SimulateGPU_X()
    {

        //Set data to structs for GPU
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < yGridSize - 1; y++)
            {

                dataLaxW_X inData = new dataLaxW_X();
                inData.HXValue = Hx[x, y];
                inData.UXValue = Ux[x, y];
                inData.VXValue = Vx[x, y];

                inData.HValues = new Vector2(H[x + 1, y + 1], H[x, y + 1]);
                inData.UValues = new Vector2(U[x + 1, y + 1], U[x, y + 1]);
                inData.VValues = new Vector2(V[x + 1, y + 1], V[x, y + 1]);


                xData[(x * xGridSize) + y] = inData;
            }

        //Send data to GPU
        int totalSize = sizeof(float) * 9;
        ComputeBuffer dataBuffer = new ComputeBuffer(xData.Length, totalSize);

        
        dataBuffer.SetData(xData);
        computeShader.SetBuffer(0, "xDatas", dataBuffer);
        computeShader.Dispatch(0, xData.Length / 128, 1, 1);
        dataBuffer.GetData(xData);
        

        //Recieve and read data
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < xGridSize - 1; y++)
            {
                dataLaxW_X outData = xData[x * xGridSize + y];
                Hx[x, y] = outData.HXValue;
                Ux[x, y] = outData.UXValue;
                Vx[x, y] = outData.VXValue;

            }

  
        dataBuffer.Dispose();

        
    }

    //Runs the algorithm on the GPU for the y-direction
    void SimulateGPU_Y()
    {


        //Set data to structs for GPU
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < yGridSize - 1; y++)
            {

                dataLaxW_Y inData = new dataLaxW_Y();
                inData.HYValue = Hy[x, y];
                inData.UYValue = Uy[x, y];
                inData.VYValue = Vy[x, y];

                inData.HValues = new Vector2(H[x + 1, y + 1], H[x + 1, y]);
                inData.UValues = new Vector2(U[x + 1, y + 1], U[x + 1, y]);
                inData.VValues = new Vector2(V[x + 1, y + 1], V[x + 1, y]);


                yData[(x * xGridSize) + y] = inData;
            }

        //Send data to GPU
        int totalSize = sizeof(float) * 9;
        ComputeBuffer dataBuffer = new ComputeBuffer(yData.Length, totalSize);

        
        dataBuffer.SetData(yData);
        computeShader.SetBuffer(1, "yDatas", dataBuffer);
        computeShader.Dispatch(1, yData.Length / 128, 1, 1);
        dataBuffer.GetData(yData);
       


        //Recieve and read data
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < xGridSize - 1; y++)
            {
                dataLaxW_Y outData = yData[x * xGridSize + y];
                Hy[x, y] = outData.HYValue;
                Uy[x, y] = outData.UYValue;
                Vy[x, y] = outData.VYValue;

            }

        dataBuffer.Dispose();


    }

    //Runs the algorithm on the GPU for the second step
    void SimulateGPU_F()
    {

        //Set data to structs for GPU
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < yGridSize - 1; y++)
            {

                dataLaxW_F inData = new dataLaxW_F();
                inData.HValue = H[x, y];
                inData.UValue = U[x, y];
                inData.VValue = V[x, y];

                inData.UXValues = new Vector2(Ux[x,y - 1], Ux[x - 1,y - 1]);
                inData.HXValues = new Vector2(Hx[x,y - 1], Hx[x - 1, y - 1]);
                inData.VYValues = new Vector2(Vy[x - 1, y], Vy[x - 1, y - 1]);
                inData.UYValues = new Vector2(Uy[x - 1, y], Uy[x - 1, y - 1]);
                inData.HYValues = new Vector2(Hy[x - 1, y], Hy[x - 1, y - 1]);
                inData.VXValues = new Vector2(Vx[x, y - 1], Vx[x - 1, y - 1]);


                fData[(x * xGridSize) + y] = inData;
            }

        //Send data to GPU
        int totalSize = sizeof(float) * 15;
        ComputeBuffer dataBuffer = new ComputeBuffer(fData.Length, totalSize);

        
        dataBuffer.SetData(fData);
        computeShader.SetBuffer(2, "fDatas", dataBuffer);
        computeShader.Dispatch(2, fData.Length / 128, 1, 1);
        dataBuffer.GetData(fData);
        

        //Recieve and read data
        for (int x = 1; x < xGridSize - 1; x++)
            for (int y = 1; y < xGridSize - 1; y++)
            {
                dataLaxW_F outData = fData[x * xGridSize + y];
                H[x, y] = outData.HValue;
                U[x, y] = outData.UValue;
                V[x, y] = outData.VValue;

            }

        dataBuffer.Dispose();


    }


    void Boundaries()
    {
        //Boundary conditions
        for (int x = 0; x < xGridSize - 1; x++)
        {
            H[x, 1] = H[x, 2];
            U[x, 1] = U[x, 2];
            V[x, 1] = -V[x, 2];

            H[x, yGridSize - 2] = H[x, yGridSize - 3];
            U[x, yGridSize - 2] = U[x, yGridSize - 3];
            V[x, yGridSize - 2] = -V[x, yGridSize - 3];
        }

        for (int y = 0; y < yGridSize - 1; y++)
        {
            H[1, y] = H[2, y];
            U[1, y] = -U[2, y];
            V[1, y] = V[2, y];

            H[xGridSize - 2, y] = H[xGridSize - 3, y];
            U[xGridSize - 2, y] = -U[xGridSize - 3, y];
            V[xGridSize - 2, y] = V[xGridSize - 3, y];
        }
    }


    // Update is called once per frame
    void Update()
    {
        //Simulate water drop
        if (Input.GetKeyDown(KeyCode.Space))
        {

            int xStart = Random.Range(1, xGridSize - 20);
            int yStart = Random.Range(1, xGridSize - 20);
            for (int x = 0; x < 21; x++)
            {
                for (int y = 0; y < 21; y++)
                {
                    H[xStart + x, yStart + y] += 5f * Mathf.Exp(-5 * (Mathf.Pow(-1f + (0.1f * x), 2) + Mathf.Pow(-1f + (0.1f * y), 2)));
                }
            }
        }


        if (run)
        {
            Boundaries();

            //Calls all the steps main algorithm for simulation on GPU
            SimulateGPU_X();
            SimulateGPU_Y();
            SimulateGPU_F();
        
            //Updates the grid on the scene 
            UpdateMesh();
        }

        /*
        //Gets the average time of one run of the algorithm

        iterationNumber++;
        if (iterationNumber == 100)
        {
            totalTime = totalTime / 100;

            string path = "Assets/Honours/Resources/LWGPU.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */
    }
}
