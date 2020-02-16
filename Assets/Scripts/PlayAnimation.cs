using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UTJ.Alembic;

public class PlayAnimation : MonoBehaviour
{

    public AlembicStreamPlayer player;

    public float playSpeed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        player.currentTime = Mathf.Repeat(playSpeed * Time.time, (float)player.duration );
    }
}
