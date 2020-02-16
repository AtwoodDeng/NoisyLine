using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Obi;
using TMPro;
using UnityEngine;
using Random = System.Random;

public class SweepMesh : MonoBehaviour
{
    [Header("=====Input=====")]
    public int trailLength = 20;
    public int particleCount = 200;
    public int PolyDivision = 6;
    public float trailRadius = 0.05f;
    public ObiSolver solver;
    public int capDivision = 3;
    public float capLength = 0.05f;
    //public AnimationCurve trailCurve;
    public AnimationCurve capCurve;
    public float simRate = 1.0f;
    public int RenderDelay = 5;

    [Header("=====Inner=====")]
    private Mesh mesh;

    // verticesPos structure :
    // |==================== Per Particle =====================|
    // |==First Trail Div(PolyDivision)===|...|==Last Trail Div(PolyDivision)===|
    // |<-----                      (trailLength)                    ---------->|
    //
    // |==First Tail Cap Div(PolyDivision)  ===|...|==Last Tail Cap Div(PolyDivision) ===|== Last Tail Cap Top Position
    // |<-----                               (CapDivision)                    ---------->|
    //
    // |==First Head Cap Div(PolyDivision)  ===|...|==Last Head Cap Div(PolyDivision) ===|== Last Head Cap Top Position
    // |<-----                               (CapDivision)                    ---------->|
    //
    private Vector3[] verticesPos;
    private Color[] verticesColor;
    private List<Vector3[]> positions;
    private List<Vector3[]> velocities;
    [Header("=====Inner=====")]
    public int positionIndex=0;

    private float[,] slicePositions;

    public bool needUpdate;
    private int renderDelayCounter = 0;

    Color GetComposedColor(Color col, float hueOffset , float value , float saturation )
    {
        float h, s, v;
        Color.RGBToHSV(col , out h , out s , out v );

        h = Mathf.Repeat(h + hueOffset, 1f);
        s = saturation;
        v = value;

        return Color.HSVToRGB(h, s, v);
    }

    Color ConvertToVertexColor(float trailRatio, float time , Vector3 veolcity)
    {
        return new Color(trailRatio , Mathf.Repeat(time * 0.1f, 1f ) , veolcity.magnitude , 0 );
    }

