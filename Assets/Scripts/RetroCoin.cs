using UnityEngine;
namespace RetroPlatformer
{
    public class RetroCoin : MonoBehaviour
    {
        bool collected;
        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<RetroPlayer>();
            if (collected || !p || p.IsDead) return;
            collected = true; if (RetroSession.Instance) RetroSession.Instance.AddCoin();
            RetroScorePopups.LooseCoin(transform.position + Vector3.up * .35f); Destroy(gameObject);
        }
    }
}
