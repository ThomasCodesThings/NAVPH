using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserScript : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 100f;
    [SerializeField] private float bulletLifeTime = 5f;

    private float lastShotTime = 0f;
    private float shotDelay = 0.1f;
    private float bulletOffset = 1f;
    private static int maxAmmo = 10;
    private int ammo = maxAmmo;
    private int magazine = 40;
    private int damage = 25;
    private string name = "Laser";
    private GameObject player;
   

    public int getAmmo()
    {
        return ammo;
    }

    public int getMagazine()
    {
        return magazine;
    }

    public int getDamage()
    {
        return damage;
    }

    public string getName()
    {
        return name;
    }

    public void addAmmo(int amount)
    {
        magazine += amount;
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
{
    if (Time.timeScale == 0)
    {
        return;
    }

    if(player == null){
        return;
    }

    if (Input.GetKeyDown(KeyCode.R))
    {
        if (magazine > 0)
        {
            int missingAmmo = maxAmmo - ammo;
            if (magazine >= missingAmmo)
            {
                ammo = maxAmmo;
                magazine -= missingAmmo;
            }
            else
            {
                ammo += magazine;
                magazine = 0;
            }
        }
    }
    
    string currentWeaponName = player.GetComponent<PlayerScript>().getCurrentWeaponName();
    
    if(Input.GetMouseButton(0) && Time.time - lastShotTime > shotDelay && ammo > 0 && currentWeaponName == name)
    {
        lastShotTime = Time.time;

        Vector3 bulletSpawnPosition = transform.position + transform.right * bulletOffset;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPosition, Quaternion.identity);
        ammo--;
        bullet.GetComponent<Rigidbody2D>().velocity = transform.right * bulletSpeed;
        Destroy(bullet, bulletLifeTime);
    }
}
}
