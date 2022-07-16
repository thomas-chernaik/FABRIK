using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class FABRIKCLEAN : MonoBehaviour
{
    //number of segments
    public int numSegments;
    //lengths of segments
    float[] lengths;
    //the transform of the goal of the IK
    public Transform goalT;
    Vector3 goal;
    //the transform of the root bone of the chain, further bones in the chain must be the only child of the previous bone
    public Transform root;
    //how close then end will get to the goal before we decide its good enough
    public float tolerance;
    //the maximum number of iterations of the FABRIK we will attempt
    public float maxIters;
    //the positions of the ends of the bones
    Vector3[] prevPositions;
    Vector3[] nextPositions;
    // Start is called before the first frame update
    void Start()
    { 
        LengthsCalculation();
        prevPositions = new Vector3[lengths.Length + 1];
        nextPositions = new Vector3[lengths.Length + 1];
    }

    void LengthsCalculation()
    {
        lengths = new float[numSegments];
        Transform node = root;
        //iterate through the bones and calculate the lengths
        for(int i=0; i<numSegments; i++)
        {
            lengths[i] = Vector3.Distance(node.position, node.GetChild(0).position);
            print(lengths[i]);
            node = node.GetChild(0);
        }
    }

    //a function which will give us a point length away from p1 in the direction of p1 to p2
    Vector3 PointsAndLengthToNewPoint(Vector3 P1, Vector3 P2, float length)
    {
        //get the direction
        Vector3 direction = (P2 - P1).normalized;
        //return the point length away from p1 in direction direction
        return P1 + (direction * length);
    }

    //the F of FABRIK
    void Forwards()
    {
        //the first point has to be the start
        nextPositions[0] = root.position;
        //Go through each point after and set with PointsAndLengthToNewPoint
        for (int i = 1; i < lengths.Length + 1; i++)
        {
            nextPositions[i] = PointsAndLengthToNewPoint(nextPositions[i - 1], prevPositions[i], lengths[i - 1]);
        }
        //copy nextpositions to previous
        nextPositions.CopyTo(prevPositions, 0);
    }

    //the B of FABRIK
    void Backwards()
    {
        //the last point has to be the goal
        nextPositions[lengths.Length] = goal;
        //go through each point previous and set
        for (int i = lengths.Length - 1; i >= 0; i--)
        {
            nextPositions[i] = PointsAndLengthToNewPoint(nextPositions[i + 1], prevPositions[i], lengths[i]);
        }
        //copy nextpositions to previous
        nextPositions.CopyTo(prevPositions, 0);
    }

    //check if we are within our tolerance
    bool IsCloseToGoal()
    {
        return Vector3.Distance(prevPositions[lengths.Length], goal) < tolerance;
    }

    //check its possible to get to the goal
    bool CheckGoalInRange()
    {
        return lengths.Sum() > Vector3.Distance(goal, root.position);
    }

    //a method which performs the FABRIK if the goal is in range
    void SetForInRange()
    {
        int count = 0;
        while (count++ < maxIters && !IsCloseToGoal())
        {
            Backwards();
            Forwards();
        }
    }

    //a method which performs the FABRIK if the goal is not in range
    void SetForNotInRange()
    {
        prevPositions[0] = root.position;

        for (int i = 1; i < lengths.Length + 1; i++)
        {
            prevPositions[i] = root.position + (goal - root.position).normalized * lengths[i - 1];
        }

    }


    //a method which takes a direction vector and turns it into x and y euler angles
    Quaternion GetAngleFromDirection(Vector3 a)
    {
        return Quaternion.Euler(Mathf.Atan2(Mathf.Sqrt(a.x*a.x + a.z*a.z), a.y) / (2 * Mathf.PI) * 360, Mathf.Atan2(a.x, a.z) / (2 * Mathf.PI) * 360, 0);
    }

    //position the segments
    void UpdatePosition()
    {
        Transform segment = root;
        for (int i = 0; i < lengths.Length; i++)
        {

            //get the view direction vector
            Vector3 viewDirection = (prevPositions[i + 1] - prevPositions[i]);
            //set the segment rotation
            segment.rotation = GetAngleFromDirection(viewDirection);
            //update the segment to the next in the chain(the child
            segment = segment.GetChild(0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //set the goal
        goal = goalT.position;
        //check if the goal is in range or not and perform the correct setter
        if (!CheckGoalInRange())
        {
            SetForNotInRange();
        }
        else
        {
            SetForInRange();
        }
        UpdatePosition();
    }
}
