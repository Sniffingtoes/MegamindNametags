using UnityEngine;

namespace MegamindPlugin;

public class CameraFollower : MonoBehaviour
{
    public bool lookingAtThirdPerson = false;

    private void Update()
    {
        transform.LookAt((GorillaTagger.Instance.mainCamera).transform);
        transform.Rotate(new Vector3(180, 0, 180));
    }
}