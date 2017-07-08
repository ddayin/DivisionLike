﻿/*
 * reference - https://www.youtube.com/watch?v=gN1BmP4ONSQ&list=PLfxIz_UlKk7IwrcF2zHixNtFmh0lznXow&t=4140s&index=4
 */

using UnityEngine;
using System.Collections;

namespace DivisionLike
{
    [RequireComponent( typeof( Collider ) )]
    [RequireComponent( typeof( Rigidbody ) )]
    public class Weapon : MonoBehaviour
    {
        private Collider col;
        private Rigidbody rigidBody;
        private Animator animator;
        private SoundController sc;

        public Light faceLight;
        private float effectsDisplayTime = 0.2f;                // The proportion of the timeBetweenBullets that the effects will display for.
        private float timer = 0f;

        public enum WeaponType
        {
            Primary = 0,
            Secondary,
            Sidearm
        }
        public WeaponType weaponType;

        public class WeaponName
        {
            public string makarov = "Makarov";
            public string ModernRussianAR = "Modern Russian AR";
            public string M4A1 = "M4A1";
        
        }
        public string weaponName;

        [System.Serializable]
        public class UserSettings
        {
            public Transform leftHandIKTarget;
            public Vector3 spineRotation;
        }
        [SerializeField]
        public UserSettings userSettings;

        [System.Serializable]
        public class WeaponSettings
        {
            [Header( "-Bullet Options-" )]
            public Transform bulletSpawn;
            public float damage = 40.0f;
            public float bulletSpread = 5.0f;
            public float fireRate = 0.2f;
            public LayerMask bulletLayers;
            public float range = 200.0f;

            [Header( "-Effects-" )]
            public GameObject muzzleFlash;
            public GameObject decal;
            public GameObject shell;
            public GameObject clip;

            [Header( "-Other-" )]
            //public GameObject crosshairPrefab;
            public float reloadDuration = 2.0f;
            public Transform shellEjectSpot;
            public float shellEjectSpeed = 7.5f;
            public Transform clipEjectPos;
            public GameObject clipGO;

            [Header( "-Positioning-" )]
            public Vector3 equipPosition;
            public Vector3 equipRotation;
            public Vector3 unequipPosition;
            public Vector3 unequipRotation;

            [Header( "-Animation-" )]
            public bool useAnimation;
            public int fireAnimationLayer = 0;
            public string fireAnimationName = "Fire";
        }
        [SerializeField]
        public WeaponSettings weaponSettings;

        [System.Serializable]
        public class Ammunition
        {
            public int carryingAmmo;    // 장전되어 있는 총알의 개수
            public int clipAmmo;        // 모든 총알의 개수
            public int maxClipAmmo;     // 장전될 수 있는 총알의 최대 개수
        }
        [SerializeField]
        public Ammunition ammo;

        private WeaponHandler owner;
        private bool isEquipped;
        private bool resettingCartridge;

        [System.Serializable]
        public class SoundSettings
        {
            public AudioClip[] gunshotSounds;
            public AudioClip reloadSound;
            [Range( 0, 3 )] public float pitchMin = 1;
            [Range( 0, 3 )] public float pitchMax = 1.2f;
            public AudioSource audioS;
        }
        [SerializeField]
        public SoundSettings soundSettings;

        // Use this for initialization
        void Start()
        {
            GameObject check = GameObject.FindGameObjectWithTag( "Sound Controller" );

            if ( check != null )
            {
                sc = check.GetComponent<SoundController>();
            }

            col = GetComponent<Collider>();
            rigidBody = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();

        }

