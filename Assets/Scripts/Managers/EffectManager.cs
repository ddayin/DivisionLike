﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance = null;
    public GameObject[] particlePrefabs = new GameObject[ 8 ];
    public Transform particleParent;

    private Dictionary<int, ParticleSystem> effectDic = new Dictionary<int, ParticleSystem>();

    private void Awake()
    {
        instance = this;
    }

    public void CreateParticle( int index, Vector3 position )
    {
        GameObject obj = Instantiate( particlePrefabs[ index ], position, Quaternion.identity, particleParent );
        ParticleSystem particle = obj.transform.Find( "Particle System" ).GetComponent<ParticleSystem>();

        effectDic.Add( index, particle );
    }

    public ParticleSystem GetParticle( int index )
    {
        return effectDic[ index ];
    }
}
