using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System.IO;

public class PlayerManager : MonoBehaviour
{
    private PhotonView pv;
    public GameObject controller;
    private bool kill;

    public float damageDealt;
    public int headShots;
    public int bodyShots;

    private void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (pv.IsMine)
        {
            GameManager.Instance.playerManager = this;
            CreateController();
        }
    }

    public void CreateController()
    {
        kill = false;
        damageDealt = headShots = bodyShots = 0;
        Transform spawnpoint = SpawnManager.Instance.GetSpawnpoint(PhotonNetwork.LocalPlayer.IsMasterClient);
        controller = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerController"), spawnpoint.position, spawnpoint.rotation, 0, new object[] { pv.ViewID });
        GameManager.Instance.OnSpawn();
    }

    void DestroyController()
    {
        if (controller == null)
        {
            return;
        }

        PhotonNetwork.Destroy(controller);
    }

    public void Die()
    {
        DestroyController();

        if (!kill)
        {
            GameManager.Instance.OnDeath();
        }   
        else
        {
            GameManager.Instance.cam.SetActive(true);
        }
    }

    public void Kill()
    {
        kill = true;
        Invoke("DestroyController", GameManager.Instance.timeBetweenRounds);
        GameManager.Instance.OnKill();
    }
}