        // Update is called once per frame
        void Update()
        {
            timer += Time.deltaTime;

            if ( owner )
            {
                DisableEnableComponents( false );
                if ( isEquipped )
                {
                    if ( owner.userSettings.rightHand )
                    {
                        Equip();
                    }
                }
                else
                {
                    if ( weaponSettings.bulletSpawn.childCount > 0 )
                    {
                        foreach ( Transform t in weaponSettings.bulletSpawn.GetComponentsInChildren<Transform>() )
                        {
                            if ( t != weaponSettings.bulletSpawn )
                            {
                                Destroy( t.gameObject );
                            }
                        }
                    }
                    Unequip( weaponType );
                }
            }
            else
            { // If owner is null
                DisableEnableComponents( true );
                transform.SetParent( null );
            }

            // 총알 빛 효과 일정 시간 지나면 끈다.
            if ( timer >= effectsDisplayTime )
            {
                faceLight.enabled = false;
            }
        }

        private BoxCollider boxCollider;

        // MeshRenderer의 크기에 맞게 BoxCollider의 크기도 조정한다.
        private void FitBoxColliderSize()
        {
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            boxCollider.center = renderer.bounds.center;
            boxCollider.size = renderer.bounds.size;
        }

        //This fires the weapon
        public void Fire( Ray ray )
        {
            if ( ammo.clipAmmo <= 0 || resettingCartridge || !weaponSettings.bulletSpawn || !isEquipped )
                return;

            timer = 0f;

            faceLight.enabled = true;

            RaycastHit hit;
            RaycastHit ragdollHit;
            Transform bSpawn = weaponSettings.bulletSpawn;
            Vector3 bSpawnPoint = bSpawn.position;
            Vector3 dir = ray.GetPoint( weaponSettings.range ) - bSpawnPoint;

            dir += (Vector3) Random.insideUnitCircle * weaponSettings.bulletSpread;

            // 적 캐릭터를 맞추었을 때
            // layer mask 13 이 Ragdoll 이고 적 캐릭터를 의미함
            int layerMask = LayerMask.GetMask( "Ragdoll" );
            if ( Physics.Raycast( bSpawnPoint, dir, out ragdollHit, weaponSettings.range, layerMask ) )
            {
                // Try and find an EnemyHealth script on the gameobject hit.
                EnemyHealth enemyHealth = ragdollHit.collider.GetComponent<EnemyHealth>();

                // If the EnemyHealth component exist...
                if ( enemyHealth != null )
                {
                    // ... the enemy should take damage.
                    enemyHealth.TakeDamage( (int) weaponSettings.damage, ragdollHit.point );
                }
            }

            // 적 캐릭터 제외
            if ( Physics.Raycast( bSpawnPoint, dir, out hit, weaponSettings.range, weaponSettings.bulletLayers ) )
            {
                HitEffects( hit );
            }

            GunEffects();

            if ( weaponSettings.useAnimation )
                animator.Play( weaponSettings.fireAnimationName, weaponSettings.fireAnimationLayer );

            ammo.clipAmmo--;
            resettingCartridge = true;
            StartCoroutine( LoadNextBullet() );
        }

        //Loads the next bullet into the chamber
        IEnumerator LoadNextBullet()
        {
            yield return new WaitForSeconds( weaponSettings.fireRate );
            resettingCartridge = false;
        }

        void HitEffects( RaycastHit hit )
        {
            if ( hit.collider.gameObject.isStatic )
            {
                if ( weaponSettings.decal )
                {
                    Vector3 hitPoint = hit.point;
                    Quaternion lookRotation = Quaternion.LookRotation( hit.normal );
                    GameObject decal = Instantiate( weaponSettings.decal, hitPoint, lookRotation ) as GameObject;
                    Transform decalT = decal.transform;
                    Transform hitT = hit.transform;
                    decalT.SetParent( hitT );
                    Destroy( decal, Random.Range( 15.0f, 20.0f ) );
                }
            }
        }

