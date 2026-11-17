using UnityEngine;
using UnityEngine.UI;
namespace RetroPlatformer
{
    // Keep exactly the same visible world width at 256:240, regardless of window shape.
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, DefaultExecutionOrder(-500)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(false, sourceNamespace: "RetroPlatformer", sourceClassName: "RetroNESFrame")]
    public sealed class AspectRatio : MonoBehaviour
    {
        public Vector2Int referenceResolution = new Vector2Int(256,240);
        public RectTransform hudArea;
        public bool letterbox = true;
        Camera cam; Rect oldRect; float oldAspect; int lastWidth=-1,lastHeight=-1;
        Vector2Int lastReference; bool lastLetterbox;
        Canvas barsCanvas; readonly RectTransform[] bars=new RectTransform[4];
        void Awake(){cam=GetComponent<Camera>();oldRect=cam.rect;oldAspect=cam.aspect;Apply();}
        void LateUpdate(){if(Screen.width!=lastWidth||Screen.height!=lastHeight||referenceResolution!=lastReference||letterbox!=lastLetterbox)Apply();}
        void Apply()
        {
            if(!cam||Screen.width<=0||Screen.height<=0)return;
            lastWidth=Screen.width;lastHeight=Screen.height;lastReference=referenceResolution;lastLetterbox=letterbox;
            float target=Mathf.Max(1,referenceResolution.x)/(float)Mathf.Max(1,referenceResolution.y), current=Screen.width/(float)Screen.height;
            Rect r=new Rect(0,0,1,1);
            if(letterbox){if(current>target){r.width=target/current;r.x=(1-r.width)*.5f;}else{r.height=current/target;r.y=(1-r.height)*.5f;}}
            cam.rect=r; if(letterbox)cam.aspect=target;else cam.ResetAspect();
            if(hudArea){hudArea.anchorMin=r.min;hudArea.anchorMax=r.max;hudArea.offsetMin=Vector2.zero;hudArea.offsetMax=Vector2.zero;}
            if(!barsCanvas)CreateBars();
            SetBar(0,new Vector2(0,0),new Vector2(r.xMin,1));
            SetBar(1,new Vector2(r.xMax,0),new Vector2(1,1));
            SetBar(2,new Vector2(r.xMin,0),new Vector2(r.xMax,r.yMin));
            SetBar(3,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,1));
        }
        void CreateBars()
        {
            var obj=new GameObject("NES Black Borders (Runtime)",typeof(RectTransform),typeof(Canvas));
            barsCanvas=obj.GetComponent<Canvas>();barsCanvas.renderMode=RenderMode.ScreenSpaceOverlay;barsCanvas.sortingOrder=1000;
            for(int i=0;i<bars.Length;i++){var go=new GameObject("Border "+i,typeof(RectTransform),typeof(Image));go.transform.SetParent(obj.transform,false);var im=go.GetComponent<Image>();im.color=Color.black;im.raycastTarget=false;bars[i]=go.GetComponent<RectTransform>();}
        }
        void SetBar(int i,Vector2 a,Vector2 b){var r=bars[i];r.anchorMin=a;r.anchorMax=b;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;r.gameObject.SetActive(b.x-a.x>.0001f&&b.y-a.y>.0001f);}
        void OnDestroy(){if(barsCanvas)Destroy(barsCanvas.gameObject);if(cam){cam.rect=oldRect;cam.aspect=oldAspect;}}
    }
}
