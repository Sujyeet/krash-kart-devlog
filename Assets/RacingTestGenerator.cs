using UnityEngine;
using System.Collections.Generic;

public class RacingTestGenerator : MonoBehaviour
{
    // Runs automatically in the Editor the moment you attach this script to a GameObject
    private void Reset()
    {
        SpawnTestModels();
    }

    // Also accessible by right-clicking the component title bar in the Inspector
    [ContextMenu("Spawn Racing Test Models")]
    public void SpawnTestModels()
    {
        SpawnProceduralApexCurb();
        SpawnPrototypeCar();
        Debug.Log("Spawned Procedural_Apex_Curb and Prototype_Racer at (0,0,0)!");
    }

    private void SpawnProceduralApexCurb()
    {
        GameObject curbObj = new GameObject("Procedural_Apex_Curb");
        curbObj.transform.position = transform.position + new Vector3(-2.5f, 0f, 0f);

        MeshFilter mf = curbObj.AddComponent<MeshFilter>();
        MeshRenderer mr = curbObj.AddComponent<MeshRenderer>();

        int segments = 8;
        float segLength = 1.0f;
        float width = 0.8f;
        float height = 0.12f;

        Vector3[] verts = new Vector3[(segments + 1) * 3];
        List<int> redTris = new List<int>();
        List<int> whiteTris = new List<int>();

        for (int i = 0; i <= segments; i++)
        {
            float z = i * segLength;
            verts[i * 3 + 0] = new Vector3(0f, 0f, z);
            verts[i * 3 + 1] = new Vector3(width * 0.6f, height, z);
            verts[i * 3 + 2] = new Vector3(width, height, z);
        }

        for (int i = 0; i < segments; i++)
        {
            int b = i * 3;
            int n = (i + 1) * 3;
            List<int> targetList = (i % 2 == 0) ? redTris : whiteTris;

            targetList.AddRange(new int[] { b, n, n + 1, b, n + 1, b + 1 });
            targetList.AddRange(new int[] { b + 1, n + 1, n + 2, b + 1, n + 2, b + 2 });
        }

        Mesh mesh = new Mesh { name = "ApexCurb_Mesh", subMeshCount = 2 };
        mesh.vertices = verts;
        mesh.SetTriangles(redTris, 0);
        mesh.SetTriangles(whiteTris, 1);
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;
        mr.sharedMaterials = new Material[] {
            CreateColorMaterial("Curb_Red", new Color(0.85f, 0.1f, 0.1f)),
            CreateColorMaterial("Curb_White", new Color(0.92f, 0.92f, 0.92f))
        };
        curbObj.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    private void SpawnPrototypeCar()
    {
        GameObject carRoot = new GameObject("Prototype_Racer");
        carRoot.transform.position = transform.position + new Vector3(0f, 0f, 2f);

        Material bodyMat = CreateColorMaterial("Body_Blue", new Color(0.1f, 0.35f, 0.85f));
        Material glassMat = CreateColorMaterial("Cabin_Dark", new Color(0.1f, 0.1f, 0.12f));
        Material tireMat = CreateColorMaterial("Tire_Black", new Color(0.15f, 0.15f, 0.15f));

        GameObject chassis = new GameObject("Chassis");
        chassis.transform.SetParent(carRoot.transform, false);
        MeshFilter mf = chassis.AddComponent<MeshFilter>();
        MeshRenderer mr = chassis.AddComponent<MeshRenderer>();
        mr.sharedMaterial = bodyMat;

        Mesh bodyMesh = new Mesh { name = "Wedge_Chassis" };
        bodyMesh.vertices = new Vector3[] {
            new Vector3(-0.9f, 0.25f, -2.0f), new Vector3(0.9f, 0.25f, -2.0f),
            new Vector3(-0.85f, 0.75f, -2.0f), new Vector3(0.85f, 0.75f, -2.0f),
            new Vector3(-0.9f, 0.20f,  2.0f), new Vector3(0.9f, 0.20f,  2.0f),
            new Vector3(-0.80f, 0.48f,  2.0f), new Vector3(0.80f, 0.48f,  2.0f)
        };
        bodyMesh.triangles = new int[] {
            2, 3, 1, 2, 1, 0,
            6, 4, 5, 6, 5, 7,
            6, 7, 3, 6, 3, 2,
            0, 1, 5, 0, 5, 4,
            2, 0, 4, 2, 4, 6,
            7, 5, 1, 7, 1, 3
        };
        bodyMesh.RecalculateNormals();
        mf.sharedMesh = bodyMesh;

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cockpit";
        cabin.transform.SetParent(carRoot.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 0.85f, -0.3f);
        cabin.transform.localScale = new Vector3(1.3f, 0.45f, 1.6f);
        cabin.GetComponent<MeshRenderer>().sharedMaterial = glassMat;

        Vector3[] wheelOffsets = {
            new Vector3(-0.95f, 0.35f,  1.3f), new Vector3(0.95f, 0.35f,  1.3f),
            new Vector3(-0.95f, 0.35f, -1.3f), new Vector3(0.95f, 0.35f, -1.3f)
        };
        string[] wheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };

        for (int i = 0; i < 4; i++)
        {
            GameObject wheelPivot = new GameObject(wheelNames[i]);
            wheelPivot.transform.SetParent(carRoot.transform, false);
            wheelPivot.transform.localPosition = wheelOffsets[i];

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Tire_Mesh";
            cylinder.transform.SetParent(wheelPivot.transform, false);
            cylinder.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            cylinder.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = tireMat;
        }
    }

    private Material CreateColorMaterial(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("HDRP/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color };
        return mat;
    }
}