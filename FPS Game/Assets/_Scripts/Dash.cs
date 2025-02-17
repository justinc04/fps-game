using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Dash : Ability
{
    [SerializeField] float speed;
    [SerializeField] float duration;
    [SerializeField] ParticleSystem dashLines;
    [SerializeField] TrailRenderer dashTrail;

    public override void UseAbility()
    {
        StartCoroutine(UseDash());
    }

    IEnumerator UseDash()
    {
        ControlMovement(true);
        playerMovement.speed = speed;

        if (playerMovement.direction == Vector3.zero)
        {
            playerMovement.direction = transform.forward;
        }

        playerMovement.direction.Normalize();
        playerMovement.velocity = Vector3.zero;
        dashLines.Play();
        pv.RPC("RPC_DashTrail", RpcTarget.Others, true);
        playerAudio.Play("Dash");
        Disable();
        playerController.canShoot = false;
        yield return new WaitForSeconds(duration);
        ControlMovement(false);
        dashLines.Stop();
        pv.RPC("RPC_DashTrail", RpcTarget.Others, false);
        playerController.canShoot = true;
        StartCoroutine(StartCooldown());
    }

    void ControlMovement(bool control)
    {
        playerMovement.speedControlled = control;
        playerMovement.directionControlled = control;
        playerMovement.gravityControlled = control;
        playerMovement.jumpControlled = control;
    }

    [PunRPC]
    void RPC_DashTrail(bool state)
    {
        dashTrail.emitting = state;
    }
}
