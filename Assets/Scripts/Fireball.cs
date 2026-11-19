using UnityEngine;
namespace RetroPlatformer
{
    // Swept collision, no physical collider: cannot shove Mario, push enemies, or block the player.
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroFireball")]
    public sealed class Fireball : MonoBehaviour
    {
        PlayerMovement owner; SpriteRenderer visual; Sprite[] flight, explosions;
        Vector2 velocity; float gravity, bounce, elapsed, burstTime; int direction, upSign;
        readonly RaycastHit2D[] hits = new RaycastHit2D[48];
        public bool Finished { get; private set; }
        const float Radius = .17f;
        public void Initialize(PlayerMovement p, SpriteRenderer sr, Sprite[] f, Sprite[] e, int dir, float speed, float g, float rebound)
        { owner=p; visual=sr; flight=f; explosions=e; direction=dir; upSign=p.UpSign; velocity=new Vector2(dir * speed,-3f * upSign); gravity=g * upSign; bounce=rebound; }
        void FixedUpdate()
        {
            if (Finished || !owner) return;
            if (owner.IsDead || (GameManager.Instance && GameManager.Instance.Finished)) { Destroy(gameObject); return; }
            float dt=Time.fixedDeltaTime; velocity.y-=gravity*dt; Vector2 delta=velocity*dt;
            Vector2 pos=transform.position;
            var filter=new ContactFilter2D { useTriggers=false };
            int count=Physics2D.CircleCast(pos,Radius,delta.normalized,filter,hits,delta.magnitude);
            int best=-1; float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var c=hits[i].collider;
                if(!c || c.GetComponentInParent<PlayerMovement>()) continue;
                var enemy=c.GetComponentInParent<Walker>();
                if(enemy && (enemy.kind!=Walker.Kind.Enemy || !enemy.Active)) continue;
                if(!enemy && (owner.terrainLayers.value & (1<<c.gameObject.layer))==0) continue;
                if(hits[i].distance<distance){best=i;distance=hits[i].distance;}
            }
            if(best>=0)
            {
                var hit=hits[best]; transform.position=pos+delta.normalized*Mathf.Max(0,distance-.01f);
                var w=hit.collider.GetComponentInParent<Walker>();
                if(w) {w.HitByFire(direction); Burst();return;}
                // Marked tiles break on any impact, including a downward bounce.
                var breakable=hit.collider.GetComponent<FireBreakable>();
                if(breakable && breakable.isActiveAndEnabled && breakable.TryBreak())
                {
                    Burst(); return;
                }
                if(hit.normal.y * upSign>.55f && velocity.y * upSign<0) {velocity.y=bounce * upSign; transform.position+=Vector3.up*(.02f * upSign);}
                else { Burst(); return; }
            }
            else transform.position=pos+delta;
            var cam=Camera.main; if((upSign > 0 ? transform.position.y < -5 : transform.position.y > owner.invertedDeathY + 2) || (cam && Mathf.Abs(transform.position.x-cam.transform.position.x)>cam.orthographicSize*cam.aspect+2)) Destroy(gameObject);
        }
        void Burst(){Finished=true;burstTime=0;if(explosions!=null && explosions.Length>0) visual.sprite=explosions[0];else Destroy(gameObject);}
        void Update()
        {
            if(!owner){Destroy(gameObject);return;}
            if(Finished){burstTime+=Time.deltaTime;int i=(int)(burstTime*18f);if(explosions==null||i>=explosions.Length){Destroy(gameObject);return;} visual.sprite=explosions[i];}
            else {elapsed+=Time.deltaTime;if(elapsed>6){Destroy(gameObject);return;} visual.sprite=flight[(int)(elapsed*16f)%flight.Length];}
        }
    }
}
