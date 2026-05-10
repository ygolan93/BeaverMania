using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHandler : MonoBehaviour
{
    public Behaviour player;
    public ScorpionScript Boss;
    //NPC_Audio bossSound;
    public GameObject ChatCollider;
    public GameObject BossBar;
    public GameObject BossPanel;
    //public GameObject BossContinueButton;
    //public GameObject BossSkipButton;


    // Start is called before the first frame update
    void Start()
    {
        player = gameObject.GetComponent<Behaviour>();

        if (!ValidateReferences())
        {
            return;
        }

        //bossSound = Boss.GetComponent<NPC_Audio>();
        BossBar.SetActive(false);
    }

    bool ValidateReferences()
    {
        return RuntimeReferenceValidator.Require(player, this, nameof(player)) &
            RuntimeReferenceValidator.Require(Boss, this, nameof(Boss)) &
            RuntimeReferenceValidator.Require(ChatCollider, this, nameof(ChatCollider)) &
            RuntimeReferenceValidator.Require(BossBar, this, nameof(BossBar)) &
            RuntimeReferenceValidator.Require(BossPanel, this, nameof(BossPanel));
    }


    private void Update()
    {
        if (Boss == null)
        {
            BossBar.SetActive(false);
        }
    }


    public void SkipBossChat()
    {
        ChatCollider.SetActive(false);
        player.isAtTrader = false;
        BossPanel.SetActive(false);
        player.RestoreGameplayCursorAfterUiClose();
        Boss.isAttacking = true;
        BossBar.SetActive(true);
        player.TryConfigureFreeLookForBoss(6f, 300f, 2f, player.Root);
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Arena"))
        {  
            player.ShowCursor();
            player.isAtTrader = true;
            Boss.isAttacking = false;
            BossPanel.SetActive(true);
            player.TryConfigureFreeLookForBoss(15f, 0f, 0f, Boss.transform);
        }
    }
    [System.Obsolete("Scorpion boss damage is applied by BossHitbox components on prefab hitbox colliders.")]
    public void OnTriggerEnter(Collider OBJ)
    {
    }



}
