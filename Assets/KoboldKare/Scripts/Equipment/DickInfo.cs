using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;
using KoboldKare;
using Naelstrof.Easing;
using Naelstrof.Inflatable;
using Naelstrof.Mozzarella;
using Photon.Pun;
using SkinnedMeshDecals;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(DickInfo))]
public class DickInfoEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
    }
    public void OnSceneGUI(){
        DickInfo t = (DickInfo)target;
        int i = 0;
        foreach(DickInfo.DickSet set in t.dicks) {
            if (set.dick == null) {
                continue;
            }
            Vector3 globalPosition = Handles.PositionHandle(t.transform.TransformPoint(set.attachPosition), t.transform.rotation);
            if (Vector3.Distance(t.transform.InverseTransformPoint(globalPosition), set.attachPosition) > 0.01f) {
                set.attachPosition = t.transform.InverseTransformPoint(globalPosition);
                EditorUtility.SetDirty(target);
            }
            Handles.Label(t.transform.TransformPoint(set.attachPosition), "DICK " + i++ + " ATTACH");
        }
    }
}

#endif
// DickInfo is mainly used to have an in-scene reference to a bunch of dick info. Most of the functionality of a dick is split between DickEquipment.cs, and Dick.cs
public class DickInfo : MonoBehaviour {
    private Task attachTask;
    private Kobold attachedKobold;

    [PenetratorListener(typeof(KoboldDickListener), "Kobold Dick Listener")]
    private class KoboldDickListener : PenetratorListener {
        public KoboldDickListener(Kobold kobold, DickSet set) {
            attachedKobold = kobold;
            dickSet = set;
        }

        private readonly Kobold attachedKobold;
        private DickSet dickSet;
        private float lastDepthDist;
        private Penetrable penetrableMem;
        public override void OnPenetrationStart(Penetrable penetrable) {
            base.OnPenetrationStart(penetrable);
            penetrableMem = penetrable;
        }

        protected override void OnPenetrationDepthChange(float depthDist) {
            base.OnPenetrationDepthChange(depthDist);
            float movementAmount = depthDist - lastDepthDist;
            attachedKobold.PumpUpDick(Mathf.Abs(movementAmount)*10f);
            attachedKobold.AddStimulation(Mathf.Abs(movementAmount));
            lastDepthDist = depthDist;
            dickSet.inside = depthDist != 0f && depthDist < penetrableMem.GetSplinePath().arcLength;
        }
    }

    [System.Serializable]
    public class DickSet {
        public Transform dickContainer;
        public PenetrationTech.Penetrator dick;
        
        public Inflatable ballSizeInflater;
        public Inflatable dickSizeInflater;
        public Inflatable bonerInflater;
        
        public Equipment.AttachPoint attachPoint;
        public Material cumSplatProjectorMaterial;

        public Vector3 attachPosition;
        public AudioPack cumSoundPack;

        [HideInInspector]
        public DickInfo info;
        public HumanBodyBones parent;
        [HideInInspector]
        public Transform parentTransform;
        [HideInInspector]
        public int dickIdentifier;
        public void Destroy() {
            GameObject.Destroy(dick.gameObject);
        }