    void GetAxisFromPositions(Vector3 thisPos, Vector3 lastPos, out Vector3 forward, out Vector3 up, out Vector3 side)
    {

        var p_diff = thisPos - lastPos;
        var p_pos_length = p_diff.magnitude;

        forward = p_pos_length > 0
            ? p_diff / p_pos_length
            : new Vector3(0, 1f, 0);
        up = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 0, 1f)));

        side = Vector3.Cross(forward, up);

    }

    public void UpdateParticlePositions(int index)
    {
        for (int i = 0; i < particleCount; ++i)
        {
            positions[index][i] = solver.renderablePositions.GetVector3(i);
            velocities[index][i] = solver.velocities.GetVector3(i);

        }
    }


    public void InitMesh()
    {
        mesh = new Mesh();
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    public int GetVertexCountPerParticle()
    {
        return trailLength * PolyDivision + ( capDivision * PolyDivision + 1) *2 ;
    }

    void UpdateVertexColor(int positionIndex)
    {
        int index = 0;

        // float hueOffset = (Mathf.PerlinNoise(Time.time * 0.2f, Time.time * 0.3f) - 0.5f) * colorHueOffset;

        for (int i = 0; i < particleCount; ++i)
        {
            // deal with trail

            for (int t = 0; t < trailLength-1; ++t)
            {
                var trailRatio = t * 1.0f / (trailLength - 1);
                var lastFrameColor = verticesColor[index + PolyDivision];
                var color = new Color(trailRatio,lastFrameColor.g,lastFrameColor.b,0);
                for (int d = 0; d < PolyDivision; ++d)
                {
                    verticesColor[index++] = color;
                }
            }

            {
                for (int d = 0; d < PolyDivision; ++d)
                {
                    verticesColor[index++] = ConvertToVertexColor(1f, Time.time , velocities[positionIndex][i]);
                }
            }

            // deal with tail cap
            {
                var tailColor = verticesColor[GetVertexCountPerParticle() * i];
                for (int c = 0; c < capDivision; ++c)
                {
                    //var capRatio = (c + 1) * 1.0f / (capDivision + 1);
                    
                    for (int d = 0; d < PolyDivision; ++d)
                    {
                        verticesColor[index++] = tailColor;
                    }
                }

                verticesColor[index++] = tailColor;
            }

            {
                var headColor = ConvertToVertexColor(1f, Time.time, velocities[positionIndex][i]);
                // deal with head cap
                for (int c = 0; c < capDivision; ++c)
                {
                    //var capRatio = (c + 1) * 1.0f / (capDivision + 1);
                    
                    for (int d = 0; d < PolyDivision; ++d)
                    {
                        verticesColor[index++] = headColor;
                    }
                }


                verticesColor[index++] = headColor;
            }
        }

    }

    void UpdateVertices( int positionIndex )
    {
        for (int i = 0; i < particleCount; ++i)
        {
            {
                // copy the old position
                var indexOffset = GetVertexCountPerParticle() * i;
                for( int j = 0 ; j < trailLength -1 ; ++ j )
                {
                    var toIndex = indexOffset + j * PolyDivision;
                    var nextIndex = (j + 1) % trailLength;
                    var fromIndex = indexOffset + nextIndex * PolyDivision;

                    Array.Copy(verticesPos,fromIndex,verticesPos,toIndex,PolyDivision);

                }
            }

            {
                // deal with the new trail position
                var p_pos = positions[positionIndex][i];
                var p_last_pos = positions[(positionIndex + trailLength - 1) % trailLength][i];
                
                Vector3 forward, up, side;
                GetAxisFromPositions(p_pos, p_last_pos, out forward, out up, out side);

                var particleIndex = GetVertexCountPerParticle() * i + (trailLength - 1 ) * PolyDivision;

                for (int j = 0; j < PolyDivision; ++j)
                {
                    verticesPos[particleIndex++] =
                          p_pos + up * slicePositions[j, 0] * trailRadius + side * slicePositions[j, 1] * trailRadius;
                }
            }

            // deal with tail cap
            {
                var index = GetVertexCountPerParticle() * i + trailLength * PolyDivision ;

                var tail_pos = positions[(positionIndex + 2) % trailLength][i];
                var tail_last_pos = positions[(positionIndex + 1) % trailLength][i];

                Vector3 forward, up, side;
                GetAxisFromPositions( tail_pos, tail_last_pos, out forward, out up, out side);

                for (int j = 0; j < capDivision; ++j)
                {
                    var tailRatio = (j + 1 ) * 1.0f / (capDivision+1);
                    var radius = trailRadius * capCurve.Evaluate(tailRatio);
                    for (int k = 0; k < PolyDivision; ++k)
                    {
                        verticesPos[index++] = tail_pos - forward * capLength * tailRatio +
                                            up * slicePositions[k, 0] * radius + side * slicePositions[k, 1] * radius;
                    }
                }

                verticesPos[index++] = tail_pos - forward * capLength;
            }

            // deal with head cap
            {
                var index = GetVertexCountPerParticle() * i + trailLength * PolyDivision + capDivision * PolyDivision + 1;

                var head_pos = positions[positionIndex][i];
                var head_last_pos = positions[(positionIndex + trailLength - 1) % trailLength][i];

                Vector3 forward, up, side;
                GetAxisFromPositions(head_pos, head_last_pos, out forward, out up, out side);


                for (int j = 0; j < capDivision; ++j)
                {
                    var capRatio = (j + 1) * 1.0f / (capDivision+1);
                    var radius = trailRadius * capCurve.Evaluate(capRatio);
                    for (int k = 0; k < PolyDivision; ++k)
                    {
                        verticesPos[index++] = head_pos + forward * capLength * capRatio + 
                                                   up * slicePositions[k, 0] * radius + side * slicePositions[k, 1] * radius;
                    }
                }

                verticesPos[index++] = head_pos + forward * capLength;
            }


        }

    }

    int[] CreateIndexArray( )
    {
        var indexCountPerParticle = (trailLength - 1) * PolyDivision * 6 + ( capDivision * PolyDivision * 6 + PolyDivision * 3) * 2;
        var array = new int[indexCountPerParticle * particleCount];
        var index = 0;


        for (var k = 0; k < particleCount; k++)
        {
            // trail
            index = indexCountPerParticle * k;
            var particleVertexOffset = GetVertexCountPerParticle() * k;
            var trailVertexOffset = particleVertexOffset + trailLength * PolyDivision;
            var headVertexOffset = trailVertexOffset + capDivision * PolyDivision + 1;

            for (var i = 0; i < trailLength - 1; ++i)
            {
                for (var j = 0; j < PolyDivision; j++)
                {
                    var v00 = particleVertexOffset + i * PolyDivision + j; 
                    var v01 = particleVertexOffset + (i+1) * PolyDivision + j;
                    var v10 = particleVertexOffset + i * PolyDivision + (j+1) % PolyDivision;
                    var v11 = particleVertexOffset + (i + 1)*PolyDivision + (j+1) % PolyDivision;

                    array[index++] = v00;
                    array[index++] = v01;
                    array[index++] = v11;

                    array[index++] = v00;
                    array[index++] = v11;
                    array[index++] = v10;
                }
            }

            // tail cap
            for (var i = 0; i < capDivision; ++i)
            {
                for (var j = 0; j < PolyDivision; j++)
                {
                    var v00 = (((i==0)? particleVertexOffset : trailVertexOffset + (i-1) * PolyDivision)) + j ;
                    var v01 = trailVertexOffset + i * PolyDivision + j;
                    var v10 = (((i == 0) ? particleVertexOffset : trailVertexOffset + (i - 1) * PolyDivision)) + (j + 1) % PolyDivision;
                    var v11 = trailVertexOffset + i * PolyDivision + (j + 1) % PolyDivision;


                    array[index++] = v00;
                    array[index++] = v11;
                    array[index++] = v01;

                    array[index++] = v00;
                    array[index++] = v10;
                    array[index++] = v11;
                }
            }

            for (var j = 0; j < PolyDivision; j++)
            {
                var v0 = trailVertexOffset + (capDivision - 1) * PolyDivision + j;
                var v1 = trailVertexOffset + (capDivision - 1) * PolyDivision + (j + 1) % PolyDivision;
                var v2 = trailVertexOffset + capDivision * PolyDivision;

                array[index++] = v0;
                array[index++] = v1;
                array[index++] = v2;

            }

            // head cap
            for (var i = 0; i < capDivision; ++i)
            {
                for (var j = 0; j < PolyDivision; j++)
                {
                    var v00 = (((i == 0) ? particleVertexOffset + (trailLength-1) * PolyDivision : headVertexOffset + (i - 1) * PolyDivision)) + j;
                    var v01 = headVertexOffset + i * PolyDivision + j;
                    var v10 = (((i == 0) ? particleVertexOffset + (trailLength - 1) * PolyDivision : headVertexOffset + (i - 1) * PolyDivision)) + (j + 1) % PolyDivision;
                    var v11 = headVertexOffset + i * PolyDivision + (j + 1) % PolyDivision;


                    array[index++] = v00;
                    array[index++] = v01;
                    array[index++] = v11;

                    array[index++] = v00;
                    array[index++] = v11;
                    array[index++] = v10;
                }
            }

            for (var j = 0; j < PolyDivision; j++)
            {
                var v0 = headVertexOffset + (capDivision - 1) * PolyDivision + j;
                var v1 = headVertexOffset + (capDivision - 1) * PolyDivision + (j + 1) % PolyDivision;
                var v2 = headVertexOffset + capDivision * PolyDivision;

                array[index++] = v0;
                array[index++] = v2;
                array[index++] = v1;

            }
        }
        

        return array;
    }

    public void UpdateMesh()
    {
        if ((renderDelayCounter++) < RenderDelay)
            return;

        var vertexCount = GetVertexCountPerParticle() * particleCount;

        needUpdate = (verticesPos == null || vertexCount != verticesPos.Length);


        if (needUpdate)
        {
            verticesPos = new Vector3[vertexCount];
            verticesColor = new Color[vertexCount];

            positions = new List<Vector3[]>();
            velocities = new List<Vector3[]>();

            slicePositions = new float[PolyDivision,2];
            for (int i = 0; i < PolyDivision; ++i)
            {
                var angle = Mathf.PI * 2f / PolyDivision;
                slicePositions[i, 0] = Mathf.Sin(angle*i);
                slicePositions[i, 1] = Mathf.Cos(angle*i);
            }
            solver.renderablePositions.OnBeforeSerialize();

            for (int i = 0; i < trailLength; ++i)
            {
                positions.Add(new Vector3[particleCount]);
                velocities.Add(new Vector3[particleCount]);
                UpdateParticlePositions(i);
            }

            // init vertex
            for (int i = 0; i < trailLength; ++i)
            {
                UpdateVertices(i);
            }

            mesh.Clear();
        }

        UpdateParticlePositions(positionIndex);
        UpdateVertices(positionIndex);
        UpdateVertexColor(positionIndex);

        mesh.vertices = verticesPos;
        mesh.colors = verticesColor;

        if (needUpdate)
        {
            mesh.SetIndices( CreateIndexArray() , MeshTopology.Triangles , 0 );

        }

        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        positionIndex=(positionIndex+1) % trailLength;

    }

    void Start()
    {
        InitMesh();
        Time.timeScale = simRate;
    }

    void Update()
    {
        UpdateMesh();
    }

    public Vector3 ToV3(Vector4 v4)
    {
        return new Vector3(v4.x, v4.y, v4.z);
    }

    

    public void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.red;
        if ( positions != null && positions.Count > 0 )
        for (int i = 0; i < particleCount; ++i)
        {
            for (int j = 0; j < trailLength - 1; ++j)
            {
                var p1 = positions[j][i];
                var p2 = positions[j+1][i];
                Gizmos.DrawLine(p1,p2);
            }
        }

        Gizmos.color = Color.cyan;
        if (verticesPos != null && verticesPos.Length > 0)
        {
            for (int i = 0; i < particleCount; ++i)
            {
                for (int j = 0; j < trailLength - 1; ++j)
                {
                    for (int k = 0; k < PolyDivision; ++k)
                    {
                        var p1 = verticesPos[GetVertexCountPerParticle() * i + j * PolyDivision + k ];
                        var p2 = verticesPos[GetVertexCountPerParticle() * i + (j +1 ) * PolyDivision + k];
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }

            Gizmos.color = Color.magenta;
            for (int i = 0; i < particleCount; ++i)
            {
                for (int j = 0; j < capDivision; ++j)
                {
                    for (int k = 0; k < PolyDivision; ++k)
                    {
                        var p1 = verticesPos[GetVertexCountPerParticle() * i + trailLength * PolyDivision + j * PolyDivision + k];
                        var p2 = verticesPos[GetVertexCountPerParticle() * i + trailLength * PolyDivision + j * PolyDivision + (k+1)%PolyDivision];
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }

            Gizmos.color = Color.yellow; 
            for (int i = 0; i < particleCount; ++i)
            {
                for (int j = 0; j < capDivision; ++j)
                {
                    for (int k = 0; k < PolyDivision; ++k)
                    {
                        var p1 = verticesPos[GetVertexCountPerParticle() * i + trailLength * PolyDivision + capDivision * PolyDivision + 1 + j * PolyDivision + k];
                        var p2 = verticesPos[GetVertexCountPerParticle() * i + trailLength * PolyDivision + capDivision * PolyDivision + 1 + j * PolyDivision + (k + 1) % PolyDivision];
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }
        }
    }
}
