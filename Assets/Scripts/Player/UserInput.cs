﻿/*
 * reference - https://www.youtube.com/watch?v=gN1BmP4ONSQ&list=PLfxIz_UlKk7IwrcF2zHixNtFmh0lznXow&t=4140s&index=4
 */

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace DivisionLike
{
    public class UserInput : MonoBehaviour
    {
        private CharacterMovement m_CharacterMove;
        private WeaponHandler m_WeaponHandler;
        private PlayerInventory m_Inventory;
        private GrenadeHandler m_GrenadeHandler;

        [System.Serializable]
        public class InputSettings
        {
            public string m_VerticalAxis = "Vertical";
            public string m_HorizontalAxis = "Horizontal";
            public string m_JumpButton = "Jump";
            public string m_ReloadButton = "Reload";
            public string m_AimButton = "Fire2";
            public string m_FireButton = "Fire1";
            public string m_SwitchWeaponButton = "Fire3";
            public string m_PrimaryWeaponButton = "1";
            public string m_SecondaryWeaponButton = "2";
            public string m_SidearmButton = "3";
            public string m_SprintButton = "Sprint";
            public string m_MedikitButton = "Medikit";
            public string m_GrenadeButton = "Grenade";
        }
        [SerializeField]
        public InputSettings m_InputSettings;

        [System.Serializable]
        public class OtherSettings
        {
            public float m_LookSpeed = 5.0f;
            public float m_LookDistance = 30.0f;
            public bool m_RequireInputForTurn = true;
            public LayerMask m_AimDetectionLayers;
        }
        [SerializeField]
        public OtherSettings m_OtherSettings;

        private Camera m_TPSCamera;
        private Transform m_CenterTransform;

        public bool m_DebugAim = false;
        public Transform m_SpineTransform;
        public bool m_IsAiming = false;
        public bool m_IsFiring = false;
        public bool m_IsSprinting = false;
        public bool m_IsGrenadeMode = false;

        private Dictionary<Weapon, GameObject> m_CrosshairPrefabMap = new Dictionary<Weapon, GameObject>();

        // Use this for initialization
        void Start()
        {
            m_CharacterMove = transform.GetComponent<CharacterMovement>();
            m_WeaponHandler = transform.GetComponent<WeaponHandler>();
            m_Inventory = transform.GetComponent<PlayerInventory>();
            m_GrenadeHandler = transform.GetComponent<GrenadeHandler>();

            m_TPSCamera = Camera.main;
            

            SetupCrosshairs();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        

        // Update is called once per frame
        void Update()
        {
            CharacterLogic();
            CameraLookLogic();
            WeaponLogic();
            GrenadeLogic();
            InventoryLogic();
        }

        void LateUpdate()
        {
            if ( m_WeaponHandler )
            {
                if ( m_WeaponHandler.m_CurrentWeapon )
                {
                    if ( m_IsAiming == true )
                    {
                        PositionSpine();
                    }   
                }
            }
        }

        private float m_v = 0f;
        private float m_h = 0f;

        /// <summary>
        /// Handles character logic
        /// </summary>
        void CharacterLogic()
        {
            if ( !m_CharacterMove )
                return;

            m_v = Input.GetAxis( m_InputSettings.m_VerticalAxis );
            m_h = Input.GetAxis( m_InputSettings.m_HorizontalAxis );

            // always walk when player is aiming
            if ( Player.instance.m_UserInput.m_IsAiming == true )
            {
                m_CharacterMove.Animate( m_v * 0.5f, m_h * 0.5f );
            }
            else
            {
                // sprint
                if ( Input.GetButtonDown( m_InputSettings.m_SprintButton ) == true )
                {
                    m_IsSprinting = !m_IsSprinting;
                }

                if ( m_IsSprinting == true )
                {
                    m_CharacterMove.Animate( m_v, m_h );
                }
                else
                {
                    m_CharacterMove.Animate( m_v * 0.5f, m_h * 0.5f );
                }
            }
            
            //if ( Input.GetButtonDown( inputSettings.jumpButton ) )
            //    characterMove.Jump();
        }

        /// <summary>
        /// Handles camera logic
        /// </summary>
        void CameraLookLogic()
        {
            if ( !m_TPSCamera )
                return;

            m_OtherSettings.m_RequireInputForTurn = !m_IsAiming;

            if ( m_OtherSettings.m_RequireInputForTurn == true )
            {
                if ( Input.GetAxis( m_InputSettings.m_HorizontalAxis ) != 0 || Input.GetAxis( m_InputSettings.m_VerticalAxis ) != 0 )
                {
                    CharacterLook();
                }
            }
            else
            {
                CharacterLook();
            }
        }

        /// <summary>
        /// Handles all weapon logic
        /// </summary>
        void WeaponLogic()
        {
            if ( !m_WeaponHandler )
                return;

            m_IsAiming = Input.GetButton( m_InputSettings.m_AimButton ) || m_DebugAim == true;
            m_WeaponHandler.Aim( m_IsAiming );

            if ( m_IsAiming == true )
            {
                m_IsGrenadeMode = false;
            }

            if ( Input.GetButtonDown( m_InputSettings.m_SwitchWeaponButton ) )
            {
                m_WeaponHandler.SwitchWeapons();
                UpdateCrosshairs();
                PlayerHUD.instance.SetAnotherWeapon();
            }

            if ( Input.GetButtonDown( m_InputSettings.m_PrimaryWeaponButton ) )
            {
                m_WeaponHandler.SwitchWeapons( Weapon.WeaponType.Primary );
            }
            else if ( Input.GetButtonDown( m_InputSettings.m_SecondaryWeaponButton ) )
            {
                m_WeaponHandler.SwitchWeapons( Weapon.WeaponType.Secondary );
            }
            else if ( Input.GetButtonDown( m_InputSettings.m_SidearmButton ) )
            {
                //weaponHandler.SwitchWeapons( Weapon.WeaponType.Sidearm );
                m_WeaponHandler.SwitchWeapons( Weapon.WeaponType.Secondary );
            }

            if ( m_WeaponHandler.m_CurrentWeapon )
            {
                Ray aimRay = m_TPSCamera.ViewportPointToRay( new Vector3( 0.5f, 0.5f, 0f ) );
                
                Debug.DrawRay (aimRay.origin, aimRay.direction);
                if ( Input.GetButton( m_InputSettings.m_FireButton ) == true && m_IsAiming == true )
                {
                    m_IsFiring = true;
                    m_WeaponHandler.FireCurrentWeapon( aimRay );
                }
                else
                {
                    m_IsFiring = false;
                }
                if ( Input.GetButtonDown( m_InputSettings.m_ReloadButton ) == true )
                {
                    m_WeaponHandler.Reload();
                }   

                if ( m_IsAiming == true )
                {
                    ToggleCrosshair( true, m_WeaponHandler.m_CurrentWeapon );
                    //PositionCrosshair( aimRay, weaponHandler.currentWeapon );

                    if ( Player.instance.m_WeaponHandler.m_isReloading == false )
                    {
                        HitCrosshair();
                    }
                    else
                    {
                        ToggleCrosshair( false, m_WeaponHandler.m_CurrentWeapon );
                    }
                }
                else
                {
                    ToggleCrosshair( false, m_WeaponHandler.m_CurrentWeapon );
                }
            }
            else
            {
                TurnOffAllCrosshairs();
            }
        }

        private Vector3 m_Force;

        /// <summary>
        /// 
        /// </summary>
        private void GrenadeLogic()
        {
            if ( m_IsAiming == true || Player.instance.m_Inventory.m_CurrentGrenade <= 0 )
            {
                return;
            }

            if ( Input.GetButtonDown( m_InputSettings.m_GrenadeButton ) == true  )
            {
                m_IsGrenadeMode = true;
            }
            

            if ( Input.GetButtonDown( m_InputSettings.m_FireButton ) == true && m_IsGrenadeMode == true )
            {
                //force = CameraControl.instance._mainCamera.ScreenToWorldPoint( Input.mousePosition );
                m_GrenadeHandler.CreateGrenade( m_WeaponHandler.m_UserSettings.m_RightHand.position, m_WeaponHandler.m_UserSettings.m_RightHand.rotation );

                Player.instance.m_Inventory.m_CurrentGrenade--;
                PlayerHUD.instance.SetGrenadeText();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void InventoryLogic()
        {
            if ( Input.GetButtonDown( m_InputSettings.m_MedikitButton ) )
            {
                m_Inventory.UseMedikit();
            }
        }

#region crosshair

        /// <summary>
        /// crosshair 설정
        /// </summary>
        private void SetupCrosshairs()
        {
            if ( m_WeaponHandler.m_WeaponsList.Count > 0 )
            {
                foreach ( Weapon wep in m_WeaponHandler.m_WeaponsList )
                {
                    GameObject prefab = wep.m_WeaponSettings.crosshairPrefab;
                    if ( prefab != null )
                    {
                        GameObject clone = (GameObject) Instantiate( prefab );
                        clone.transform.SetParent( ScreenHUD.instance.transform );
                        clone.transform.localPosition = Vector3.zero;
                        
                        m_CrosshairPrefabMap.Add( wep, clone );
                        ToggleCrosshair( false, wep );
                    }
                }
            }
        }

        Vector3 m_FireDirection;
        Vector3 m_FirePoint;

        /// <summary>
        /// 
        /// </summary>
        void HitCrosshair()
        {
            RaycastHit hit;
            int range = 1000;
            m_FireDirection = m_TPSCamera.transform.forward * 10;
            m_FirePoint = m_WeaponHandler.m_CurrentWeapon.m_WeaponSettings.bulletSpawn.position;
            // Debug the ray out in the editor:
            Debug.DrawRay( m_FirePoint, m_FireDirection, Color.green );

            CrosshairHandler crosshair = m_WeaponHandler.m_CurrentWeapon.m_WeaponSettings.crosshairPrefab.GetComponent<CrosshairHandler>();

            if ( Physics.Raycast( m_FirePoint, ( m_FireDirection ), out hit, range ) == true )
            {
                //if ( hit.transform.gameObject.layer == LayerMask.GetMask( "Ragdoll" ) )
                if ( hit.transform.gameObject.layer == 13 )
                {
                    //Debug.LogWarning( "crosshair Red" );
                    crosshair.ChangeColor( Color.red );
                }
                else
                {
                    //Debug.Log( "crosshair white" );
                    crosshair.ChangeColor( Color.white );
                }
            }
            else
            {
                crosshair.ChangeColor( Color.white );
            }
        }

        /// <summary>
        /// 모든 crosshair들을 끈다.
        /// </summary>
        private void TurnOffAllCrosshairs()
        {
            foreach ( Weapon wep in m_CrosshairPrefabMap.Keys )
            {
                ToggleCrosshair( false, wep );
            }
        }

        /// <summary>
        /// 해당 무기의 crosshair를 생성한다.
        /// </summary>
        /// <param name="wep"></param>
        private void CreateCrosshair( Weapon wep )
        {
            GameObject prefab = wep.m_WeaponSettings.crosshairPrefab;
            if ( prefab != null )
            {
                prefab = Instantiate( prefab );
                prefab.transform.SetParent( ScreenHUD.instance.transform );
                ToggleCrosshair( false, wep );
            }
        }

        /// <summary>
        /// 해당 무기의 crosshair를 삭제한다.
        /// </summary>
        /// <param name="wep"></param>
        private void DeleteCrosshair( Weapon wep )
        {
            if ( !m_CrosshairPrefabMap.ContainsKey( wep ) )
                return;

            Destroy( m_CrosshairPrefabMap[ wep ] );
            m_CrosshairPrefabMap.Remove( wep );
        }

        // Position the crosshair to the point that we are aiming
        //private void PositionCrosshair( Ray ray, Weapon wep )
        //{
        //    Weapon curWeapon = weaponHandler.currentWeapon;
        //    if ( curWeapon == null )
        //        return;
        //    if ( !crosshairPrefabMap.ContainsKey( wep ) )
        //        return;

        //    GameObject crosshairPrefab = crosshairPrefabMap[ wep ];
        //    RaycastHit hit;
        //    Transform bSpawn = curWeapon.weaponSettings.bulletSpawn;
        //    Vector3 bSpawnPoint = bSpawn.position;
        //    Vector3 dir = ray.GetPoint( curWeapon.weaponSettings.range ) - bSpawnPoint;

        //    //Debug.DrawRay( bSpawnPoint, dir );

        //    if ( Physics.Raycast( bSpawnPoint, dir, out hit, curWeapon.weaponSettings.range,
        //        curWeapon.weaponSettings.bulletLayers ) )
        //    {
        //        if ( crosshairPrefab != null )
        //        {
        //            ToggleCrosshair( true, curWeapon );
        //            Vector3 newPos = hit.point;
        //            newPos.z = 10.0f;       // maintain certain position of crosshair z
        //            crosshairPrefab.transform.position = newPos;
        //            crosshairPrefab.transform.LookAt( Camera.main.transform );
        //        }
        //    }
        //    else
        //    {
        //        ToggleCrosshair( false, curWeapon );
        //    }
        //}


        /// <summary>
        /// Toggle on and off the crosshair prefab
        /// </summary>
        /// <param name="enabled"></param>
        /// <param name="wep"></param>
        private void ToggleCrosshair( bool enabled, Weapon wep )
        {
            if ( !m_CrosshairPrefabMap.ContainsKey( wep ) )
                return;

            m_CrosshairPrefabMap[ wep ].SetActive( enabled );
        }

        /// <summary>
        /// 
        /// </summary>
        private void UpdateCrosshairs()
        {
            if ( m_WeaponHandler.m_WeaponsList.Count == 0 )
                return;

            foreach ( Weapon wep in m_WeaponHandler.m_WeaponsList )
            {
                if ( wep != m_WeaponHandler.m_CurrentWeapon )
                {
                    ToggleCrosshair( false, wep );
                }
            }
        }
        #endregion


        /// <summary>
        /// Postions the spine when aiming
        /// </summary>
        void PositionSpine()
        {
            if ( !m_SpineTransform || !m_WeaponHandler.m_CurrentWeapon || !m_TPSCamera )
                return;

            Transform mainCamT = m_TPSCamera.transform;
            Vector3 mainCamPos = mainCamT.position;
            Vector3 dir = mainCamT.forward;
            Ray ray = new Ray( mainCamPos, dir );

            m_SpineTransform.LookAt( ray.GetPoint( 50f ) );

            Vector3 eulerAngleOffset = m_WeaponHandler.m_CurrentWeapon.m_UserSettings.spineRotation;
            m_SpineTransform.Rotate( eulerAngleOffset );
        }

        /// <summary>
        /// Make the character look at a forward point from the camera
        /// </summary>
        void CharacterLook()
        {
            Transform mainCamT = m_TPSCamera.transform;
            Transform pivotT = mainCamT.parent.parent;
            Vector3 pivotPos = pivotT.position;

            if ( m_IsFiring == true )
            {
                pivotT.Rotate( -Player.instance.m_WeaponHandler.m_CurrentWeapon.m_WeaponSettings._goUpSpeed * Time.deltaTime, 0f, 0f );

            }
            //Debug.Log( "pivotT.rotation = " + pivotT.rotation );

            Vector3 lookTarget = pivotPos + ( pivotT.forward * m_OtherSettings.m_LookDistance );
            
            Vector3 thisPos = transform.position;
            Vector3 lookDir = lookTarget - thisPos;
            
            Quaternion lookRot = Quaternion.LookRotation( lookDir );
            lookRot.x = 0;
            lookRot.z = 0;

            Quaternion newRotation = Quaternion.Lerp( transform.rotation, lookRot, Time.deltaTime * m_OtherSettings.m_LookSpeed );
            transform.rotation = newRotation;
        }
        

    }
}
