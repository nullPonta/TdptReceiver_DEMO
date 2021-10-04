/*
 * TdptReceiver
 * 
 * Original
 *   ExternalReceiver
 *   https://sabowl.sakura.ne.jp/gpsnmeajp/
 *   https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity
 *
 * MIT License
 * 
 * Copyright (c) 2019 gpsnmeajp
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Text;
using UnityEngine;


namespace ReceiveFromTDPT
{

    public class TdptReceiver : MonoBehaviour
    {
        [Header("TdptReceiver")]

        public bool IsFreeze = false;

        //パケットフレーム数が一定値を超えるとき、パケットを捨てる
        public bool IsPacktLimiter = true;

        /*--------------------------------*
         * パケット数
         *--------------------------------*/
        // リミットを超えたら、それ以上は受け取らない。
        const int PACKET_LIMIT_MAX = 30;

        // パケット数測定
        int packetCounterInFrame = 0;

        // 受信したパケットフレーム数
        int lastPacketframeCounterInFrame = 0;

        // 廃棄されたパケット
        int dropPackets = 0;

        /*--------------------------------*
         * 通信
         *--------------------------------*/
        //通信状態の保持変数
        int available = 0;

        //送信時の時刻
        float remoteTime = 0;

        uOSC.uOscServer server = null;

        BoneCache boneCache = new BoneCache();

        BoneCache traCache = new BoneCache();

        Action<BoneCache> onReceived = null;

        /*--------------------------------*
         * Message
         *--------------------------------*/
        StringBuilder sb = new StringBuilder();

        string statusMessage = "";


        public void Start() {

            //5.6.3p1などRunInBackgroundが既定で無効な場合Unityが極めて重くなるため対処
            Application.runInBackground = true;

            //サーバーを取得
            server = GetComponent<uOSC.uOscServer>();
            if (server == null) {
                Debug.LogError("uOscServer is missing.");
                return;
            }

            //サーバーを初期化
            Debug.Log("onDataReceived.AddListener");
            statusMessage = "Waiting for TDPT...";
            server.onDataReceived.AddListener(OnDataReceived);
        }

        public void SetOnReceived(Action<BoneCache> onReceived) {
            this.onReceived = onReceived;
        }

        //外部から通信状態を取得するための公開関数
        public int GetAvailable() {
            return available;
        }

        //外部から通信時刻を取得するための公開関数
        public float GetRemoteTime() {
            return remoteTime;
        }

        public string GetMessage() {

            sb.Clear();
            sb.AppendLine("StatusMessage : " + statusMessage);
            sb.AppendLine("Available : " + available);
            sb.AppendLine("RemoteTime : " + remoteTime);
            sb.AppendLine("");
            sb.AppendLine("LastPacketframeCounterInFrame : " + lastPacketframeCounterInFrame);
            sb.AppendLine("DropPackets : " + dropPackets);

            return sb.ToString();
        }

        public void Update() {
            //Freeze有効時は動きを一切止める
            if (IsFreeze) { return; }

            lastPacketframeCounterInFrame = packetCounterInFrame;
            packetCounterInFrame = 0;

            if (onReceived != null) {
                onReceived(boneCache);
            }

        }

        //データ受信イベント
        private void OnDataReceived(uOSC.Message message) {
            //Startされていない場合無視
            if ((enabled == false) || (gameObject.activeInHierarchy == false)) {
                return;
            }

            if (IsFreeze) {
                return;
            }

            if (IsPacktLimiter &&
                (lastPacketframeCounterInFrame > PACKET_LIMIT_MAX)) {
                
                // パケットリミッターが有効な場合
                // 一定以上のパケット数を観測した場合、次のフレームまでパケットを捨てる
                dropPackets++;
                return;
            }

            try {
                //メッセージを処理
                ProcessMessage(ref message);
            }
            catch (Exception e) {
                //異常を検出して動作停止
                statusMessage = "Error: Exception";
                Debug.LogError(" --- Communication Error ---");
                Debug.LogError(e.ToString());
                return;
            }

        }

        //メッセージ処理本体
        private void ProcessMessage(ref uOSC.Message message) {

            // メッセージアドレスがない
            // あるいはメッセージがない不正な形式の場合は処理しない
            if (message.address == null || message.values == null) {
                statusMessage = "Bad message.";
                return;
            }

            /* Bone姿勢 */
            if (ProcessMessage_VMC_Ext_Bone_Pos(ref message)) {
                return;
            }

            /* Device Transform : トラッカーの姿勢情報 */
            if (ProcessMessage_VMC_Ext_Tra_Pos(ref message)) {
                return;
            }

            /* Root姿勢 */
            if (ProcessMessage_VMC_Ext_Root_Pos(ref message)) {
                return;
            }

            /* 利用可否 (Available) */
            if (ProcessMessage_VMC_Ext_OK(ref message)) {
                return;
            }

            /* 送信側相対時刻 (Time) */
            if (ProcessMessage_VMC_Ext_T(ref message)) {
                return;
            }

            Debug.LogError("Unknown message : " + message.address);
        }

        bool ProcessMessage_VMC_Ext_OK(ref uOSC.Message message) {

            if (message.address != "/VMC/Ext/OK") {
                return false;
            }

            if (message.values[0] is not int) {
                statusMessage = "Bad message.";
                return false;
            }

            //モーションデータ送信可否
            available = (int)message.values[0];

            return true;
        }

        bool ProcessMessage_VMC_Ext_T(ref uOSC.Message message) {

            if (message.address != "/VMC/Ext/T") {
                return false;
            }

            if (message.values[0] is not float) {
                statusMessage = "Bad message.";
                return false;
            }

            //データ送信時刻
            remoteTime = (float)message.values[0];

            //フレーム中のパケットフレーム数を測定
            packetCounterInFrame++;

            return true;
        }

        bool ProcessMessage_VMC_Ext_Root_Pos(ref uOSC.Message message) {

            if (message.address != "/VMC/Ext/Root/Pos") {
                return false;
            }

            if ((message.values[0] is not string) ||
                (message.values[1] is not float) ||
                (message.values[2] is not float) ||
                (message.values[3] is not float) ||
                (message.values[4] is not float) ||
                (message.values[5] is not float) ||
                (message.values[6] is not float) ||
                (message.values[7] is not float)) {

                statusMessage = "Bad message.";
                return false;
            }

            statusMessage = "Root-Pos [OK]";

            boneCache.rootPos.x = (float)message.values[1];
            boneCache.rootPos.y = (float)message.values[2];
            boneCache.rootPos.z = (float)message.values[3];

            boneCache.rotoRot.x = (float)message.values[4];
            boneCache.rotoRot.y = (float)message.values[5];
            boneCache.rotoRot.z = (float)message.values[6];
            boneCache.rotoRot.w = (float)message.values[7];

            return true;
        }

        bool ProcessMessage_VMC_Ext_Bone_Pos(ref uOSC.Message message) {

            if (message.address != "/VMC/Ext/Bone/Pos") {
                return false;
            }

            if ((message.values[0] is not string) ||
                (message.values[1] is not float) ||
                (message.values[2] is not float) ||
                (message.values[3] is not float) ||
                (message.values[4] is not float) ||
                (message.values[5] is not float) ||
                (message.values[6] is not float) ||
                (message.values[7] is not float)) {

                statusMessage = "Bad message.";
                return false;
            }

            string boneName = (string)message.values[0];

            boneCache.posBuffer.x = (float)message.values[1];
            boneCache.posBuffer.y = (float)message.values[2];
            boneCache.posBuffer.z = (float)message.values[3];

            boneCache.rotBuffer.x = (float)message.values[4];
            boneCache.rotBuffer.y = (float)message.values[5];
            boneCache.rotBuffer.z = (float)message.values[6];
            boneCache.rotBuffer.w = (float)message.values[7];

            boneCache.AddToTable(boneName);

            return true;
        }

        bool ProcessMessage_VMC_Ext_Tra_Pos(ref uOSC.Message message) {

            if (message.address != "/VMC/Ext/Tra/Pos") {
                return false;
            }

            if ((message.values[0] is not string) ||
                (message.values[1] is not float) ||
                (message.values[2] is not float) ||
                (message.values[3] is not float) ||
                (message.values[4] is not float) ||
                (message.values[5] is not float) ||
                (message.values[6] is not float) ||
                (message.values[7] is not float)) {

                statusMessage = "Bad message.";
                return false;
            }

            string boneName = (string)message.values[0];

            traCache.posBuffer.x = (float)message.values[1];
            traCache.posBuffer.y = (float)message.values[2];
            traCache.posBuffer.z = (float)message.values[3];

            traCache.rotBuffer.x = (float)message.values[4];
            traCache.rotBuffer.y = (float)message.values[5];
            traCache.rotBuffer.z = (float)message.values[6];
            traCache.rotBuffer.w = (float)message.values[7];

            traCache.AddToTable(boneName);

            return true;
        }

    }

}
