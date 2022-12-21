using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Instancer : MonoBehaviour
{
    public int Instances;
    public Mesh mesh;
    public Material[] Materials;
    private List<List<Matrix4x4>> Batches = new List<List<Matrix4x4>>();

    private void RenderBatches()
    {
        foreach (var Batch in Batches)
        {
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Graphics.DrawMeshInstanced(mesh, i, Materials[i], Batch);
            }
        }
    }
    private void Update()
    {
        RenderBatches();
    }

    private void Start()
    {
        int AddedMatrices = 0;

        Batches.Add(item: new List<Matrix4x4>());


        for (int i = 0; i < Instances; i++)
        {
            if(AddedMatrices<1000)
            {
                Batches[Batches.Count - 1].Add(item:Matrix4x4.TRS(pos:new Vector3(Random.Range(0,50), Random.Range(0, 50), Random.Range(0, 50)), Random.rotation, s: new Vector3()));
                AddedMatrices += 1;
            }
            else
            {
                Batches.Add(item: new List<Matrix4x4>());
                AddedMatrices = 0;
            }
        }
    }

}