        public bool inside { get; set; }
    }
    public List<DickSet> dicks = new List<DickSet>();
    public void Awake() {
        foreach (DickSet set in dicks) {
            //set.ball
            set.info = this;
            set.bonerInflater.OnEnable();
            set.dickSizeInflater.OnEnable();
            set.ballSizeInflater.OnEnable();
        }
    }
    public void AttachTo(Kobold k) {
        if (attachTask != null){
            attachTask.Stop();
        }
        attachTask = new Task(AttachToRoutine(k));
    }
    /*public void OnDickMovement(float movementAmount) {
        attachedKobold.PumpUpDick(Mathf.Abs(movementAmount));
        attachedKobold.AddStimulation(Mathf.Abs(movementAmount));
    }*/
    public void RemoveFrom(Kobold k) {
        foreach (DickSet set in dicks) {
            k.activeDicks.Remove(set);
            foreach(Rigidbody r in k.ragdoller.GetRagdollBodies()) {
                if (r.GetComponent<Collider>() == null) {
                    continue;
                }
            }
        }
        bool shouldReenableVagina = true;
        foreach(var dick in k.activeDicks) {
            if (dick.parent == HumanBodyBones.Hips) {
                shouldReenableVagina = false;
            }
        }
        if (shouldReenableVagina) {
            foreach(var hole in k.penetratables) {
                if (hole.isFemaleExclusiveAnatomy) {
                    hole.penetratable.gameObject.SetActive(true);
                }
            }
        }
        if (k == attachedKobold) {
            attachedKobold = null;
        }
    }
    public IEnumerator CumRoutine(DickSet set) {
        int pulses = 12;
        float pulseDuration = 0.8f;
        for (int i = 0; i < pulses; i++) {
            GameManager.instance.SpawnAudioClipInWorld(set.cumSoundPack, set.dick.transform.position);
            float pulseStartTime = Time.time;
            while (Time.time < pulseStartTime+pulseDuration) {
                float t = ((Time.time - pulseStartTime) / pulseDuration);
                foreach (var renderTarget in set.dick.GetTargetRenderers()) {
                    Mesh mesh = ((SkinnedMeshRenderer)renderTarget.renderer).sharedMesh;
                    float easingStart = Mathf.Clamp01(Easing.Cubic.InOut(1f-(Mathf.Abs(t-0.25f)*4f)));
                    float easingMiddle = Mathf.Clamp01(Easing.Cubic.InOut(1f-(Mathf.Abs(t-0.5f)*4f)));
                    float easingEnd = Mathf.Clamp01(Easing.Cubic.InOut(1f-(Mathf.Abs(t-0.75f)*4f)));
                    ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum0"), easingStart*100f);
                    ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum1"), easingMiddle*100f);
                    ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum2"), easingEnd*100f);
                }
                yield return null;
            }
            foreach (var renderTarget in set.dick.GetTargetRenderers()) {
                Mesh mesh = ((SkinnedMeshRenderer)renderTarget.renderer).sharedMesh;
                ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum0"), 0f);
                ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum1"), 0f);
                ((SkinnedMeshRenderer)renderTarget.renderer).SetBlendShapeWeight(mesh.GetBlendShapeIndex("Cum2"), 0f);
            }

            if (!set.dick.TryGetPenetrable(out Penetrable pennedHole) || !set.inside || pennedHole.GetComponentInParent<GenericReagentContainer>() == null) {
                if (MozzarellaPool.instance.TryInstantiate(out Mozzarella mozzarella)) {
                    ReagentContents alloc = attachedKobold.GetBallsContents().Spill(attachedKobold.GetBallsContents().volume / pulses);
                    alloc.AddMix(ReagentDatabase.GetReagent("Cum").GetReagent(attachedKobold.baseBallsSize*0.01f));
                    mozzarella.SetVolumeMultiplier(alloc.volume*2f);
                    mozzarella.hitCallback += (hit, startPos, dir, length, volume) => {
                        GenericReagentContainer container = hit.collider.GetComponentInParent<GenericReagentContainer>();
                        if (container != null) {
                            container.AddMix(alloc.Spill(alloc.volume * 0.1f), GenericReagentContainer.InjectType.Spray);
                        }

                        //Debug.DrawLine(hit.point, hit.point + hit.normal, Color.red, 5f);
                        PaintDecal.RenderDecalForCollider(hit.collider, set.cumSplatProjectorMaterial,
                            hit.point - hit.normal * 0.1f, Quaternion.LookRotation(hit.normal, Vector3.up)*Quaternion.AngleAxis(UnityEngine.Random.Range(-180f,180f), Vector3.forward),
                            Vector2.one * (volume * 4f), length);
                    };
                    mozzarella.SetFollowPenetrator(set.dick);
                }
                continue;
            }
            Vector3 holePos = pennedHole.GetSplinePath().GetPositionFromT(0f);
            Vector3 holeTangent = pennedHole.GetSplinePath().GetVelocityFromT(0f);
            SkinnedMeshDecals.PaintDecal.RenderDecalInSphere(holePos, set.dick.transform.lossyScale.x * 0.25f,
                set.cumSplatProjectorMaterial, Quaternion.LookRotation(holeTangent, Vector3.up),
                GameManager.instance.decalHitMask);
            pennedHole.GetComponentInParent<GenericReagentContainer>().AddMix(
                attachedKobold.GetBallsContents().Spill(attachedKobold.GetBallsContents().volume / pulses),
                GenericReagentContainer.InjectType.Inject);
        }
        yield return new WaitForSeconds(3f);
        attachedKobold.photonView.RPC(nameof(CharacterControllerAnimator.StopAnimationRPC), RpcTarget.All);
    }

