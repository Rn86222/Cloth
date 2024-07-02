using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle
{
    public float mass;
    public Vector3 position;
    public Vector3 positionTemp;
    public Vector3 velocity;
    public Vector3 velocityTemp;

    public Particle(Vector3 position, float mass)
    {
        this.position = position;
        this.mass = mass;
        positionTemp = position;
        velocity = new Vector3(0, 0, 0);
        velocityTemp = new Vector3(0, 0, 0);
    }

    public void UpdateVelocityTemp(Vector3 force, float dt)
    {
        velocityTemp += force / mass * dt;
    }
}
