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
    private int[] grid;
    private bool canUpdate = true;
    private GameObject[] cubesPool;
    private GameObject cubePrefab;
    private CameraController cameraController;
    void Start()
    {
        SetupTheCameraTarget();
        this.grid = GenerateRandomArray(this.sizeX, this.sizeY, this.sizeZ);
        this.cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Cube.prefab");
        this.cubesPool = InitialisePool();

        // this could work if we dive into it properly
        // this.gameObject.AddComponent<MeshFilter>();
        // this.gameObject.AddComponent<MeshRenderer>();
        // CombineMeshes(this.gameObject);
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

    private void ClearCubes()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
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


}
