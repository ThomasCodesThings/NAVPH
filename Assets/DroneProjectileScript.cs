using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneProjectileScript : MonoBehaviour
{

    int damage = 10;
    

    public int getDamage()
    {
        return damage;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Floor" || collision.gameObject.tag == "Enemy"){
            Destroy(gameObject);
        }
    }
}
