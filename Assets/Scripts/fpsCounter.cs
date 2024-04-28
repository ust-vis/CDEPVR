using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class fpsCounter : MonoBehaviour
{
    Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = ((int)(1 / Time.smoothDeltaTime)).ToString();
    }
}