    private IEnumerator AttachToRoutine(Kobold k) {
        attachedKobold = k;
        // We need to make sure that our model isn't disabled, otherwise k.animator.GetBoneTransform always returns null :weary:
        while(!k.gameObject.activeInHierarchy) {
            yield return new WaitUntil(()=>k.gameObject.activeInHierarchy);
        }
        // Kobold, or the dicks must've been destroyed before we got to attach. Abort!
        if (k == null || dicks == null || dicks.Count <= 0 || dicks[0] == null || dicks[0].dickContainer == null) {
            yield break;
        }
        foreach(DickSet set in dicks) {
            foreach(JigglePhysics.JiggleRigBuilder rig in set.dick.GetComponentsInChildren<JigglePhysics.JiggleRigBuilder>(true)) {
                rig.enabled = false;
            }
            foreach(Rigidbody b in set.dick.GetComponentsInChildren<Rigidbody>(true)) {
                b.isKinematic = true;
            }
        }
        bool animatorWasEnabled = k.animator.enabled;
        k.animator.enabled = true;
        foreach(DickSet set in dicks) {
            Vector3 scale = set.dickContainer.localScale;
            set.parentTransform = k.animator.GetBoneTransform(set.parent);
            while(set.parentTransform == null) {
                yield return new WaitUntil(()=>k.animator.isActiveAndEnabled);
                set.parentTransform = k.animator.GetBoneTransform(set.parent);
            }
            set.info = this;
            set.dickContainer.parent = k.attachPoints[(int)set.attachPoint];
            set.dickContainer.localScale = scale;
            set.dickContainer.transform.localPosition = -set.attachPosition;
            set.dickContainer.transform.localRotation = Quaternion.identity;

            if (set.parent == HumanBodyBones.Hips) {
                foreach(var hole in attachedKobold.penetratables) {
                    if (hole.isFemaleExclusiveAnatomy) {
                        hole.penetratable.gameObject.SetActive(false);
                    }
                }
            }

            set.dick.listeners.Add(new KoboldDickListener(k,set));
            k.activeDicks.Add(set);
            k.SetBaseDickSize(k.baseDickSize);
            k.SetBaseBallsSize(k.baseBallsSize);
            // Make sure the dick is the right color, this just forces a reset of the colors.
            Color colorSave = k.HueBrightnessContrastSaturation;
            k.HueBrightnessContrastSaturation = Color.white;
            k.HueBrightnessContrastSaturation = colorSave;
        }
        foreach(DickSet set in dicks) {
            foreach(JigglePhysics.JiggleRigBuilder rig in set.dick.GetComponentsInChildren<JigglePhysics.JiggleRigBuilder>()) {
                rig.enabled = true;
            }
        }
        k.animator.enabled = animatorWasEnabled;
    }
}
