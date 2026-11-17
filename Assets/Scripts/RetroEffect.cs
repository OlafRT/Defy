using UnityEngine;
namespace RetroPlatformer
{
    public class RetroEffect : MonoBehaviour
    {
        Vector2 velocity; float gravity;
        public void Launch(Vector2 v, float life, float g) { velocity = v; gravity = g; Destroy(gameObject,life); }
        void Update() { velocity.y -= gravity * Time.deltaTime; transform.position += (Vector3)(velocity * Time.deltaTime); }
    }
}
