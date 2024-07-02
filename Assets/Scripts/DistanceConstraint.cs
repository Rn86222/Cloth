using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DistanceConstraint
{
    private Particle p1;
    private Particle p2;
    private float restLength;
    private float stiffness;
    public Action project;
    public DistanceConstraint(Particle p1, Particle p2, float stiffness)
    {
        this.p1 = p1;
        this.p2 = p2;
        restLength = (p1.position - p2.position).magnitude;
        this.stiffness = stiffness;
        project = () =>
        {
            var w1 = 1 / this.p1.mass;
            var w2 = 1 / this.p2.mass;

            var p2_to_p1 = p1.positionTemp - p2.positionTemp;
            var dist = p2_to_p1.magnitude;
            var p1_coef = -(w1 / (w1 + w2)) * ((dist - restLength) / dist) * this.stiffness;
            var p2_coef = +(w2 / (w1 + w2)) * ((dist - restLength) / dist) * this.stiffness;

            p1.positionTemp += p2_to_p1 * p1_coef;
            p2.positionTemp += p2_to_p1 * p2_coef;
        };
    }
}
