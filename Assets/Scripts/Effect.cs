using UnityEngine;
namespace RetroPlatformer
{
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroEffect")]
    public class Effect : MonoBehaviour
    {
        Vector2 velocity; float gravity;
        public void Launch(Vector2 v, float life, float g) { velocity = v; gravity = g; Destroy(gameObject,life); }
        void Update() { velocity.y -= gravity * Time.deltaTime; transform.position += (Vector3)(velocity * Time.deltaTime); }
    }
}
