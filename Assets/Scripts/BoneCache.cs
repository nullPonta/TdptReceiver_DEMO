using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReceiveFromTDPT
{
    public class BoneCache
    {

        // ボーンのENUM情報テーブル
        Dictionary<string, HumanBodyBones> cacheTable = new Dictionary<string, HumanBodyBones>();

        // ボーン位置テーブル
        Dictionary<HumanBodyBones, Vector3> positionTable = new Dictionary<HumanBodyBones, Vector3>();

        // ボーン回転テーブル
        Dictionary<HumanBodyBones, Quaternion> rotationTable = new Dictionary<HumanBodyBones, Quaternion>();

        // メッセージ処理一時変数
        // 宣言structへ格納することで負荷対策
        public Vector3 rootPos;
        public Quaternion rotoRot;

        public Vector3 posBuffer;
        public Quaternion rotBuffer;


        public bool AddToTable(string boneName) {

            HumanBodyBones bone = HumanBodyBones.LastBone;

            //Humanoidボーンに該当するボーンがあるか調べる
            if (TryParse(ref boneName, out bone) == false) {
                Debug.Log(boneName);
                return false;
            }

            SetPosAndRot(bone, posBuffer, rotBuffer);

            return true;
        }

        public Dictionary<string, HumanBodyBones> GetCacheTable() {
            return cacheTable;
        }

        //ボーンENUM情報をキャッシュして高速化
        bool TryParse(ref string boneName, out HumanBodyBones bone) {

            if (cacheTable.ContainsKey(boneName)) {
                bone = cacheTable[boneName];

                if (bone == HumanBodyBones.LastBone) {
                    // LastBoneは発見しなかったことにする(無効値として扱う)
                    return false;
                }

                return true;
            }

            //キャッシュテーブルにない場合、検索する
            var res = Enum.TryParse(boneName, out bone);
            if (!res) {
                //見つからなかった場合はLastBoneとして登録する(無効値として扱う)ことにより次回から検索しない
                bone = HumanBodyBones.LastBone;
            }

            //キャシュテーブルに登録する
            cacheTable.Add(boneName, bone);

            return res;
        }

        public void Print() {

            foreach (var bone in cacheTable) {
                Debug.Log(bone.Value);
            }
        }

        public void SetPosAndRot(HumanBodyBones bone, in Vector3 pos, in Quaternion rot) {

            /* Position */
            if (positionTable.ContainsKey(bone)) {
                positionTable[bone] = pos;
            }
            else {
                positionTable.Add(bone, pos);
            }

            /* Rotation */
            if (rotationTable.ContainsKey(bone)) {
                rotationTable[bone] = rot;
            }
            else {
                rotationTable.Add(bone, rot);
            }

        }

        public bool GetPosAndRot(HumanBodyBones bone, out Vector3 pos, out Quaternion rot) {

            var isPos = positionTable.ContainsKey(bone);
            var isRot = rotationTable.ContainsKey(bone);

            if ((isPos == false) || (isRot == false)) {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                return false;
            }

            //キャッシュされた位置・回転を適用
            pos = positionTable[bone];
            rot = rotationTable[bone];

            return true;
        }

    }

}
