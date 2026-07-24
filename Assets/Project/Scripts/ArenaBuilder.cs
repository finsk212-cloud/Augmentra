using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ArenaBuilder : MonoBehaviour
{
    private const float ArenaSize = 40f;
    private const float HalfSize = 20f;
    private const float WallHeight = 4f;
    private const float WallThickness = 1f;

    private int arenaLayer;
    private Material floorMaterial;
    private Material wallMaterial;
    private Material pillarMaterial;

    private readonly List<Vector3> pillarPositions = new List<Vector3>();

    private void Awake()
    {
        arenaLayer = LayerMask.NameToLayer("Arena");

        CreateMaterials();
        DestroyDefaultDirectionalLights();
        BuildFloor();
        BuildWalls();
        BuildPillars();
        BuildLighting();
    }

    private void CreateMaterials()
    {
        floorMaterial = MakeMaterial(ParseColor("#7A7A7A"), 0.15f, 0f);
        wallMaterial = MakeMaterial(ParseColor("#6E6E6E"), 0.1f, 0f);
        pillarMaterial = MakeMaterial(ParseColor("#8A8076"), 0.1f, 0f);
    }

    private void BuildFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(transform, false);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(ArenaSize / 10f, 1f, ArenaSize / 10f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
        SetLayer(floor);
    }

    private void BuildWalls()
    {
        float length = ArenaSize + WallThickness * 2f;
        float offset = HalfSize + WallThickness * 0.5f;
        float y = WallHeight * 0.5f;

        CreateWall("WallNorth", new Vector3(0f, y, offset), new Vector3(length, WallHeight, WallThickness));
        CreateWall("WallSouth", new Vector3(0f, y, -offset), new Vector3(length, WallHeight, WallThickness));
        CreateWall("WallEast", new Vector3(offset, y, 0f), new Vector3(WallThickness, WallHeight, ArenaSize));
        CreateWall("WallWest", new Vector3(-offset, y, 0f), new Vector3(WallThickness, WallHeight, ArenaSize));
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(transform, false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = wallMaterial;
        SetLayer(wall);
    }

    private void BuildPillars()
    {
        int placed = 0;
        int attempts = 0;

        while (placed < 8 && attempts < 300)
        {
            attempts++;

            float x = Random.Range(-(HalfSize - 3f), HalfSize - 3f);
            float z = Random.Range(-(HalfSize - 3f), HalfSize - 3f);
            Vector3 ground = new Vector3(x, 0f, z);

            if (ground.magnitude < 5f)
            {
                continue;
            }

            if (IsNearPillar(ground, 3f))
            {
                continue;
            }

            float height = Random.Range(0.8f, 1.6f);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Pillar";
            pillar.transform.SetParent(transform, false);
            pillar.transform.position = new Vector3(x, height * 0.5f, z);
            pillar.transform.localScale = new Vector3(1.2f, height * 0.5f, 1.2f);
            pillar.GetComponent<Renderer>().sharedMaterial = pillarMaterial;
            SetLayer(pillar);

            NavMeshObstacle obstacle = pillar.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.center = Vector3.zero;
            obstacle.radius = 0.5f;
            obstacle.height = 2f;
            obstacle.carving = true;

            pillarPositions.Add(ground);
            placed++;
        }
    }

    private void BuildLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

        GameObject sunObject = new GameObject("SunLight");
        sunObject.transform.SetParent(transform, false);
        sunObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = ParseColor("#FFF6E6");
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
    }

    private void DestroyDefaultDirectionalLights()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);

        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                Destroy(light.gameObject);
            }
        }
    }

    private bool IsNearPillar(Vector3 position, float minDistance)
    {
        foreach (Vector3 existing in pillarPositions)
        {
            if (Vector3.Distance(existing, position) < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void SetLayer(GameObject go)
    {
        if (arenaLayer >= 0)
        {
            go.layer = arenaLayer;
        }
    }

    private Material MakeMaterial(Color color, float smoothness, float metallic)
    {
        Material material = new Material(GetLitShader());
        material.color = color;

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        return material;
    }

    private Shader GetLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return shader;
    }

    private Color ParseColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
