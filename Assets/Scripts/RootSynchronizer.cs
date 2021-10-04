using UnityEngine;


namespace ReceiveFromTDPT
{
    /* Rootの位置と回転を反映 */
    public class RootSynchronizer
    {

        Transform tran;

        public void SetTarget(Transform tran) {
            this.tran = tran;
        }

        public void Synchronize(Vector3 rootPos, Quaternion rotoRot) {

            tran.position = rootPos;
            tran.rotation = rotoRot;
        }

    }

}
