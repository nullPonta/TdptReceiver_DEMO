using ReceiveFromTDPT;
using UnityEngine;


public class TdptReceiverDEMO : MonoBehaviour
{
    /* 3D pose */
    TdptReceiver tdptReceiver;

    BoneSynchronizer boneSynchronizer = new BoneSynchronizer();
    RootSynchronizer rootSynchronizer = new RootSynchronizer();



    void Start()
    {
        tdptReceiver = GameObject.Find("/uOSC_ReceiveFromTDPT").GetComponent<TdptReceiver>();
        tdptReceiver.SetOnReceived(OnReceivedPosePrediction);

        var character = GameObject.Find("CecilHenShin");
        boneSynchronizer.SetAnimator(character.GetComponent<Animator>());
        rootSynchronizer.SetTarget(character.transform);

    }

    void OnReceivedPosePrediction(BoneCache boneCache) {

        boneSynchronizer.Synchronize(boneCache);
        rootSynchronizer.Synchronize(boneCache.rootPos, boneCache.rotoRot);
    }

}
