using UnityEngine;

namespace BlueCheese.App.Sample
{
    public class DestroyOnClick : MonoBehaviour
    {
        private void OnMouseDown()
		{
			Destroy(gameObject);
		}
    }
}
