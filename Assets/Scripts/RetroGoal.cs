using System.Collections;
using UnityEngine;
using UnityEngine.Events;
namespace RetroPlatformer
{
    // The goal owns the scripted movement. The controller and its visual drivers are
    // temporarily disabled so physics, input and fire palettes cannot fight the cutscene.
    public class RetroGoal : MonoBehaviour
    {
        public UnityEvent onReached;
        [Header("Scene references")]
        public Transform poleBottom, poleTop, flag, castleDoor;
        public Sprite[] smallClimb, bigClimb, fireClimb;
        [Header("Animation timing")]
        public float slideSpeed = 6f, slideFPS = 8f, grabOffset = .40f;
        public float bottomPause = .25f, turnPause = .12f, walkSpeed = 4.2f, walkFPS = 12f;
        public float descentDuration = .22f, timeTicksPerSecond = 60f, endHold = .35f;
        public bool showPolePoints = true;
        bool used;
        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<RetroPlayer>();
            if (used || !p || p.IsDead || !p.ControlEnabled) return;
            var session = RetroSession.Instance;
            if (!session) return;
            if (!poleBottom || !poleTop || !flag || !castleDoor || !p.artwork)
            { used = true; onReached?.Invoke(); session.Finish(); return; }
            float fraction = Mathf.InverseLerp(poleBottom.position.y, poleTop.position.y, p.transform.position.y + (p.IsBig ? 1.5f : .7f));
            int bonus = fraction >= .87f ? 5000 : fraction >= .67f ? 2000 : fraction >= .47f ? 800 : fraction >= .25f ? 400 : 100;
            if (!session.BeginGoal(bonus)) return;
            used = true; onReached?.Invoke(); StartCoroutine(Sequence(p, session, bonus));
        }
        IEnumerator Sequence(RetroPlayer p, RetroSession session, int bonus)
        {
            var art = p.artwork;
            var anim = p.GetComponent<RetroMarioAnimation>();
            var fire = p.GetComponent<RetroFirePower>();
            var rb = p.GetComponent<Rigidbody2D>();
            bool big = p.IsBig, fiery = p.IsFire;
            Sprite idle = anim ? (big ? anim.bigIdle : anim.smallIdle) : (big ? p.bigSprite : p.smallSprite);
            Sprite[] walk = anim ? (big ? anim.bigWalk : anim.smallWalk) : null;
            if (fiery && fire && fire.fire != null && fire.fire.Length >= 7)
            { idle = fire.fire[0]; walk = new[] { fire.fire[4], fire.fire[5], fire.fire[6] }; }
            Sprite[] climb = fiery ? fireClimb : big ? bigClimb : smallClimb;
            if (anim) anim.enabled = false;
            if (fire) fire.enabled = false;
            p.enabled = false;
            if (rb) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
            art.enabled = true; art.color = Color.white;
            Vector3 offset = anim ? (Vector3)anim.visualOffset : Vector3.zero;
            float x = poleBottom.position.x, bottom = poleBottom.position.y;
            float ground = bottom - 1f;
            float z = p.transform.position.z;
            float y = Mathf.Clamp(p.transform.position.y, bottom, Mathf.Max(bottom,poleTop.position.y - (big ? 1.8f : .9f)));
            // Keep Mario on the left of the pole, facing it; flip to the right at the base.
            p.transform.position = new Vector3(x - grabOffset, y, z);
            Vector3 flagStart = flag.position;
            Vector3 flagEnd = new Vector3(flagStart.x, bottom + .50f, flagStart.z);
            float duration = Mathf.Max(.15f, (y-bottom)/Mathf.Max(.01f,slideSpeed), (flagStart.y-flagEnd.y)/Mathf.Max(.01f,slideSpeed));
            RetroScorePopup points = showPolePoints ? RetroScorePopups.Pole(bonus, new Vector3(x+.35f,bottom+1,z) + RetroScorePopups.PoleOffset, transform) : null;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float f = Mathf.Clamp01(t/duration);
                p.transform.position = new Vector3(x-grabOffset,Mathf.Lerp(y,bottom,f),z);
                flag.position = Vector3.Lerp(flagStart,flagEnd,f);
                Pose(art, Frame(climb,t*slideFPS,idle),false,offset);
                if (points) points.transform.position = new Vector3(x+.35f,Mathf.Lerp(bottom+1,poleTop.position.y-.7f,f),z) + RetroScorePopups.PoleOffset;
                yield return null;
            }
            p.transform.position = new Vector3(x-grabOffset,bottom,z); flag.position = flagEnd;
            if (points) points.transform.position = new Vector3(x+.35f,poleTop.position.y-.7f,z) + RetroScorePopups.PoleOffset;
            yield return new WaitForSeconds(bottomPause);
            p.transform.position = new Vector3(x+grabOffset,bottom,z);
            Pose(art,Frame(climb,0,idle),true,offset);
            yield return new WaitForSeconds(turnPause);
            float stageTime = RetroAudio.BeginCastleMusic(); float musicEnd = Time.unscaledTime + stageTime;
            float ticker = 0f, elapsed = 0f;
            Vector3 start = p.transform.position;
            Vector3 landing = new Vector3(x+1.05f,ground,z);
            float descent = Mathf.Max(.01f,descentDuration);
            for (float t=0;t<descent;t+=Time.deltaTime)
            {
                p.transform.position = Vector3.Lerp(start,landing,t/descent);
                Pose(art,Frame(walk,elapsed*walkFPS,idle),false,offset);
                Tick(session,ref ticker); elapsed+=Time.deltaTime; yield return null;
            }
            p.transform.position = landing;
            Vector3 door = new Vector3(castleDoor.position.x,castleDoor.position.y,z);
            while (Vector3.Distance(p.transform.position,door)>.01f)
            {
                p.transform.position = Vector3.MoveTowards(p.transform.position,door,Mathf.Max(.01f,walkSpeed)*Time.deltaTime);
                Pose(art,Frame(walk,elapsed*walkFPS,idle),door.x<p.transform.position.x,offset);
                Tick(session,ref ticker); elapsed+=Time.deltaTime; yield return null;
            }
            p.transform.position = door; art.enabled = false;
            while (session.TimeLeftRounded > 0 || Time.unscaledTime < musicEnd)
            { Tick(session,ref ticker); yield return null; }
            yield return new WaitForSecondsRealtime(Mathf.Max(0,endHold));
            session.CompleteGoal();
        }
        void Tick(RetroSession session, ref float accumulator)
        {
            accumulator += Time.deltaTime * Mathf.Max(1,timeTicksPerSecond);
            int n = Mathf.FloorToInt(accumulator); if(n>0){accumulator-=n;session.ConsumeGoalTime(n);}
        }
        static Sprite Frame(Sprite[] frames,float phase,Sprite fallback)
        { if(frames==null || frames.Length==0)return fallback; var s=frames[Mathf.FloorToInt(Mathf.Max(0,phase))%frames.Length];return s?s:fallback; }
        static void Pose(SpriteRenderer r,Sprite sprite,bool flip,Vector3 offset)
        {
            if(!sprite)return; r.sprite=sprite; r.flipX=flip;
            r.transform.localScale=Vector3.one;
            r.transform.localPosition=new Vector3(offset.x,offset.y-sprite.bounds.min.y,r.transform.localPosition.z);
        }
    }
}
