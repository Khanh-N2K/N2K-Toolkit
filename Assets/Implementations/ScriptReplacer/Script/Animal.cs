using N2K;
using UnityEngine;

public abstract class Animal : MonoBehaviour
{
    [InfoBox("Press the 3 dots button -> Replace Script and change them between cow - tiger\nThen see the 'ReferenceToAnimal' game object to see if the reference is changed or still there"
        , InforBoxMessageType.Info)]
    public string animalName = "Tigger";
    public float size = 1.8f;
    public int weight = 220;
}
