using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;



using System.IO;
public class Lax_Wendroff : MonoBehaviour
{
    //Grid Variables
    int xGridSize;
    int yGridSize;

    //Constant 
    int n;

    //Equation variables
    float g = 9.8f;
    float dt = 0.01f;
    float dx = 0.5f;
    float dy = 0.5f;

    //Simplifiers
    float alpha;
    float beta;
    float gamma;
    float omega;

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


    // Start is called before the first frame update
    void Start()
    {
        //Get mesh data from scene
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //Set intial calculation values
        alpha = dt / (2 * dx);
        beta = dt / (2 * dy);
        gamma = dt / dx;
        omega = dt / dy;




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
         n = gridXY - 1;

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

    //Boundary conditions for wave reflection
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

    //Lax Wendroff algorithm function
    void LaxWAlgorithm()
    {
        Profiler.BeginSample("WAlgorithm");
        //First Half ---- X Direction

        //Height
        for (int x = 0; x < n + 1; x++)
        {
            for (int y = 0; y < n; y++)
            {
                Hx[x, y] = (H[x + 1, y + 1] + H[x, y + 1]) / 2;
                Hx[x, y] = Hx[x, y] - alpha * (U[x + 1, y + 1] - U[x, y + 1]);
            }
        }

        //X-Momen
        for (int x = 0; x < n + 1; x++)
        {
            for (int y = 0; y < n; y++)
            {
                Ux[x, y] = (U[x + 1, y + 1] + U[x, y + 1]) / 2;

                float f1 = (U[x + 1, y + 1]* U[x + 1, y + 1]) / H[x + 1, y + 1] + (g * 0.5f * (H[x + 1, y + 1]* H[x + 1, y + 1]));
                float f2 = (U[x, y + 1]* U[x, y + 1]) / H[x, y + 1] + (g * 0.5f * (H[x, y + 1]* H[x, y + 1]));

                Ux[x, y] = Ux[x, y] - alpha * (f1 - f2);



            }
        }

        //Y-Momen
        for (int x = 0; x < n + 1; x++)
        {
            for (int y = 0; y < n; y++)
            {
                Vx[x, y] = (V[x + 1, y + 1] + V[x, y + 1]) / 2;

                float f1 = U[x + 1, y + 1] * V[x + 1, y + 1] / H[x + 1, y + 1];
                float f2 = U[x, y + 1] * V[x, y + 1] / H[x, y + 1];

                Vx[x, y] = Vx[x, y] - alpha * (f1 - f2);


            }
        }

        //First Half ---- Y Direction

        //Height
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n + 1; y++)
            {
                Hy[x, y] = (H[x + 1, y + 1] + H[x + 1, y]) / 2;
                Hy[x, y] = Hy[x, y] - beta * (V[x + 1, y + 1] - V[x + 1, y]);
            }
        }


        //X-Momen
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n + 1; y++)
            {
                Uy[x, y] = (U[x + 1, y + 1] + U[x + 1, y]) / 2;

                float f1 = V[x + 1, y + 1] * U[x + 1, y + 1] / H[x + 1, y + 1];
                float f2 = V[x + 1, y] * U[x + 1, y] / H[x + 1, y];

                Uy[x, y] = Uy[x, y] - beta * (f1 - f2);


            }
        }


        //Y-Momen
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n + 1; y++)
            {
                Vy[x, y] = (V[x + 1, y + 1] + V[x + 1, y]) / 2;

                float f1 = (V[x + 1, y + 1]* V[x + 1, y + 1]) / H[x + 1, y + 1] + (g * 0.5f * (H[x + 1, y + 1]* H[x + 1, y + 1]));
                float f2 = (V[x + 1, y]* V[x + 1, y]) / H[x + 1, y] + (g * 0.5f * (H[x + 1, y]* H[x + 1, y]));

                Vy[x, y] = Vy[x, y] - beta * (f1 - f2);


            }
        }


        //Second Half

        //Height
        for (int x = 1; x < n + 1; x++)
        {
            for (int y = 1; y < n + 1; y++)
            {
                H[x, y] = H[x, y]-(gamma * (Ux[x, y - 1] - Ux[x - 1, y - 1])) - (omega * (Vy[x - 1, y] - Vy[x - 1, y - 1]));
            }
        }


        //X-Momen
        for (int x = 1; x < n + 1; x++)
        {
            for (int y = 1; y < n + 1; y++)
            {
                float f1 = (Ux[x, y - 1]* Ux[x, y - 1]) / Hx[x, y - 1] + (g * 0.5f * (Hx[x, y - 1]* Hx[x, y - 1]));
                float f2 = (Ux[x - 1, y - 1]* Ux[x - 1, y - 1]) / Hx[x - 1, y - 1] + (g * 0.5f * (Hx[x - 1, y - 1]* Hx[x - 1, y - 1]));
                float f3 = Vy[x - 1, y] * Uy[x - 1, y] / Hy[x - 1, y];
                float f4 = Vy[x - 1, y - 1] * Uy[x - 1, y - 1] / Hy[x - 1, y - 1];

                U[x, y] = U[x, y]- (gamma * (f1 - f2)) - (omega * (f3 - f4));

             
            }
        }



        //Y-Momen
        for (int x = 1; x < n + 1; x++)
        {
            for (int y = 1; y < n + 1; y++)
            {
                float f1 = Ux[x, y - 1] * Vx[x, y - 1] / Hx[x, y - 1];
                float f2 = Ux[x - 1, y - 1] * Vx[x - 1, y - 1] / Hx[x - 1, y - 1];
                float f3 = (Vy[x - 1, y]* Vy[x - 1, y]) / Hy[x - 1, y] + (g * 0.5f * (Hy[x - 1, y]* Hy[x - 1, y]));
                float f4 = (Vy[x - 1, y - 1]* Vy[x - 1, y - 1]) / Hy[x - 1, y - 1] + (g * 0.5f * (Hy[x - 1, y - 1]* Hy[x - 1, y - 1]));

                V[x, y] = V[x, y] - (gamma * (f1 - f2)) - (omega * (f3 - f4));


            }
        }

        Profiler.EndSample();
    }

    //Update the mesh in the scene
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

                if (float.IsNaN(H[x, y]))
                    run = false;
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

            int xStart = Random.Range(1, xGridSize-20);
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
            //Checks boundary conditions
            Boundaries();
            //Calls main algorithm
            LaxWAlgorithm();
            //Updates the scene mesh
            UpdateMesh();

            

            //iterationNumber++;
        }

        /*
        //Gets the average time of one run of the algorithm
        if (iterationNumber == 100)
        {
            totalTime = totalTime / 100;

            string path = "Assets/Honours/Resources/LW.txt";

            StreamWriter writer = new StreamWriter(path, true);

            writer.WriteLine(totalTime.ToString());

            writer.Close();

            Debug.Log(xGridSize.ToString());
            done = true;

        }
        */
    }



}