        void GunEffects()
        {
            if ( weaponSettings.muzzleFlash )
            {
                Vector3 bulletSpawnPos = weaponSettings.bulletSpawn.position;
                GameObject muzzleFlash = Instantiate( weaponSettings.muzzleFlash, bulletSpawnPos, Quaternion.identity ) as GameObject;
                Transform muzzleT = muzzleFlash.transform;
                muzzleT.SetParent( weaponSettings.bulletSpawn );
                Destroy( muzzleFlash, 2.0f );
            }

            if ( weaponSettings.shell )
            {
                if ( weaponSettings.shellEjectSpot )
                {
                    Vector3 shellEjectPos = weaponSettings.shellEjectSpot.position;
                    Quaternion shellEjectRot = weaponSettings.shellEjectSpot.rotation;
                    GameObject shell = Instantiate( weaponSettings.shell, shellEjectPos, shellEjectRot ) as GameObject;

                    if ( shell.GetComponent<Rigidbody>() )
                    {
                        Rigidbody rigidB = shell.GetComponent<Rigidbody>();
                        rigidB.AddForce( weaponSettings.shellEjectSpot.forward * weaponSettings.shellEjectSpeed, ForceMode.Impulse );
                    }

                    Destroy( shell, Random.Range( 15.0f, 20.0f ) );
                }
            }

            PlayGunshotSound();
        }

        void PlayGunshotSound()
        {
            if ( sc == null )
            {
                return;
            }

            if ( soundSettings.audioS != null )
            {
                if ( soundSettings.gunshotSounds.Length > 0 )
                {
                    sc.InstantiateClip(
                        weaponSettings.bulletSpawn.position, // Where we want to play the sound from
                        soundSettings.gunshotSounds[ Random.Range( 0, soundSettings.gunshotSounds.Length ) ],  // What audio clip we will use for this sound
                        2, // How long before we destroy the audio
                        true, // Do we want to randomize the sound?
                        soundSettings.pitchMin, // The minimum pitch that the sound will use.
                        soundSettings.pitchMax ); // The maximum pitch that the sound will use.
                }
            }
        }

        //Disables or enables collider and rigidbody
        void DisableEnableComponents( bool enabled )
        {
            if ( !enabled )
            {
                rigidBody.isKinematic = true;
                col.enabled = false;
            }
            else
            {
                rigidBody.isKinematic = false;
                col.enabled = true;
            }
        }

        //Equips this weapon to the hand
        void Equip()
        {
            if ( owner == null )
                return;
            else if ( owner.userSettings.rightHand == false )
                return;

            transform.SetParent( owner.userSettings.rightHand );
            transform.localPosition = weaponSettings.equipPosition;
            Quaternion equipRot = Quaternion.Euler( weaponSettings.equipRotation );
            transform.localRotation = equipRot;
        }

        //Unequips the weapon and places it to the desired location
        void Unequip( WeaponType wpType )
        {
            if ( owner == null )
                return;

            switch ( wpType )
            {
                case WeaponType.Primary:
                    transform.SetParent( owner.userSettings.rifleUnequipSpot );
                    break;
                case WeaponType.Secondary:
                    transform.SetParent( owner.userSettings.pistolUnequipSpot );
                    break;
                case WeaponType.Sidearm:
                    break;
                default:
                    break;
            }
            transform.localPosition = weaponSettings.unequipPosition;
            Quaternion unEquipRot = Quaternion.Euler( weaponSettings.unequipRotation );
            transform.localRotation = unEquipRot;
        }

        //Loads the clip and calculates the ammo
        public void LoadClip()
        {
            int ammoNeeded = ammo.maxClipAmmo - ammo.clipAmmo;

            if ( ammoNeeded >= ammo.carryingAmmo )
            {
                ammo.clipAmmo = ammo.carryingAmmo;
                ammo.carryingAmmo = 0;
            }
            else
            {
                ammo.carryingAmmo -= ammoNeeded;
                ammo.clipAmmo = ammo.maxClipAmmo;
            }
        }

        //Sets the weapons equip state
        public void SetEquipped( bool equip )
        {
            isEquipped = equip;
        }

        //Sets the owner of this weapon
        public void SetOwner( WeaponHandler wp )
        {
            owner = wp;
        }
    }
}