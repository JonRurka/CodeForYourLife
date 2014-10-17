using UnityEngine;
using System.Collections;

public class CharacterControl : MonoBehaviour {

    public float moveSpeed = 10;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rigidbody.velocity = new Vector3(horizontal * moveSpeed, rigidbody.velocity.y, vertical * moveSpeed);
	}
}
