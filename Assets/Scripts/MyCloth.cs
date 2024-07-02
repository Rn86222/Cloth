using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class MyCloth : MonoBehaviour
{
    private const int X = 64;
    private const int Y = 64;

    private Vector3[] vertices = new Vector3[Y * X];
    private int[] triangles = new int[(Y - 1) * (X - 1) * 2 * 3];
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    private Particle[] particles = new Particle[Y * X];
    private List<DistanceConstraint> distanceConstraints = new List<DistanceConstraint>();
    
    private float dragCoef = 0.01f;
    private float liftCoef = 0.01f;
    private float rho = 1.225f;

    [SerializeField] private int iteration = 30;

    [SerializeField] private SliderController windVelocitySliderObserver;
    [SerializeField] private SliderController dragCoefSliderObserver;
    [SerializeField] private SliderController liftCoefSliderObserver;
    [SerializeField] private SliderController rhoSliderObserver;
    [SerializeField] private Shader shader;

    private Vector3 gravity = new(0, -9.8f, 0);
    private Vector3 windVelocity = new(0, 0, 0);

    void Start()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = new(shader)
        {
            color = new Color(0.6f, 0.4f, 0.1f, 1.0f)
        };

        InitVertices();
        mesh.SetVertices(vertices);
        InitIndices();
        mesh.SetTriangles(triangles, 0);
    }

    void Update()
    {
        windVelocity.z = -windVelocitySliderObserver.GetSliderValue();
        dragCoef = dragCoefSliderObserver.GetSliderValue();
        liftCoef = liftCoefSliderObserver.GetSliderValue();
        rho = rhoSliderObserver.GetSliderValue();
        SimulateOneStep();
        for (int y = 0; y < Y; y++)
        {
            for (int x = 0; x < X; x++)
            {
                vertices[y * X + x] = particles[y * X + x].position;
            }
        }
        mesh.SetVertices(vertices);

        mesh.RecalculateNormals();

        Render();
    }

    private void SimulateOneStep()
    {
        // gravity
        foreach (var p in particles)
        {
            var force = new Vector3(0, 0, 0);
            if (p.mass != Mathf.Infinity)
            {
                force += gravity * p.mass;
            }
            
            p.UpdateVelocityTemp(force, Time.deltaTime);
        }

        // wind
        for (int y = 0; y < Y - 1; y++)
        {
            for (int x = 0; x < X - 1; x++)
            {
                var idx0 = y * X + x;
                var idx1 = (y + 1) * X + x;
                var idx2 = y * X + x + 1;
                var forces1 = CalcWindForce(idx0, idx1, idx2);
                particles[idx0].UpdateVelocityTemp(forces1[0], Time.deltaTime);
                particles[idx1].UpdateVelocityTemp(forces1[1], Time.deltaTime);
                particles[idx2].UpdateVelocityTemp(forces1[2], Time.deltaTime);

                idx0 = y * X + x + 1;
                idx1 = (y + 1) * X + x;
                idx2 = (y + 1) * X + x + 1;
                var forces2 = CalcWindForce(idx0, idx1, idx2);
                particles[idx0].UpdateVelocityTemp(forces2[0], Time.deltaTime);
                particles[idx1].UpdateVelocityTemp(forces2[1], Time.deltaTime);
                particles[idx2].UpdateVelocityTemp(forces2[2], Time.deltaTime);
            }
        }

        foreach (var p in particles)
        {
            p.velocity = p.velocityTemp;
            p.positionTemp = p.position + p.velocity * Time.deltaTime;
        }

        for (int i = 0; i < iteration; i++)
        {
            foreach (var dc in distanceConstraints)
            {
                dc.project.Invoke();
            }
        }

        foreach (var p in particles)
        {
            p.velocity = (p.positionTemp - p.position) / Time.deltaTime;
            p.velocityTemp = p.velocity;
            p.position = p.positionTemp;
        }
    }

    // Referred to https://dl.acm.org/doi/10.1145/2614106.2614120
    // and https://github.com/yuki-koyama/elasty/blob/b959790659968d3793f78c882bb39619c81886ce/src/cloth-sim-object.cpp#L248
    private Vector3[] CalcWindForce(int idx0, int idx1, int idx2)
    {
        var massSum = particles[idx0].mass + particles[idx1].mass + particles[idx2].mass;
        if (massSum == Mathf.Infinity)
        {
            return new Vector3[] {Vector3.zero, Vector3.zero, Vector3.zero};
        }
        var vTriangle = (particles[idx0].velocityTemp * particles[idx0].mass + particles[idx1].velocityTemp * particles[idx1].mass + particles[idx2].velocityTemp * particles[idx2].mass) / massSum;

        var vRel = vTriangle - windVelocity;
        var vRelSquared = vRel.sqrMagnitude;

        var cross = Vector3.Cross(particles[idx1].position - particles[idx0].position, particles[idx2].position - particles[idx0].position);
        var area = cross.magnitude / 2.0f;
        var nEitherSide = cross.normalized;
        var n = Vector3.Dot(vRel, nEitherSide) > 0 ? nEitherSide : -nEitherSide;

        var coef = 0.5f * rho * area;
        var force = -coef * ((dragCoef - liftCoef) * Vector3.Dot(vRel, n) * vRel + liftCoef * vRelSquared * n);

        return new Vector3[] {force * particles[idx0].mass / massSum, force * particles[idx1].mass / massSum, force * particles[idx2].mass / massSum};
    }

    private void InitVertices()
    {
        for (int y = 0; y < Y; y++)
        {
            for (int x = 0; x < X; x++)
            {
                float z = 0;
                vertices[y * X + x].Set(x, y, z);
                if (y == Y - 1 && (x == 0 || x  == X - 1))
                {
                    particles[y * X + x] = new Particle(vertices[y * X + x], Mathf.Infinity);
                }
                else
                {
                    particles[y * X + x] = new Particle(vertices[y * X + x], 0.1f);
                }
            }
        }

        for (int y = 0; y < Y; y++)
        {
            for (int x = 0; x < X; x++)
            {
                if (x < X - 1)
                {
                    distanceConstraints.Add(new DistanceConstraint(particles[y * X + x], particles[y * X + x + 1], 1.0f));
                }
                if (y < Y - 1)
                {
                    distanceConstraints.Add(new DistanceConstraint(particles[y * X + x], particles[(y + 1) * X + x], 1.0f));
                }
                if (x < X - 1 && y < Y - 1)
                {
                    distanceConstraints.Add(new DistanceConstraint(particles[y * X + x], particles[(y + 1) * X + x + 1], 1.0f));
                }
            }
        }
    }

    private void InitIndices()
    {
        for (int y = 0; y < Y - 1; y++)
        {
            for (int x = 0; x < X - 1; x++)
            {
                triangles[(y * (X - 1) + x) * 6] = y * X + x;
                triangles[(y * (X - 1) + x) * 6 + 1] = (y + 1) * X + x;
                triangles[(y * (X - 1) + x) * 6 + 2] = y * X + x + 1;
                triangles[(y * (X - 1) + x) * 6 + 3] = y * X + x + 1;
                triangles[(y * (X - 1) + x) * 6 + 4] = (y + 1) * X + x;
                triangles[(y * (X - 1) + x) * 6 + 5] = (y + 1) * X  + x + 1;
            }
        }
    }

    private void Render()
    {
        meshFilter.mesh = mesh;
    }
}
