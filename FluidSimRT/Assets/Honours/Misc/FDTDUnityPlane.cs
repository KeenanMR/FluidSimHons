using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FDTDUnityPlane : MonoBehaviour
{
    public float amplitude = 0;
    public float frequency = 0;
    public float speed = 0;

    float timeStep = 0.1f;
    float spaceStep = 0.2f;
    float timer = 0f;

    List<float> xVelocity = new List<float>();
    List<float> yVelocity = new List<float>();
    List<float> zetaU = new List<float>();
    List<float> zetaV = new List<float>();
    List<float> zeta = new List<float>();
    List<int> bounds = new List<int>();

    bool running = true;

    // Start is called before the first frame update
    public void OnAmpUpdate(float value) { amplitude = value; }
    public void OnFreqUpdate(float value) { frequency = value; }
    public void OnSpeedUpdate(float value) { speed = value; }

    public Vector3 NearestToClick(Vector3[] localVertices)
    {
        Vector3 mousePos = transform.InverseTransformPoint(Input.mousePosition);

        float minDistanceSqr = Mathf.Infinity;
        Vector3 nearestVertex = Vector3.zero;

        for (var i = 0; i <= 5; i++)
        {

            Vector3 diff = mousePos - localVertices[i];
            float distSqr = diff.sqrMagnitude;

            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                nearestVertex = localVertices[i];

            }
        }

        return transform.TransformPoint(nearestVertex);
    }

    
    void Start()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        //Set starting conditions
        for (int m = 0; m <= vertices.Length - 1; m++)
        {
            vertices[m].y = 1.0f;
            //Get rid of X for line wave in 1D
            vertices[m].y += Mathf.Exp(-5 * (Mathf.Pow(vertices[m].z, 2)+ Mathf.Pow(vertices[m].x, 2)));
        }

        //Set bounds to 0
        for (var m = 0; m < 11; m++)
        {
            vertices[m].y = 1;
            bounds.Add(m);
            vertices[m + 110].y = 1;
            bounds.Add(m + 110);
            vertices[m * 11].y = 1;
            bounds.Add(m * 11);
            vertices[m * 11 + 10].y = 1;
            bounds.Add(m * 11 + 10);

        }
      
        //Set initial list variables
        for (var i = 0; i <= vertices.Length - 1; i++)
        {
            xVelocity.Add(0f);
            yVelocity.Add(0f);
            zetaU.Add(0f);
            zetaV.Add(0f);
            zeta.Add(vertices[i].y);
        }

        //Update mesh
        mesh.vertices = vertices;
    }
    

    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        if (Input.GetKeyDown(KeyCode.Space) || (timer > 0.2f && running))
        {
            timer = 0;

            for (var m = 0; m <= vertices.Length - 1; m++)
            {
                    //Equation 1
                if (!bounds.Contains(m))
                {
                    //zeta[m] = vertices[m].y - ((0.5f * (timeStep / spaceStep)) * ((vertices[m + 11].y * xVelocity[m + 11]) - (vertices[m - 11].y * xVelocity[m - 11])));
                    zeta[m] = vertices[m].y - ((0.5f * (timeStep / spaceStep)) * (((vertices[m + 11].y * xVelocity[m + 11]) - (vertices[m - 11].y * xVelocity[m - 11])) - (vertices[m + 1].y * yVelocity[m + 1]) - (vertices[m - 1].y * yVelocity[m - 1])));
                    zeta[m] = Mathf.Clamp(zeta[m], 0, 2);
                }

                if(float.IsNaN(zeta[m]))
                {
                    Debug.Log("broke");
                    running = false;
                }
                
            }

            //List of current timestep zetaU values
            List<float> oldZetaU = zetaU;

            for (var m = 0; m <= vertices.Length - 1; m++)
            {
                //Equation 2
                if (!bounds.Contains(m))
                {
                    //zetaU[m] = oldZetaU[m] - ((0.5f*(timeStep / spaceStep)) * (zeta[m + 11] * Mathf.Pow(xVelocity[m + 11], 2)) - (zeta[m - 11] * Mathf.Pow(xVelocity[m - 11], 2)) + ((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m + 11], 2)) - ((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m - 11], 2)));
                    zetaU[m] = oldZetaU[m] - ((0.5f * (timeStep / spaceStep)) * ((zeta[m + 11] * Mathf.Pow(xVelocity[m + 11], 2)) - (zeta[m - 11] * Mathf.Pow(xVelocity[m - 11], 2)) + ((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m + 11], 2)) - ((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m - 11], 2))) + ((zeta[m + 11] * xVelocity[m + 11] * yVelocity[m + 11]) - (zeta[m - 11] * xVelocity[m - 11] * yVelocity[m - 11])));
                    zetaU[m] = Mathf.Clamp(zetaU[m], 0, 2);
                }
            }


            //List of current timestep zetaV values
            List<float> oldZetaV = zetaU;

            for (var m = 0; m <= vertices.Length - 1; m++)
            {
                //Equation 3
                if (!bounds.Contains(m))
                {
                    zetaV[m] = oldZetaV[m] - ((0.5f * (timeStep / spaceStep)) * ((zeta[m + 1] * xVelocity[m + 1] * yVelocity[m + 1]) - (zeta[m - 1] * xVelocity[m - 1] * yVelocity[m - 1])+(zeta[m+1] * Mathf.Pow(yVelocity[m + 1], 2) - zeta[m - 1] * Mathf.Pow(yVelocity[m - 1], 2)) +((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m + 1], 2)) - ((0.5f/* * -9.8f*/) * Mathf.Pow(zeta[m - 1], 2))));
                    zetaV[m] = Mathf.Clamp(zetaV[m], 0, 2);
                }
            }

            for (var m = 0; m <= vertices.Length - 1; m++)
            {
                //Equation 4 & 5
                if (!bounds.Contains(m))
                {
                    xVelocity[m] = zetaU[m] / zeta[m];
                    xVelocity[m] = Mathf.Clamp(xVelocity[m], 0, 2);
                    yVelocity[m] = zetaV[m] / zeta[m];
                    yVelocity[m] = Mathf.Clamp(yVelocity[m], 0, 2);
                    vertices[m].y = zeta[m];
                }
            }

            Debug.Log("");

            foreach(var item in bounds)
            {
                if(item < 11)
                {
                    vertices[item].y = vertices[item + 11].y;
                    xVelocity[item] = xVelocity[item + 11];
                }
                else if(item > 109)
                {
                    vertices[item].y = vertices[item - 11].y;
                    xVelocity[item] = xVelocity[item - 11];
                }
                else if(item % 11 == 0)
                {
                    vertices[item].y = vertices[item - 1].y;
                    yVelocity[item] = yVelocity[item - 1];
                }
                else
                {
                    vertices[item].y = vertices[item + 1].y;
                    yVelocity[item] = yVelocity[item + 1];
                }

            }

        }

        if (running)
        {
            mesh.vertices = vertices;
            timer += Time.deltaTime;
        }
    }
}
