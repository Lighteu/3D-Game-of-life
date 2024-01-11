using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

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
    private int[,,] grid;
    private bool canUpdate = true;
    private GameObject[,,] cubesPool;
    private GameObject cubePrefab;
    private CameraController cameraController;
    void Start()
    {
        SetupTheCameraTarget();
        this.grid = GenerateRandomArray(this.sizeX, this.sizeY, this.sizeZ);
        this.cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Cube.prefab");
        this.cubesPool = InitialisePool();
        this.gameObject.AddComponent<MeshFilter>();
        this.gameObject.AddComponent<MeshRenderer>();
        CombineMeshes(this.gameObject);
        ActivateAliveCells();
    }


    void Update()
    {
        if (this.canUpdate)
        {
            StartCoroutine(DelayUpdate());
        }
    }

    IEnumerator DelayUpdate()
    {
        this.canUpdate = false;
        yield return new WaitForSeconds(this.stepDelay);
        NextGen();
        ActivateAliveCells();
        this.canUpdate = true;
    }

    private void SetupTheCameraTarget()
    {
        GameObject cameraObject = GameObject.FindWithTag("MainCamera");
        CameraController cameraController = cameraObject.GetComponent<CameraController>();
        cameraController.SetTarget(new Vector3(this.sizeX/2, this.sizeY/2, this.sizeZ/2));
        cameraController.SetMaxZoom(2 * Math.Max(this.sizeX, Math.Max(this.sizeZ, this.sizeY)));
        cameraController.SetCurrentZoom(4.0f * (Math.Max(this.sizeX, Math.Max(this.sizeZ, this.sizeY)) / 5.0f) + cameraController.getMinZoom());
    }

    private void ActivateAliveCells()
    {
        for (int x = 0; x < this.sizeX; x++)
        {
            for (int y = 0; y < this.sizeY; y++)
            {
                for (int z = 0; z < this.sizeZ; z++)
                {
                    if (this.grid[x, y, z] == 1)
                        this.cubesPool[x, y, z].SetActive(true);
                    else
                        this.cubesPool[x, y, z].SetActive(false);
                }
            }
        }
    }

    private void NextGen()
    {
        int[,,] array3D = new int[sizeX, sizeY, sizeZ];
        Parallel.For(0, this.sizeX, x =>
        {
            for (int y = 0; y < this.sizeY; y++)
            {
                for (int z = 0; z < this.sizeZ; z++)
                {
                    int aliveNeighbours = CountAliveNeighbours(x, y, z);
                    if (this.grid[x, y, z] == 1)
                    {
                        if (aliveNeighbours <= this.overpopulationValue && aliveNeighbours >= this.underpopulationValue)
                        {
                            array3D[x, y, z] = 1;
                        }
                        else if (aliveNeighbours < this.underpopulationValue || aliveNeighbours > this.overpopulationValue)
                        {
                            array3D[x, y, z] = 0;
                        }
                    }
                    else
                    {
                        if (Array.Exists(this.reviveValues, element => element == aliveNeighbours))
                        {
                            array3D[x, y, z] = 1;
                        }
                    }
                }
            }
        });
        this.grid = array3D;
    }


    private int CountAliveNeighbours(int x, int y, int z)
    {
        int result = 0;
        for (int i = Math.Max(0, x - 1); i < Math.Min(this.sizeX, x + 2); i++)
        {
            for (int j = Math.Max(0, y - 1); j < Math.Min(this.sizeY, y + 2); j++)
            {
                for (int k = Math.Max(0, z - 1); k < Math.Min(this.sizeZ, z + 2); k++)
                {
                    if (i == x && j == y && k == z)
                        continue;

                    result += this.grid[i, j, k];
                }
            }
        }
        return result;
    }

    public static int[,,] GenerateRandomArray(int sizeX, int sizeY, int sizeZ)
    {
        int[,,] array3D = new int[sizeX, sizeY, sizeZ];
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    array3D[x, y, z] = UnityEngine.Random.Range(0, 2);
                }
            }
        }
        return array3D;
    }

    private GameObject CreateCube(Vector3 position, float size)
    {
        GameObject cube = Instantiate(this.cubePrefab, position, Quaternion.identity);
        cube.transform.localScale = new Vector3(size, size, size);

        cube.transform.SetParent(this.transform);
        cube.SetActive(false);
        return cube;
    }

    private void CombineMeshes(GameObject parentObject)
    {
        MeshFilter[] meshFilters = parentObject.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }

        parentObject.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        parentObject.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        parentObject.transform.gameObject.SetActive(true);
    }


    private GameObject[,,] InitialisePool()
    {
        GameObject[,,] result = new GameObject[this.sizeX, this.sizeY, this.sizeZ];
        for (int x = 0; x < this.sizeX; x++)
        {
            for (int y = 0; y < this.sizeY; y++)
            {
                for (int z = 0; z < this.sizeZ; z++)
                {
                    result[x, y, z] = CreateCube(new Vector3(x, y, z), 1f);
                }
            }
        }
        return result;
    }

    private void ClearCubes()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

}
