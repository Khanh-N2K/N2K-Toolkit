using UnityEngine;
using UnityEngine.Events;

public class ReferenceToAnimal : MonoBehaviour
{
    [SerializeField]
    private Animal animal;

    [SerializeField]
    private UnityEvent _eventA;
}
