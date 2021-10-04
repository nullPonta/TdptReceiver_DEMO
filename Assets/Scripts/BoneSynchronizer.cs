using UnityEngine;


namespace ReceiveFromTDPT
{
    public class BoneSynchronizer
    {

        //指状態反映オフ
        public bool IsHandPoseSynchronize = false;

        //目ボーン反映オフ
        public bool ISEyeBoneSynchronize = false;

        //ボーン情報取得
        Animator animator = null;


        public void SetAnimator(Animator animator) {
            this.animator = animator;
        }

        // ボーン位置をキャッシュテーブルに基づいて更新
        public void Synchronize(BoneCache boneCache) {

            Vector3 pos;
            Quaternion rot;

            var cacheTable = boneCache.GetCacheTable();

            foreach (var bone in cacheTable) {

                if (boneCache.GetPosAndRot(bone.Value, out pos, out rot) == false) {
                    continue;
                }

                Synchronize(bone.Value, pos, rot);
            }
        }

        // ボーン1本の同期
        void Synchronize(HumanBodyBones bone, in Vector3 pos, in Quaternion rot) {

            if (bone == HumanBodyBones.LastBone) {
                return;
            }

            if (animator == null) {
                return;
            }

            var t = animator.GetBoneTransform(bone);
            if (t == null) {
                return;
            }

            BoneSynchronize(t, pos, rot);
        }

        // ボーン位置と回転の同期
        void BoneSynchronize(Transform t, in Vector3 pos, in Quaternion rot) {

            t.localPosition = pos;
            t.localRotation = rot;
        }

    }

}
