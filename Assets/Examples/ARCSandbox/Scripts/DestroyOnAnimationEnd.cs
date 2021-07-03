using UnityEngine;

public class DestroyOnAnimationEnd : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Destroy(animator.transform.root.gameObject, stateInfo.length);
    }
}
