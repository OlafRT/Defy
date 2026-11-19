using UnityEngine;
namespace RetroPlatformer
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroCoin")]
    public class Coin : MonoBehaviour
    {
        bool collected;
        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<PlayerMovement>();
            if (collected || !p || p.IsDead) return;
            collected = true; if (GameManager.Instance) GameManager.Instance.AddCoin();
            ScoreManager.LooseCoin(transform.position + Vector3.up * (.35f * p.UpSign), p.UpSign); Destroy(gameObject);
        }
    }
}
