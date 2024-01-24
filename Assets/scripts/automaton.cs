using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

public class automaton : MonoBehaviour
{
    [SerializeField]
    [Header("Size Settings")]
    private int sizeX = 30;
    [SerializeField]
    private int sizeY = 30;
    [SerializeField]
    private int sizeZ = 30;

    [SerializeField]
    [Header("Rules Settings")]
    private int underpopulationValue = 6;
    [SerializeField]
    private int overpopulationValue = 9;
    [SerializeField]
    private int[] reviveValues = { 9, 10, 11 };

    [SerializeField]
    [Header("Delay before each step")]
    private float stepDelay = 1f;

    public ComputeShader computeShader;
    private int[] grid;
    private GameObject[] cubesPool;
    private List<List<Matrix4x4>> batches = new List<List<Matrix4x4>>();
    public Material[] materials;
    public Mesh mesh;
    private GameObject cubePrefab;
    private CameraController cameraController;
    private float updateCounter = 0;
    void Start()
    {
        this.updateCounter = this.stepDelay;
        SetupTheCameraTarget();
        this.GenerateRandomBatches();
        
    }


    void Update()
    {
        if(this.updateCounter <= 0)
        {
            this.GenerateRandomBatches();
            this.updateCounter = this.stepDelay;
        }
        else
        {
            this.updateCounter -= Time.deltaTime;
        }
        RenderBatches();
    }

    private void SetupTheCameraTarget()
    {
        GameObject cameraObject = GameObject.FindWithTag("MainCamera");
        CameraController cameraController = cameraObject.GetComponent<CameraController>();
        cameraController.SetTarget(new Vector3(this.sizeX / 2, this.sizeY / 2, this.sizeZ / 2));
        cameraController.SetMaxZoom(2 * Math.Max(this.sizeX, Math.Max(this.sizeZ, this.sizeY)));
        cameraController.SetCurrentZoom(4.0f * (Math.Max(this.sizeX, Math.Max(this.sizeZ, this.sizeY)) / 5.0f) + cameraController.getMinZoom());
    }

    private void ActivateAliveCells()
    {
        for (int i = 0; i < this.sizeX * this.sizeY * this.sizeZ; i++)
        {

            if (this.grid[i] == 1)
                this.cubesPool[i].SetActive(true);
            else
                this.cubesPool[i].SetActive(false);

        }
    }

    private void NextGenComputeShader()
    {
        ComputeBuffer computeBuffer = new ComputeBuffer(this.grid.Length , sizeof (int));
        computeBuffer.SetData(this.grid);

        computeShader.SetBuffer(0, "Result", computeBuffer);
        computeShader.Dispatch(0, this.grid.Length / 64, 1, 1);
        computeBuffer.GetData(this.grid);
        computeBuffer.Dispose();
    }
    private void NextGen()
    {
        int[] array = new int[sizeX * sizeY * sizeZ];
        Parallel.For(0, this.sizeX * this.sizeY * this.sizeZ, i =>
        {

            int aliveNeighbours = CountAliveNeighbours(i);
            if (this.grid[i] == 1)
            {
                if (aliveNeighbours <= this.overpopulationValue && aliveNeighbours >= this.underpopulationValue)
                {
                    array[i] = 1;
                }
                else if (aliveNeighbours < this.underpopulationValue || aliveNeighbours > this.overpopulationValue)
                {
                    array[i] = 0;
                }
            }
            else
            {
                if (Array.Exists(this.reviveValues, element => element == aliveNeighbours))
                {
                    array[i] = 1;
                }
            }

        });
        this.grid = array;
    }


    private int CountAliveNeighbours(int index)
    {
        int result = 0;
        (int x, int y, int z) = this.To3D(index);

        for (int i = Math.Max(0, x - 1); i < Math.Min(this.sizeX, x + 2); i++)
        {
            for (int j = Math.Max(0, y - 1); j < Math.Min(this.sizeY, y + 2); j++)
            {
                for (int k = Math.Max(0, z - 1); k < Math.Min(this.sizeZ, z + 2); k++)
                {
                    if (i == x && j == y && k == z)
                        continue;

                    int neighbourIndex = this.To1D(i, j, k);
                    result += this.grid[neighbourIndex];
                }
            }
        }

        return result;
    }


    public static int[] GenerateRandomArray(int sizeX, int sizeY, int sizeZ)
    {
        int[] array = new int[sizeX * sizeY * sizeZ];
        for (int i = 0; i < sizeX * sizeY * sizeZ; i++)
        {

            array[i] = UnityEngine.Random.Range(0, 2);

        }
        return array;
    }

    private void GenerateRandomBatches()
    {
        int size = this.sizeX * this.sizeY * this.sizeZ;
        this.batches = new List<List<Matrix4x4>>();
        for (int i = 0; i < Math.Floor((double)(size / 1000)); i++)
        {
            List<Matrix4x4> temporary = new List<Matrix4x4>();
            for (int j = 0; j < Math.Min(1000, size - (i+1)*1000); j++)
            {
                int choice = UnityEngine.Random.Range(0, 2);
                if(choice == 1)
                {
                    (int x, int y, int z) = this.To3D(i*1000 + j);
                    temporary.Add(Matrix4x4.TRS(new Vector3(x, y ,z), Quaternion.identity, new Vector3(1,1,1)));
                }   
            }
            this.batches.Add(temporary);
        }
    }

    private GameObject CreateCube(Vector3 position, float size)
    {
        GameObject cube = Instantiate(this.cubePrefab, position, Quaternion.identity);
        cube.transform.localScale = new Vector3(size, size, size);
        cube.transform.SetParent(this.transform);
        cube.SetActive(false);
        return cube;
    }



    private GameObject[] InitialisePool()
    {
        GameObject[] result = new GameObject[this.sizeX * this.sizeY * this.sizeZ];
        for (int i = 0; i < this.sizeX * this.sizeY * this.sizeZ; i++)
        {
            (int x, int y, int z) = this.To3D(i);
            result[i] = CreateCube(new Vector3(x, y, z), 1f);

        }
        return result;
    }



    public int To1D(int x, int y, int z)
    {
        return x + (y * this.sizeX) + (z * this.sizeX * this.sizeY);
    }

    public (int, int, int) To3D(int i)
    {
        int z = i / (this.sizeX * this.sizeY);
        i %= this.sizeX * this.sizeY;
        int y = i / this.sizeX;
        int x = i % this.sizeX;
        return (x, y, z);
    }

    private void RenderBatches()
    {
        foreach (var batch in this.batches)
        {
            for(int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstanced(this.mesh, i, this.materials[i], batch);
            }
        }
    }


}
