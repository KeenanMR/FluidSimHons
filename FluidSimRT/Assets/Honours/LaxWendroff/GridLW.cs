using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GridLW : MonoBehaviour
{
	//Grid size
	int xSize;

	//Mesh Data
	private Vector3[] vertices;
	private Mesh mesh;
	public Material waterMat;

	//Mesh scale
	float scale = 0.25f;

	//UI Input data
	public Toggle GPUToggle;
	public InputField gridSize;




	//Function that generates the grid
	public void Generate()
	{
		//Gets int data from input field text
		xSize = int.Parse(gridSize.text);


		//Setup mesh 
		GetComponent<MeshFilter>().mesh = mesh = new Mesh();
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		GetComponent<Renderer>().material = waterMat;

		//Attach algorithm script
		if (!GPUToggle.isOn)
		{
			Lax_Wendroff script = gameObject.AddComponent<Lax_Wendroff>();
			script.Init(xSize);
		}
        else
        {
			LaxWGPU script2 = gameObject.AddComponent<LaxWGPU>();
			script2.Init(xSize);
		}

	

		//Generate mesh vertices
		vertices = new Vector3[(xSize + 1) * (xSize + 1)];
		Vector2[] uv = new Vector2[vertices.Length];
		for (int i = 0, y = 0; y <= xSize; y++)
		{
			for (int x = 0; x <= xSize; x++, i++)
			{
				vertices[i] = new Vector3(x, 0, y);
				uv[i] = new Vector2((float)x / xSize, (float)y / xSize);

			}
		}

		mesh.vertices = vertices;
		mesh.uv = uv;

		//Generate mesh triangles
		int[] triangles = new int[xSize * xSize * 6];
		for (int ti = 0, vi = 0, y = 0; y < xSize; y++, vi++)
		{
			for (int x = 0; x < xSize; x++, ti += 6, vi++)
			{
				triangles[ti] = vi;
				triangles[ti + 3] = triangles[ti + 2] = vi + 1;
				triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
				triangles[ti + 5] = vi + xSize + 2;
			}
		}
		mesh.triangles = triangles;
		mesh.RecalculateNormals();

		//Apply proper scaling
		scale = ((xSize + 1) / 50);
		scale = 0.52f / scale;

		this.transform.localScale = new Vector3(scale, 1f, scale);
	}


	//remove the active script from the gameobject
	public void RemoveScript()
	{
		if (!GPUToggle.isOn)
		{
			Destroy(GetComponent<Lax_Wendroff>());
		}
		else
		{
			Destroy(GetComponent<LaxWGPU>());
		}

	}


}
